using System.Linq;
using System.Runtime.CompilerServices;
using OpenDreamClient.Interface;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using OpenDreamShared.Rendering;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Profiling;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering world atoms
/// </summary>
internal sealed class DreamViewOverlay : Overlay {
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public bool ScreenOverlayEnabled = true;
    public bool MouseMapRenderEnabled;

    public ShaderInstance BlockColorInstance;
    public Texture? MouseMap => _mouseMapRenderTarget?.Texture;
    public readonly Dictionary<Color, RendererMetaData> MouseMapLookup = new();
    public readonly Dictionary<string, IRenderTexture> RenderSourceLookup = new();

    private const LookupFlags MapLookupFlags = LookupFlags.Approximate | LookupFlags.Uncontained;

    [Robust.Shared.IoC.Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IEntityManager _entityManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IClyde _clyde = default!;
    [Robust.Shared.IoC.Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly ProfManager _prof = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.view");

    private readonly TransformSystem _transformSystem;
    private readonly MapSystem _mapSystem;
    private readonly EntityLookupSystem _lookupSystem;
    private readonly ClientAppearanceSystem _appearanceSystem;
    private readonly ClientScreenOverlaySystem _screenOverlaySystem;
    private readonly ClientImagesSystem _clientImagesSystem;

    private readonly EntityQuery<DMISpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<DreamMobSightComponent> _mobSightQuery;

    private readonly Dictionary<int, DreamPlane> _planes = new();
    private readonly List<RendererMetaData> _spriteContainer = new();

    private readonly Dictionary<BlendMode, ShaderInstance> _blendModeInstances;
    private static ShaderInstance _colorInstance = default!;

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();

    private IRenderTexture? _mouseMapRenderTarget;
    private IRenderTexture? _baseRenderTarget;
    private readonly Stack<IRenderTexture> _renderTargetsToReturn = new();
    private readonly Stack<RendererMetaData> _rendererMetaDataRental = new();
    private readonly Stack<RendererMetaData> _rendererMetaDataToReturn = new();

    private static readonly Matrix3 FlipMatrix = Matrix3.Identity with {
        R1C1 = -1
    };

    // Defined here so it isn't recreated every frame
    private ViewAlgorithm.Tile?[,]? _tileInfo;
    private readonly HashSet<EntityUid> _entities = new();

    public DreamViewOverlay(TransformSystem transformSystem, MapSystem mapSystem, EntityLookupSystem lookupSystem,
        ClientAppearanceSystem appearanceSystem, ClientScreenOverlaySystem screenOverlaySystem, ClientImagesSystem clientImagesSystem) {
        IoCManager.InjectDependencies(this);
        _transformSystem = transformSystem;
        _mapSystem = mapSystem;
        _lookupSystem = lookupSystem;
        _appearanceSystem = appearanceSystem;
        _screenOverlaySystem = screenOverlaySystem;
        _clientImagesSystem = clientImagesSystem;

        _spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        _xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        _mobSightQuery = _entityManager.GetEntityQuery<DreamMobSightComponent>();

        _sawmill.Debug("Loading shaders...");
        BlockColorInstance = _protoManager.Index<ShaderPrototype>("blockcolor").InstanceUnique();
        _colorInstance = _protoManager.Index<ShaderPrototype>("color").InstanceUnique();
        _blendModeInstances = new(6) {
            {BlendMode.Default, _protoManager.Index<ShaderPrototype>("blend_overlay").InstanceUnique()}, //BLEND_DEFAULT (Same as BLEND_OVERLAY when there's no parent)
            {BlendMode.Overlay, _protoManager.Index<ShaderPrototype>("blend_overlay").InstanceUnique()}, //BLEND_OVERLAY
            {BlendMode.Add, _protoManager.Index<ShaderPrototype>("blend_add").InstanceUnique()}, //BLEND_ADD
            {BlendMode.Subtract, _protoManager.Index<ShaderPrototype>("blend_subtract").InstanceUnique()}, //BLEND_SUBTRACT
            {BlendMode.Multiply, _protoManager.Index<ShaderPrototype>("blend_multiply").InstanceUnique()}, //BLEND_MULTIPLY
            {BlendMode.InsertOverlay, _protoManager.Index<ShaderPrototype>("blend_inset_overlay").InstanceUnique()} //BLEND_INSET_OVERLAY //TODO
        };
    }

    protected override void Draw(in OverlayDrawArgs args) {
        using var _ = _prof.Group("Dream View Overlay");

        EntityUid? eye = _playerManager.LocalSession?.AttachedEntity;
        if (eye == null)
            return;

        //Main drawing of sprites happens here
        try {
            var viewportSize = (Vector2i)(args.Viewport.Size / args.Viewport.RenderScale);

            DrawAll(args, eye.Value, viewportSize);
        } catch (Exception e) {
            _sawmill.Error($"Error occurred while rendering frame. Error details:\n{e.Message}\n{e.StackTrace}");
        }

        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();

        RenderSourceLookup.Clear();

        //some render targets need to be kept until the end of the render cycle, so return them here.
        while(_renderTargetsToReturn.Count > 0)
            ReturnRenderTarget(_renderTargetsToReturn.Pop());

        //RendererMetaData objects get reused instead of garbage collected
        while (_rendererMetaDataToReturn.Count > 0)
            _rendererMetaDataRental.Push(_rendererMetaDataToReturn.Pop());
    }

    private void DrawAll(OverlayDrawArgs args, EntityUid eye, Vector2i viewportSize) {
        if (!_xformQuery.TryGetComponent(eye, out var eyeTransform))
            return;

        var eyeCoords = _transformSystem.GetMapCoordinates(eye, eyeTransform);
        if (!_mapManager.TryFindGridAt(eyeCoords, out var gridUid, out var grid))
            return;

        _mobSightQuery.TryGetComponent(eye, out var mobSight);
        var seeVis = mobSight?.SeeInvisibility ?? 127;
        var sight = mobSight?.Sight ?? 0;

        var worldHandle = args.WorldHandle;

        using (_prof.Group("lookup")) {
            //TODO use a sprite tree.
            //the scaling is to attempt to prevent pop-in, by rendering sprites that are *just* offscreen
            _lookupSystem.GetEntitiesIntersecting(args.MapId, args.WorldAABB.Scale(1.2f), _entities, MapLookupFlags);
        }

        var eyeTile = _mapSystem.GetTileRef(gridUid, grid, eyeCoords);
        var tiles = CalculateTileVisibility(gridUid, grid, _entities, eyeTile, seeVis);

        RefreshRenderTargets(args.WorldHandle, viewportSize);

        CollectVisibleSprites(tiles, gridUid, grid, eyeTile, _entities, seeVis, sight, args.WorldAABB);
        ClearPlanes();
        ProcessSprites(worldHandle, viewportSize, args.WorldAABB);

        //Final draw
        DrawPlanes(worldHandle, args.WorldAABB);

        //At this point all the sprites have been rendered to the base target, now we just draw it to the viewport!
        worldHandle.DrawTexture(
            MouseMapRenderEnabled ? _mouseMapRenderTarget!.Texture : _baseRenderTarget!.Texture,
            args.WorldAABB.BottomLeft);
    }

    //handles underlays, overlays, appearance flags, images. Adds them to the result list, so they can be sorted and drawn with DrawIcon()
    private void ProcessIconComponents(DreamIcon icon, Vector2 position, EntityUid uid, bool isScreen, ref int tieBreaker, List<RendererMetaData> result, RendererMetaData? parentIcon = null, bool keepTogether = false, Vector3? turfCoords = null) {
        if (icon.Appearance is null) //in the event that appearance hasn't loaded yet
            return;

        result.EnsureCapacity(result.Count + icon.Underlays.Count + icon.Overlays.Count + 1);
        RendererMetaData current = RentRendererMetaData();
        current.MainIcon = icon;
        current.Position = position + (icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter);
        current.Uid = uid;
        current.ClickUid = uid;
        current.IsScreen = isScreen;
        current.TieBreaker = tieBreaker;
        current.RenderSource = icon.Appearance.RenderSource;
        current.RenderTarget = icon.Appearance.RenderTarget;
        current.AppearanceFlags = icon.Appearance.AppearanceFlags;
        current.BlendMode = icon.Appearance.BlendMode;
        current.MouseOpacity = icon.Appearance.MouseOpacity;

        Matrix3 iconAppearanceTransformMatrix = new( //reverse rotation transforms because of 180 flip from RenderTarget->world transform
            icon.Appearance.Transform[0], -icon.Appearance.Transform[1], icon.Appearance.Transform[4],
            -icon.Appearance.Transform[2], icon.Appearance.Transform[3], icon.Appearance.Transform[5],
            0, 0, 1
        );

        if (parentIcon != null) {
            current.ClickUid = parentIcon.ClickUid;
            if ((icon.Appearance.AppearanceFlags & AppearanceFlags.ResetColor) != 0 || keepTogether) { //RESET_COLOR
                current.ColorToApply = icon.Appearance.Color;
                current.ColorMatrixToApply = icon.Appearance.ColorMatrix;
            } else {
                current.ColorToApply = parentIcon.ColorToApply * icon.Appearance.Color;
                ColorMatrix.Multiply(ref parentIcon.ColorMatrixToApply, ref icon.Appearance.ColorMatrix, out current.ColorMatrixToApply);
            }

            if ((icon.Appearance.AppearanceFlags & AppearanceFlags.ResetAlpha) != 0 || keepTogether) //RESET_ALPHA
                current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            else
                current.AlphaToApply = parentIcon.AlphaToApply * (icon.Appearance.Alpha / 255.0f);

            if ((icon.Appearance.AppearanceFlags & AppearanceFlags.ResetTransform) != 0 || keepTogether) //RESET_TRANSFORM
                current.TransformToApply = iconAppearanceTransformMatrix;
            else
                current.TransformToApply = parentIcon.TransformToApply;

            if ((icon.Appearance.Plane < -10000)) //FLOAT_PLANE - Note: yes, this really is how it works. Yes it's dumb as shit.
                current.Plane = parentIcon.Plane + (icon.Appearance.Plane + 32767);
            else
                current.Plane = icon.Appearance.Plane;

            current.Layer = (icon.Appearance.Layer < 0) ? parentIcon.Layer : icon.Appearance.Layer; //FLOAT_LAYER

            if (current.BlendMode == BlendMode.Default)
                current.BlendMode = parentIcon.BlendMode;
        } else {
            current.ColorToApply = icon.Appearance.Color;
            current.ColorMatrixToApply = icon.Appearance.ColorMatrix;
            current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            current.TransformToApply = iconAppearanceTransformMatrix;
            current.Plane = icon.Appearance.Plane;
            current.Layer = Math.Max(0, icon.Appearance.Layer); //float layers are invalid for icons with no parent
        }

        //special handling for EFFECTS_LAYER and BACKGROUND_LAYER
        //SO IT TURNS OUT EFFECTS_LAYER IS JUST A LIE *scream
        //and BACKGROUND_LAYER is basically the same behaviour as FLOAT_PLANE
        if (current.Layer >= 20000) {
            current.Layer -= 40000;
            current.IsScreen = false; //BACKGROUND_LAYER renders behind everything on that plane
        }

        keepTogether |= ((current.AppearanceFlags & AppearanceFlags.KeepTogether) != 0); //KEEP_TOGETHER

        //if the render-target starts with *, we don't render it. If it doesn't we create a placeholder RenderMetaData to position it correctly
        if (!string.IsNullOrEmpty(current.RenderTarget) && current.RenderTarget[0] != '*') {
            RendererMetaData renderTargetPlaceholder = RentRendererMetaData();

            //transform, color, alpha, filters - they should all already have been applied, so we leave them null in the placeholder
            renderTargetPlaceholder.MainIcon = current.MainIcon;
            renderTargetPlaceholder.Position = current.Position;
            renderTargetPlaceholder.Uid = current.Uid;
            renderTargetPlaceholder.ClickUid = current.Uid;
            renderTargetPlaceholder.IsScreen = current.IsScreen;
            renderTargetPlaceholder.TieBreaker = current.TieBreaker;
            renderTargetPlaceholder.Plane = current.Plane;
            renderTargetPlaceholder.Layer = current.Layer;
            renderTargetPlaceholder.RenderSource = current.RenderTarget;
            renderTargetPlaceholder.MouseOpacity = current.MouseOpacity;
            if((current.AppearanceFlags & AppearanceFlags.PlaneMaster) != 0){ //Plane masters with render targets get special handling
                renderTargetPlaceholder.TransformToApply = current.TransformToApply;
                renderTargetPlaceholder.ColorToApply = current.ColorToApply;
                renderTargetPlaceholder.ColorMatrixToApply = current.ColorMatrixToApply;
                renderTargetPlaceholder.AlphaToApply = current.AlphaToApply;
                renderTargetPlaceholder.BlendMode = current.BlendMode;
            }
            renderTargetPlaceholder.AppearanceFlags = current.AppearanceFlags;
            current.AppearanceFlags = current.AppearanceFlags & ~AppearanceFlags.PlaneMaster; //only the placeholder should be marked as master
            result.Add(renderTargetPlaceholder);
        }

        //underlays - colour, alpha, and transform are inherited, but filters aren't
        foreach (DreamIcon underlay in icon.Underlays) {
            if (underlay.Appearance == null)
                continue;

            tieBreaker++;

            if (!keepTogether || (underlay.Appearance.AppearanceFlags & AppearanceFlags.KeepApart) != 0) { //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
                ProcessIconComponents(underlay, current.Position, uid, isScreen, ref tieBreaker, result, current);
            } else {
                current.KeepTogetherGroup ??= new();
                ProcessIconComponents(underlay, current.Position, uid, isScreen, ref tieBreaker, current.KeepTogetherGroup, current, keepTogether);
            }
        }

        tieBreaker++;
        current.TieBreaker = tieBreaker;

        //overlays - colour, alpha, and transform are inherited, but filters aren't
        foreach (DreamIcon overlay in icon.Overlays) {
            if (overlay.Appearance == null)
                continue;

            tieBreaker++;

            if (!keepTogether || (overlay.Appearance.AppearanceFlags & AppearanceFlags.KeepApart) != 0) { //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
                ProcessIconComponents(overlay, current.Position, uid, isScreen, ref tieBreaker, result, current);
            } else {
                current.KeepTogetherGroup ??= new();
                ProcessIconComponents(overlay, current.Position, uid, isScreen, ref tieBreaker, current.KeepTogetherGroup, current, keepTogether);
            }
        }

        //client images act as either an overlay or replace the main icon
        //notably they cannot be applied to overlays, so don't check for them if this is an under/overlay
        //note also that we use turfCoords and not current.Position because we want world-coordinates, not screen coordinates. This is only used for turfs.
        if(parentIcon == null && _clientImagesSystem.TryGetClientImages(current.Uid, turfCoords, out List<NetEntity>? attachedClientImages)){
            foreach(NetEntity ciNetEntity in attachedClientImages) {
                EntityUid imageEntity = _entityManager.GetEntity(ciNetEntity);
                if (!_spriteQuery.TryGetComponent(imageEntity, out var sprite))
                    continue;
                if(sprite.Icon.Appearance == null)
                    continue;
                if(sprite.Icon.Appearance.Override)
                    current.MainIcon = sprite.Icon;
                else
                    ProcessIconComponents(sprite.Icon, current.Position, uid, isScreen, ref tieBreaker, result, current);
            }
        }

        foreach (var visContent in icon.Appearance.VisContents) {
            EntityUid visContentEntity = _entityManager.GetEntity(visContent);
            if (!_spriteQuery.TryGetComponent(visContentEntity, out var sprite))
                continue;

            ProcessIconComponents(sprite.Icon, position, visContentEntity, false, ref tieBreaker, result, current, keepTogether);

            // TODO: click uid should be set to current.uid again
            // TODO: vis_flags
        }

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //flatten KeepTogetherGroup. Done here so we get implicit recursive iteration down the tree.
        if (current.KeepTogetherGroup?.Count > 0) {
            List<RendererMetaData> flatKeepTogetherGroup = new List<RendererMetaData>(current.KeepTogetherGroup.Count);

            foreach (RendererMetaData ktItem in current.KeepTogetherGroup) {
                if (ktItem.KeepTogetherGroup != null)
                    flatKeepTogetherGroup.AddRange(ktItem.KeepTogetherGroup);

                flatKeepTogetherGroup.Add(ktItem);
                ktItem.KeepTogetherGroup = null; //might need to be Clear()
            }

            current.KeepTogetherGroup = flatKeepTogetherGroup;
        }

        result.Add(current);
    }

    private IRenderTexture RentRenderTarget(Vector2i size) {
        IRenderTexture result;

        if (!_renderTargetCache.TryGetValue(size, out var listResult)) {
            result = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
        } else {
            if (listResult.Count > 0) {
                result = listResult[0]; //pop a value
                listResult.Remove(result);
            } else {
                result = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
            }
        }

        return result;
    }

    private void ReturnRenderTarget(IRenderTexture rental) {
        if (!_renderTargetCache.TryGetValue(rental.Size, out var storeList)) {
            storeList = new List<IRenderTexture>(4);
            _renderTargetCache.Add(rental.Size, storeList);
        }

        storeList.Add(rental);
    }

    private void ClearRenderTarget(IRenderTexture target, DrawingHandleWorld handle, Color clearColor) {
        handle.RenderInRenderTarget(target, () => {}, clearColor);
    }

    public ShaderInstance? GetBlendAndColorShader(RendererMetaData iconMetaData, bool ignoreColor = false, bool useOverlayMode = false) {
        BlendMode blendMode = useOverlayMode ? BlendMode.Overlay : iconMetaData.BlendMode;

        ColorMatrix colorMatrix;
        if (ignoreColor)
            colorMatrix = ColorMatrix.Identity;
        else if (iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity))
            colorMatrix = new ColorMatrix(iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
        else
            colorMatrix = iconMetaData.ColorMatrixToApply;

        // We can use no shader if everything is default
        if (!iconMetaData.IsPlaneMaster && blendMode is BlendMode.Default or BlendMode.Overlay &&
            colorMatrix.Equals(ColorMatrix.Identity))
            return null;

        var blendAndColor = _blendModeInstances[blendMode].Duplicate();
        blendAndColor.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
        blendAndColor.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
        blendAndColor.SetParameter("isPlaneMaster", iconMetaData.IsPlaneMaster);
        return blendAndColor;
    }

    public void DrawIcon(DrawingHandleWorld handle, Vector2i renderTargetSize, RendererMetaData iconMetaData, Vector2 positionOffset) {
        DreamIcon? icon = iconMetaData.MainIcon;
        if (icon == null)
            return;

        //KEEP_TOGETHER groups
        if (iconMetaData.KeepTogetherGroup?.Count > 0) {
            // TODO: Use something better than a hardcoded 64x64 fallback
            iconMetaData.TextureOverride = ProcessKeepTogether(handle, iconMetaData, iconMetaData.Texture?.Size ?? (64,64));
        }

        var pixelPosition = (iconMetaData.Position + positionOffset) * EyeManager.PixelsPerMeter;
        var frame = iconMetaData.Texture;

        //if frame is null, this doesn't require a draw, so return NOP
        if (frame == null)
            return;

        //go fast when the only filter is color, and we don't have more color things to consider
        bool goFastOverride = false;
        if (icon.Appearance != null && iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity) &&
            iconMetaData.ColorToApply == Color.White && iconMetaData.AlphaToApply.Equals(1.0f) &&
            icon.Appearance.Filters is [{ FilterType: "color" }]) {
            DreamFilterColor colorFilter = (DreamFilterColor)icon.Appearance.Filters[0];
            iconMetaData.ColorMatrixToApply = colorFilter.Color;
            goFastOverride = true;
        }

        if (goFastOverride || icon.Appearance == null || icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            DrawIconFast(handle, renderTargetSize, frame, pixelPosition, GetBlendAndColorShader(iconMetaData));
        } else {
            //Slower path for filtered icons
            DrawIconSlow(handle, frame, iconMetaData, renderTargetSize, pixelPosition);
        }
    }

    /// <summary>
    /// Recreate all our render targets if our viewport size has changed.
    /// Also clears the mouse map and base render target.
    /// </summary>
    private void RefreshRenderTargets(DrawingHandleWorld handle, Vector2i size) {
        if (_baseRenderTarget == null || _baseRenderTarget.Size != size) {
            _baseRenderTarget?.Dispose();
            _mouseMapRenderTarget?.Dispose();
            _baseRenderTarget = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
            _mouseMapRenderTarget = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));

            foreach (var plane in _planes.Values) {
                plane.SetMainRenderTarget(_clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb)));
            }
        } else {
            // Clear the mouse map lookup dictionary
            MouseMapLookup.Clear();

            ClearRenderTarget(_baseRenderTarget, handle, new Color());
            ClearRenderTarget(_mouseMapRenderTarget!, handle, new Color());
        }
    }

    private void ClearPlanes() {
        foreach (var pair in _planes) {
            var plane = pair.Value;

            // We can remove the plane if there was nothing on it last frame
            if (plane.Sprites.Count == 0 && plane.Master == null) {
                plane.Dispose();
                _planes.Remove(pair.Key);
                continue;
            }

            plane.Clear();
        }
    }

    private DreamPlane GetPlane(int planeIndex, Vector2i viewportSize) {
        if (_planes.TryGetValue(planeIndex, out var plane))
            return plane;

        var renderTarget = _clyde.CreateRenderTarget(viewportSize, new(RenderTargetColorFormat.Rgba8Srgb));

        plane = new(renderTarget);
        _planes.Add(planeIndex, plane);
        _sawmill.Info($"Created plane {planeIndex}");
        return plane;
    }

    private void ProcessSprites(DrawingHandleWorld handle, Vector2i viewportSize, Box2 worldAABB) {
        using var _ = _prof.Group("process sprites / draw render targets");

        //all sprites with render targets get handled first - these are ordered by sprites.Sort(), so we can just iterate normally
        foreach (var sprite in _spriteContainer) {
            var plane = GetPlane(sprite.Plane, viewportSize);

            if (!string.IsNullOrEmpty(sprite.RenderTarget)) {
                //if this sprite has a render target, draw it to a slate instead. If it needs to be drawn on the map, a second sprite instance will already have been created for that purpose
                if (!RenderSourceLookup.TryGetValue(sprite.RenderTarget, out var tmpRenderTarget)) {
                    tmpRenderTarget = RentRenderTarget(viewportSize);
                    ClearRenderTarget(tmpRenderTarget, handle, new Color());
                    RenderSourceLookup.Add(sprite.RenderTarget, tmpRenderTarget);
                    _renderTargetsToReturn.Push(tmpRenderTarget);
                }

                if (sprite.IsPlaneMaster) { //if this is also a plane master
                    sprite.Position = Vector2.Zero; //plane masters should not have a position offset
                    plane.Master = sprite;
                    plane.SetTemporaryRenderTarget(tmpRenderTarget);
                } else { //if not a plane master, draw the sprite to the render target
                    //note we don't draw this to the mouse-map because that's handled when the RenderTarget is used as a source later
                    handle.RenderInRenderTarget(tmpRenderTarget, () => {
                        //draw the sprite centered on the RenderTarget
                        DrawIcon(handle, tmpRenderTarget.Size, sprite, ((worldAABB.Size/2)-sprite.Position)-new Vector2(0.5f,0.5f));
                    }, null);
                }
            } else { //We are no longer dealing with RenderTargets, just regular old planes, so we collect the draw actions for batching
                //if this is a plane master then we don't render it, we just set it as the plane's master
                if (sprite.IsPlaneMaster) {
                    sprite.Position = Vector2.Zero; //plane masters should not have a position offset
                    plane.Master = sprite;

                    continue;
                }

                //add this sprite for rendering
                plane.Sprites.Add(sprite);
            }
        }
    }

    private void DrawPlanes(DrawingHandleWorld handle, Box2 worldAABB) {
        using (var _ = _prof.Group("draw planes map")) {
            handle.RenderInRenderTarget(_baseRenderTarget!, () => {
                foreach (int planeIndex in _planes.Keys.Order()) {
                    var plane = _planes[planeIndex];

                    plane.Draw(this, handle, worldAABB);

                    if (plane.Master != null) {
                        // Don't draw this to the base render target if this itself is a render target
                        if (!string.IsNullOrEmpty(plane.Master.RenderTarget))
                            continue;

                        plane.Master.TextureOverride = plane.RenderTarget.Texture;
                        DrawIcon(handle, _baseRenderTarget!.Size, plane.Master, Vector2.Zero);
                    } else {
                        handle.SetTransform(CreateRenderTargetFlipMatrix(_baseRenderTarget!.Size, Vector2.Zero));
                        handle.DrawTextureRect(plane.RenderTarget.Texture, Box2.FromTwoPoints(Vector2.Zero, _baseRenderTarget.Size));
                    }
                }
            }, null);
        }

        // TODO: Can this only be done once the user clicks?
        using (_prof.Group("draw planes mouse map")) {
            handle.RenderInRenderTarget(_mouseMapRenderTarget!, () => {
                foreach (int planeIndex in _planes.Keys.Order())
                    _planes[planeIndex].DrawMouseMap(handle, this, _mouseMapRenderTarget!.Size, worldAABB);
            }, null);
        }
    }

    private ViewAlgorithm.Tile?[,] CalculateTileVisibility(EntityUid gridUid, MapGridComponent grid, HashSet<EntityUid> entities, TileRef eyeTile, int seeVis) {
        using var _ = _prof.Group("visible turfs");

        var viewRange = _interfaceManager.View;
        if (_tileInfo == null || _tileInfo.GetLength(0) != viewRange.Width + 2 || _tileInfo.GetLength(1) != viewRange.Height + 2) {
            // _tileInfo hasn't been created yet or view range has changed, so create a new array.
            // Leave a 1 tile buffer on each side
            _tileInfo = new ViewAlgorithm.Tile[viewRange.Width + 2, viewRange.Height + 2];
        }

        var eyeWorldPos = _mapSystem.GridTileToWorld(gridUid, grid, eyeTile.GridIndices);
        var tileRefs = _mapSystem.GetTilesEnumerator(gridUid, grid,
            Box2.CenteredAround(eyeWorldPos.Position, new Vector2(_tileInfo.GetLength(0), _tileInfo.GetLength(1))));

        // Gather up all the data the view algorithm needs
        while (tileRefs.MoveNext(out var tileRef)) {
            var delta = tileRef.GridIndices - eyeTile.GridIndices;
            var appearance = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId).Appearance;
            if (appearance == null)
                continue;

            int xIndex = delta.X + viewRange.CenterX;
            int yIndex = delta.Y + viewRange.CenterY;
            if (xIndex < 0 || yIndex < 0 || xIndex >= _tileInfo.GetLength(0) || yIndex >= _tileInfo.GetLength(1))
                continue;

            var tile = new ViewAlgorithm.Tile {
                Opaque = appearance.Opacity,
                Luminosity = 0,
                DeltaX = delta.X,
                DeltaY = delta.Y
            };

            _tileInfo[xIndex, yIndex] = tile;
        }

        // Apply entities' opacity
        foreach (EntityUid entity in entities) {
            // TODO use a sprite tree.
            if (!_spriteQuery.TryGetComponent(entity, out var sprite))
                continue;

            var transform = _xformQuery.GetComponent(entity);
            if (!sprite.IsVisible(transform, seeVis))
                continue;
            if (sprite.Icon.Appearance == null) //appearance hasn't loaded yet
                continue;

            var worldPos = _transformSystem.GetWorldPosition(transform);
            var tilePos = _mapSystem.WorldToTile(gridUid, grid, worldPos) - eyeTile.GridIndices + viewRange.Center;
            if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= _tileInfo.GetLength(0) || tilePos.Y >= _tileInfo.GetLength(1))
                continue;

            var tile = _tileInfo[tilePos.X, tilePos.Y];
            if (tile != null)
                tile.Opaque |= sprite.Icon.Appearance.Opacity;
        }

        ViewAlgorithm.CalculateVisibility(_tileInfo);
        return _tileInfo;
    }

    private void CollectVisibleSprites(ViewAlgorithm.Tile?[,] tiles, EntityUid gridUid, MapGridComponent grid, TileRef eyeTile, HashSet<EntityUid> entities, int seeVis, SightFlags sight, Box2 worldAABB) {
        _spriteContainer.Clear();

        // This exists purely because the tiebreaker var needs to exist somewhere
        // It's set to 0 again before every unique call to ProcessIconComponents
        int tValue;

        // Visible turf sprites
        foreach (var tile in tiles) {
            if (tile == null)
                continue;
            if (tile.IsVisible == false && (sight & SightFlags.SeeTurfs) == 0)
                continue;

            Vector2i tilePos = eyeTile.GridIndices + (tile.DeltaX, tile.DeltaY);
            TileRef tileRef = _mapSystem.GetTileRef(gridUid, grid, tilePos);
            MapCoordinates worldPos = _mapSystem.GridTileToWorld(gridUid, grid, tilePos);

            tValue = 0;
            //pass the turf coords for client.images lookup
            Vector3 turfCoords = new Vector3(tileRef.X, tileRef.Y, (int) worldPos.MapId);
            ProcessIconComponents(_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), worldPos.Position - Vector2.One, EntityUid.Invalid, false, ref tValue, _spriteContainer, turfCoords: turfCoords);
        }

        // Visible entities
        using (var _ = _prof.Group("process entities")) {
            foreach (EntityUid entity in entities) {
                // TODO use a sprite tree.
                if (!_spriteQuery.TryGetComponent(entity, out var sprite))
                    continue;

                var transform = _xformQuery.GetComponent(entity);
                if (!sprite.IsVisible(transform, seeVis))
                    continue;

                var worldPos = _transformSystem.GetWorldPosition(transform);

                // Check for visibility if the eye doesn't have SEE_OBJS or SEE_MOBS
                // TODO: Differentiate between objs and mobs
                if ((sight & (SightFlags.SeeObjs|SightFlags.SeeMobs)) == 0 && _tileInfo != null) {
                    var tilePos = _mapSystem.WorldToTile(gridUid, grid, worldPos) - eyeTile.GridIndices + _interfaceManager.View.Center;
                    if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= _tileInfo.GetLength(0) || tilePos.Y >= _tileInfo.GetLength(1))
                        continue;

                    var tile = tiles[tilePos.X, tilePos.Y];
                    if (tile?.IsVisible is not true)
                        continue;
                }

                tValue = 0;
                ProcessIconComponents(sprite.Icon, worldPos - new Vector2(0.5f), entity, false, ref tValue, _spriteContainer);
            }
        }

        // Screen objects
        if (ScreenOverlayEnabled) {
            using var _ = _prof.Group("screen objects");

            foreach (EntityUid uid in _screenOverlaySystem.ScreenObjects) {
                if (!_entityManager.TryGetComponent(uid, out DMISpriteComponent? sprite) || sprite.ScreenLocation == null)
                    continue;
                if (!sprite.IsVisible(null, seeVis))
                    continue;
                if (sprite.ScreenLocation.MapControl != null) // Don't render screen objects meant for other map controls
                    continue;

                Vector2 position = sprite.ScreenLocation.GetViewPosition(worldAABB.BottomLeft, _interfaceManager.View, EyeManager.PixelsPerMeter);
                Vector2 iconSize = sprite.Icon.DMI == null ? Vector2.Zero : sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;
                for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                    for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                        tValue = 0;
                        ProcessIconComponents(sprite.Icon, position + iconSize * new Vector2(x, y), uid, true, ref tValue, _spriteContainer);
                    }
                }
            }
        }

        using (_prof.Group("sort sprites")) {
            _spriteContainer.Sort();
        }
    }

    private RendererMetaData RentRendererMetaData() {
        RendererMetaData result;
        if (_rendererMetaDataRental.Count == 0)
            result = new RendererMetaData();
        else {
            result = _rendererMetaDataRental.Pop();
            result.Reset();
        }

        _rendererMetaDataToReturn.Push(result);
        return result;
    }

    /// <summary>
    /// Collect all of an icon's keep-together group and render them into one texture.
    /// </summary>
    private Texture ProcessKeepTogether(DrawingHandleWorld handle, RendererMetaData iconMetaData, Vector2i size) {
        //store the parent's transform, color, blend, and alpha - then clear them for drawing to the render target
        Matrix3 ktParentTransform = iconMetaData.TransformToApply;
        Color ktParentColor = iconMetaData.ColorToApply;
        float ktParentAlpha = iconMetaData.AlphaToApply;
        BlendMode ktParentBlendMode = iconMetaData.BlendMode;

        iconMetaData.TransformToApply = Matrix3.Identity;
        iconMetaData.ColorToApply = Color.White;
        iconMetaData.AlphaToApply = 1f;
        iconMetaData.BlendMode = BlendMode.Default;

        List<RendererMetaData> ktItems = new List<RendererMetaData>(iconMetaData.KeepTogetherGroup!.Count+1);
        ktItems.Add(iconMetaData);
        ktItems.AddRange(iconMetaData.KeepTogetherGroup);
        iconMetaData.KeepTogetherGroup.Clear();

        ktItems.Sort();
        //draw it onto an additional render target that we can return immediately for correction of transform
        IRenderTexture tempTexture = RentRenderTarget(size);
        ClearRenderTarget(tempTexture, handle, Color.Transparent);

        handle.RenderInRenderTarget(tempTexture, () => {
            foreach (RendererMetaData ktItem in ktItems) {
                DrawIcon(handle, tempTexture.Size, ktItem, -ktItem.Position);
            }
        }, null);

        //but keep the handle to the final KT group's render target so we don't override it later in the render cycle
        IRenderTexture ktTexture = RentRenderTarget(tempTexture.Size);
        handle.RenderInRenderTarget(ktTexture, () => {
            handle.SetTransform(CreateRenderTargetFlipMatrix(tempTexture.Size, Vector2.Zero));
            handle.DrawTextureRect(tempTexture.Texture, new Box2(Vector2.Zero, tempTexture.Size));
        }, Color.Transparent);

        _renderTargetsToReturn.Push(tempTexture);

        //now restore the original color, alpha, blend, and transform so they can be applied to the render target as a whole
        iconMetaData.TransformToApply = ktParentTransform;
        iconMetaData.ColorToApply = ktParentColor;
        iconMetaData.AlphaToApply = ktParentAlpha;
        iconMetaData.BlendMode = ktParentBlendMode;

        _renderTargetsToReturn.Push(ktTexture);
        return ktTexture.Texture;
    }

    /// <summary>
    /// Render a texture without applying any filters, making this faster and cheaper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawIconFast(DrawingHandleWorld handle, Vector2i renderTargetSize, Texture texture, Vector2 pos, ShaderInstance? shader) {
        handle.UseShader(shader);
        handle.SetTransform(CreateRenderTargetFlipMatrix(renderTargetSize, pos));
        handle.DrawTextureRect(texture, Box2.FromDimensions(Vector2.Zero, texture.Size));
    }

    /// <summary>
    /// A slower method of drawing an icon. This one renders an atom's filters.
    /// Use <see cref="DrawIconFast"/> instead if the icon has no special rendering needs.
    /// </summary>
    private void DrawIconSlow(DrawingHandleWorld handle, Texture frame, RendererMetaData iconMetaData, Vector2i renderTargetSize, Vector2 pos) {
        //first we do ping pong rendering for the multiple filters
        // TODO: This should determine the size from the filters and their settings, not just double the original
        IRenderTexture ping = RentRenderTarget(frame.Size * 2);
        IRenderTexture pong = RentRenderTarget(ping.Size);

        //we can use the color matrix shader here, since we don't need to blend
        //also because blend mode is none, we don't need to clear
        var colorMatrix = iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity)
            ? new ColorMatrix(iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply))
            : iconMetaData.ColorMatrixToApply;

        ShaderInstance colorShader = _colorInstance.Duplicate();
        colorShader.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
        colorShader.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
        colorShader.SetParameter("isPlaneMaster",iconMetaData.IsPlaneMaster);
        handle.UseShader(colorShader);
        handle.RenderInRenderTarget(pong, () => {
            handle.SetTransform(CreateRenderTargetFlipMatrix(pong.Size, frame.Size / 2));
            handle.DrawTextureRect(frame, new Box2(Vector2.Zero, frame.Size));
        }, Color.Black.WithAlpha(0));

        foreach (DreamFilter filterId in iconMetaData.MainIcon!.Appearance!.Filters) {
            ShaderInstance s = _appearanceSystem.GetFilterShader(filterId, RenderSourceLookup);

            handle.UseShader(s); // Set the shader out here to avoid a closure alloc
            handle.RenderInRenderTarget(ping, () => {
                // Technically this should be ping.Size, but they are the same size so avoid the extra closure alloc
                var transform = CreateRenderTargetFlipMatrix(pong.Size, Vector2.Zero);

                handle.SetTransform(transform);
                handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, pong.Size));
            }, Color.Black.WithAlpha(0));

            (ping, pong) = (pong, ping);
        }

        //then we draw the actual icon with filters applied
        DrawIconFast(handle, renderTargetSize, pong.Texture, pos - frame.Size / 2,
            //note we apply the color *before* the filters, so we ignore color here
            GetBlendAndColorShader(iconMetaData, ignoreColor: true)
        );

        ReturnRenderTarget(ping);
        _renderTargetsToReturn.Push(pong);
    }

    /// <summary>
    /// Creates a transformation matrix that counteracts RT's
    /// <see cref="DrawingHandleBase.RenderInRenderTarget(IRenderTarget,Action,System.Nullable{Robust.Shared.Maths.Color})"/> quirks
    /// <br/>
    /// If you are using render targets, you will almost certainly want to use this
    /// </summary>
    /// <param name="renderTargetSize">Size of the render target</param>
    /// <param name="renderPosition">The translation to draw the icon at</param>
    /// <remarks>Due to RT applying transformations out of order, render the icon at Vector2.Zero</remarks>
    public static Matrix3 CreateRenderTargetFlipMatrix(Vector2i renderTargetSize, Vector2 renderPosition) {
        // RT flips the texture when doing a RenderInRenderTarget(), so we use _flipMatrix to reverse it
        // We must also handle translations here, since RT applies its own transform in an unexpected order
        return FlipMatrix * Matrix3.CreateTranslation(renderPosition.X, renderTargetSize.Y - renderPosition.Y);
    }
}

internal sealed class RendererMetaData : IComparable<RendererMetaData> {
    public DreamIcon? MainIcon;
    public Vector2 Position;
    public int Plane; //true plane value may be different from appearance plane value, due to special flags
    public float Layer; //ditto for layer
    public EntityUid Uid;
    public EntityUid ClickUid; //the UID of the object clicks on this should be passed to (ie, for overlays)
    public bool IsScreen;
    public int TieBreaker; //Used for biasing render order (ie, for overlays)
    public Color ColorToApply;
    public ColorMatrix ColorMatrixToApply;
    public float AlphaToApply;
    public Matrix3 TransformToApply;
    public string? RenderSource;
    public string? RenderTarget;
    public List<RendererMetaData>? KeepTogetherGroup;
    public AppearanceFlags AppearanceFlags;
    public BlendMode BlendMode;
    public MouseOpacity MouseOpacity;
    public Texture? TextureOverride;

    public Texture? Texture => TextureOverride ?? MainIcon?.CurrentFrame;
    public bool IsPlaneMaster => (AppearanceFlags & AppearanceFlags.PlaneMaster) != 0;
    public bool HasRenderSource => !string.IsNullOrEmpty(RenderSource);
    public bool ShouldPassMouse => HasRenderSource && (AppearanceFlags & AppearanceFlags.PassMouse) != 0;

    public RendererMetaData() {
        Reset();
    }

    public void Reset() {
        MainIcon = null;
        Position = Vector2.Zero;
        Plane = 0;
        Layer = 0;
        Uid = EntityUid.Invalid;
        ClickUid = EntityUid.Invalid;
        IsScreen = false;
        TieBreaker = 0;
        ColorToApply = Color.White;
        ColorMatrixToApply = ColorMatrix.Identity;
        AlphaToApply = 1.0f;
        TransformToApply = Matrix3.Identity;
        RenderSource = "";
        RenderTarget = "";
        KeepTogetherGroup = null; //don't actually need to allocate this 90% of the time
        AppearanceFlags = AppearanceFlags.None;
        BlendMode = BlendMode.Default;
        MouseOpacity = MouseOpacity.Transparent;
        TextureOverride = null;
    }

    public int CompareTo(RendererMetaData? other) {
        if (other == null)
            return 1;

        //Render target and source ordering is done first.
        //Anything with a render target goes first
        int val = (!string.IsNullOrEmpty(RenderTarget)).CompareTo(!string.IsNullOrEmpty(other.RenderTarget));
        if (val != 0) {
            return -val;
        }

        //Anything with a render source which points to a render target must come *after* that render_target
        if (HasRenderSource && RenderSource == other.RenderTarget) {
            return 1;
        }

        //We now return to your regularly scheduled sprite render order

        //Plane
        val =  Plane.CompareTo(other.Plane);
        if (val != 0) {
            return val;
        }

        //Plane master objects go first for any given plane
        val = IsPlaneMaster.CompareTo(IsPlaneMaster);
        if (val != 0) {
            return -val; //sign flip because we want 1 < -1
        }

        //sub-plane (ie, HUD vs not HUD)
        val = IsScreen.CompareTo(other.IsScreen);
        if (val != 0) {
            return val;
        }

        //depending on world.map_format, either layer or physical position
        //TODO
        val = Layer.CompareTo(other.Layer);
        if (val != 0) {
            return val;
        }

        //despite assurances to the contrary by the DM Ref, position is in fact used for draw order in topdown mode
        val = Position.X.CompareTo(other.Position.X);
        if (val != 0) {
            return val;
        }

        val = Position.Y.CompareTo(other.Position.Y);
        if (val != 0) {
            return -val;
        }

        //Finally, tie-breaker - in BYOND, this is order of creation of the sprites
        //for us, we use EntityUID, with a tie-breaker (for underlays/overlays)
        val = Uid.CompareTo(other.Uid);
        if (val != 0) {
            return val;
        }

        //FLOAT_LAYER must be sorted local to the thing they're floating on, and since all overlays/underlays share their parent's UID, we
        //can do that here.
        if (MainIcon?.Appearance?.Layer < 0 && other.MainIcon?.Appearance?.Layer < 0) { //if these are FLOAT_LAYER, sort amongst them
            val = MainIcon.Appearance.Layer.CompareTo(other.MainIcon.Appearance.Layer);
            if (val != 0) {
                return val;
            }
        }

        return TieBreaker.CompareTo(other.TieBreaker);
    }
}

#region Render Toggle Commands
public sealed class ToggleScreenOverlayCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "togglescreenoverlay";
    public string Description => "Toggle rendering of screen objects";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.ScreenOverlayEnabled = !screenOverlay.ScreenOverlayEnabled;
        }
    }
}

public sealed class ToggleMouseOverlayCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "togglemouseoverlay";
    public string Description => "Toggle rendering of mouse click area for screen objects";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.MouseMapRenderEnabled = !screenOverlay.MouseMapRenderEnabled;
        }
    }
}
#endregion
