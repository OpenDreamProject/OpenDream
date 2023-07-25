﻿using System.Linq;
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

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering world atoms
/// </summary>
internal sealed class DreamViewOverlay : Overlay {
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public bool ScreenOverlayEnabled = true;
    public bool MouseMapRenderEnabled;

    public Texture? MouseMap => _mouseMapRenderTarget?.Texture;
    public readonly Dictionary<Color, RendererMetaData> MouseMapLookup = new();

    private const LookupFlags MapLookupFlags = LookupFlags.Approximate | LookupFlags.Uncontained;

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ProfManager _prof = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.view");

    private readonly TransformSystem _transformSystem;
    private readonly EntityLookupSystem _lookupSystem;
    private readonly ClientAppearanceSystem _appearanceSystem;
    private readonly ClientScreenOverlaySystem _screenOverlaySystem;

    private readonly EntityQuery<DMISpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<DreamMobSightComponent> _mobSightQuery;

    private readonly Dictionary<int, DreamPlane> _planes = new();
    private readonly List<RendererMetaData> _spriteContainer = new();

    private readonly ShaderInstance _blockColorInstance;
    private readonly ShaderInstance _colorInstance;
    private readonly Dictionary<BlendMode, ShaderInstance> _blendModeInstances;

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();

    private IRenderTexture? _mouseMapRenderTarget;
    private IRenderTexture? _baseRenderTarget;
    private readonly Dictionary<string, IRenderTexture> _renderSourceLookup = new();
    private readonly Stack<IRenderTexture> _renderTargetsToReturn = new();
    private readonly Stack<RendererMetaData> _rendererMetaDataRental = new();
    private readonly Stack<RendererMetaData> _rendererMetaDataToReturn = new();
    private readonly Matrix3 _flipMatrix;

    // Hardcoded for a 15x15 view (with 1 tile buffer on each side)
    // Defined here so it isn't recreated every frame
    private readonly ViewAlgorithm.Tile?[,] _tileInfo = new ViewAlgorithm.Tile?[17,17];

    public DreamViewOverlay(TransformSystem transformSystem, EntityLookupSystem lookupSystem,
        ClientAppearanceSystem appearanceSystem, ClientScreenOverlaySystem screenOverlaySystem) {
        IoCManager.InjectDependencies(this);
        _transformSystem = transformSystem;
        _lookupSystem = lookupSystem;
        _appearanceSystem = appearanceSystem;
        _screenOverlaySystem = screenOverlaySystem;

        _spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        _xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        _mobSightQuery = _entityManager.GetEntityQuery<DreamMobSightComponent>();

        _sawmill.Debug("Loading shaders...");
        _blockColorInstance = _protoManager.Index<ShaderPrototype>("blockcolor").InstanceUnique();
        _colorInstance = _protoManager.Index<ShaderPrototype>("color").InstanceUnique();
        _blendModeInstances = new(6) {
            {BlendMode.Default, _protoManager.Index<ShaderPrototype>("blend_overlay").InstanceUnique()}, //BLEND_DEFAULT
            {BlendMode.Overlay, _protoManager.Index<ShaderPrototype>("blend_overlay").InstanceUnique()}, //BLEND_OVERLAY (same as BLEND_DEFAULT)
            {BlendMode.Add, _protoManager.Index<ShaderPrototype>("blend_add").InstanceUnique()}, //BLEND_ADD
            {BlendMode.Subtract, _protoManager.Index<ShaderPrototype>("blend_subtract").InstanceUnique()}, //BLEND_SUBTRACT
            {BlendMode.Multiply, _protoManager.Index<ShaderPrototype>("blend_multiply").InstanceUnique()}, //BLEND_MULTIPLY
            {BlendMode.InsertOverlay, _protoManager.Index<ShaderPrototype>("blend_inset_overlay").InstanceUnique()} //BLEND_INSET_OVERLAY //TODO
        };

        _flipMatrix = Matrix3.Identity;
        _flipMatrix.R1C1 = -1;
    }

    protected override void Draw(in OverlayDrawArgs args) {
        using var _ = _prof.Group("Dream View Overlay");

        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null)
            return;

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));

        //Main drawing of sprites happens here
        try {
            var viewportSize = (Vector2i)(args.Viewport.Size / args.Viewport.RenderScale);

            DrawAll(args, eye.Value, viewportSize);
        } catch (Exception e) {
            _sawmill.Error($"Error occurred while rendering frame. Error details:\n{e.Message}\n{e.StackTrace}");
        }

        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();

        _renderSourceLookup.Clear();

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
        if (!_mapManager.TryFindGridAt(eyeTransform.MapPosition, out _, out var grid))
            return;

        _mobSightQuery.TryGetComponent(eye, out var mobSight);
        var seeVis = mobSight?.SeeInvisibility ?? 127;
        var sight = mobSight?.Sight ?? 0;

        var worldHandle = args.WorldHandle;

        HashSet<EntityUid> entities;
        using (_prof.Group("lookup")) {
            //TODO use a sprite tree.
            //the scaling is to attempt to prevent pop-in, by rendering sprites that are *just* offscreen
            entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, args.WorldAABB.Scale(1.2f), MapLookupFlags);
        }

        var eyeTile = grid.GetTileRef(eyeTransform.MapPosition);
        var tiles = CalculateTileVisibility(grid, entities, eyeTile, seeVis);

        RefreshRenderTargets(args.WorldHandle, viewportSize);

        CollectVisibleSprites(tiles, grid, eyeTile, entities, seeVis, sight, args.WorldAABB);
        ClearPlanes();
        ProcessSprites(worldHandle, viewportSize, args.WorldAABB);

        //Final draw
        //At this point all the sprites have been rendered to the base target, now we just draw it to the viewport!
        DrawPlanes(worldHandle);
        worldHandle.DrawTexture(
            MouseMapRenderEnabled ? _mouseMapRenderTarget!.Texture : _baseRenderTarget!.Texture,
            new Vector2(args.WorldAABB.Left, args.WorldAABB.Bottom * -1));
    }

    //handles underlays, overlays, appearance flags, images. Adds them to the result list, so they can be sorted and drawn with DrawIcon()
    private void ProcessIconComponents(DreamIcon icon, Vector2 position, EntityUid uid, bool isScreen, ref int tieBreaker, List<RendererMetaData> result, RendererMetaData? parentIcon = null, bool keepTogether = false) {
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
                current.ColorToApply = parentIcon.ColorToApply;
                current.ColorMatrixToApply = parentIcon.ColorMatrixToApply;
            }

            if ((icon.Appearance.AppearanceFlags & AppearanceFlags.ResetAlpha) != 0 || keepTogether) //RESET_ALPHA
                current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            else
                current.AlphaToApply = parentIcon.AlphaToApply;

            if ((icon.Appearance.AppearanceFlags & AppearanceFlags.ResetTransform) != 0 || keepTogether) //RESET_TRANSFORM
                current.TransformToApply = iconAppearanceTransformMatrix;
            else
                current.TransformToApply = parentIcon.TransformToApply;

            if ((icon.Appearance.Plane < -10000)) //FLOAT_PLANE - Note: yes, this really is how it works. Yes it's dumb as shit.
                current.Plane = parentIcon.Plane + (icon.Appearance.Plane + 32767);
            else
                current.Plane = icon.Appearance.Plane;

            if (icon.Appearance.Layer < 0) //FLOAT_LAYER
                current.Layer = parentIcon.Layer;
            else
                current.Layer = icon.Appearance.Layer;
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
            result.Add(renderTargetPlaceholder);
        }

        //TODO check for images with override here
        /*foreach (image in client.images) {
            if (image.override && image.location == icon.owner)
                current.MainIcon = image
            else
                add like overlays?
        }*/

        //TODO vis_contents
        //click uid should be set to current.uid again
        //dont forget the vis_flags

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

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //flatten KeepTogetherGroup. Done here so we get implicit recursive iteration down the tree.
        if (current.KeepTogetherGroup != null && current.KeepTogetherGroup.Count > 0) {
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

            _renderTargetCache[size] = listResult; //put the shorter list back
        }

        return result;
    }

    private void ReturnRenderTarget(IRenderTexture rental) {
        if (!_renderTargetCache.TryGetValue(rental.Size, out var storeList))
            storeList = new List<IRenderTexture>(4);

        storeList.Add(rental);
        _renderTargetCache[rental.Size] = storeList;
    }

    private void ClearRenderTarget(IRenderTexture target, DrawingHandleWorld handle, Color clearColor) {
        handle.RenderInRenderTarget(target, () => {}, clearColor);
    }

    private ShaderInstance GetBlendAndColorShader(RendererMetaData iconMetaData, Color? colorOverride = null) {
        Color rgba = colorOverride ?? iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply);

        ColorMatrix colorMatrix;
        if (colorOverride != null || iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity))
            colorMatrix = new ColorMatrix(rgba);
        else
            colorMatrix = iconMetaData.ColorMatrixToApply;

        if (!_blendModeInstances.TryGetValue(iconMetaData.BlendMode, out var blendAndColor))
            blendAndColor = _blendModeInstances[BlendMode.Default];

        blendAndColor = blendAndColor.Duplicate();
        blendAndColor.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
        blendAndColor.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
        blendAndColor.SetParameter("isPlaneMaster", iconMetaData.IsPlaneMaster);
        return blendAndColor;
    }

    private (Action, Action) DrawIconAction(DrawingHandleWorld handle, RendererMetaData iconMetaData, Vector2 positionOffset, Texture? textureOverride = null) {
        DreamIcon? icon = iconMetaData.MainIcon;
        if (icon == null)
            return (() => {}, () => {});

        Vector2 position = iconMetaData.Position + positionOffset;
        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;

        Texture? frame;
        if (textureOverride != null) {
            frame = textureOverride;

            //we flip this because GL's coordinate system is bottom-left first, and so render target textures are upside down
            iconMetaData.TransformToApply *= _flipMatrix;
        } else {
            frame = icon.CurrentFrame;
        }

        //KEEP_TOGETHER groups
        if (iconMetaData.KeepTogetherGroup != null && iconMetaData.KeepTogetherGroup.Count > 0) {
            //store the parent's transform, color, blend, and alpha - then clear them for drawing to the render target
            Matrix3 ktParentTransform = iconMetaData.TransformToApply;
            Color ktParentColor = iconMetaData.ColorToApply;
            float ktParentAlpha = iconMetaData.AlphaToApply;
            BlendMode ktParentBlendMode = iconMetaData.BlendMode;

            iconMetaData.TransformToApply = Matrix3.Identity;
            iconMetaData.ColorToApply = Color.White;
            iconMetaData.AlphaToApply = 1f;
            iconMetaData.BlendMode = BlendMode.Default;

            List<RendererMetaData> ktItems = new List<RendererMetaData>(iconMetaData.KeepTogetherGroup.Count+1);
            ktItems.Add(iconMetaData);
            ktItems.AddRange(iconMetaData.KeepTogetherGroup);
            iconMetaData.KeepTogetherGroup.Clear();

            ktItems.Sort();
            //draw it onto an additional render target that we can return immediately for correction of transform
            // TODO: Use something better than a hardcoded 64x64 fallback
            IRenderTexture tempTexture = RentRenderTarget(frame?.Size ?? (64,64));
            ClearRenderTarget(tempTexture, handle, Color.Transparent);

            foreach (RendererMetaData ktItem in ktItems) {
                DrawIconNow(handle, tempTexture, ktItem, -ktItem.Position);
            }

            //but keep the handle to the final KT group's render target so we don't override it later in the render cycle
            IRenderTexture ktTexture = RentRenderTarget(tempTexture.Size);
            handle.RenderInRenderTarget(ktTexture, () => {
                handle.DrawRect(new Box2(Vector2.Zero, tempTexture.Size), new Color());
                handle.DrawTextureRect(tempTexture.Texture, new Box2(Vector2.Zero, tempTexture.Size));
            }, Color.Transparent);

            frame = ktTexture.Texture;
            _renderTargetsToReturn.Push(tempTexture);

            //now restore the original color, alpha, blend, and transform so they can be applied to the render target as a whole
            iconMetaData.TransformToApply = ktParentTransform;
            iconMetaData.ColorToApply = ktParentColor;
            iconMetaData.AlphaToApply = ktParentAlpha;
            iconMetaData.BlendMode = ktParentBlendMode;

            _renderTargetsToReturn.Push(ktTexture);
        }

        //if frame is still null, this doesn't require a draw, so return NOP
        if (frame == null)
            return (()=>{},()=>{});

        Action iconDrawAction;
        Action mouseMapDrawAction;

        //setup the MouseMapLookup shader for use in DrawIcon()
        byte[] rgba = BitConverter.GetBytes(iconMetaData.GetHashCode());
        Color targetColor = new Color(rgba[0], rgba[1], rgba[2]); //TODO - this could result in mis-clicks due to hash-collision since we ditch a whole byte.
        MouseMapLookup[targetColor] = iconMetaData;

        Matrix3 tmpTranslation = Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, un-translate
                                 iconMetaData.TransformToApply *
                                 Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2));
        Box2 drawBounds = new Box2(pixelPosition, pixelPosition+frame.Size);

        //go fast when the only filter is color, and we don't have more color things to consider
        bool goFastOverride = false;
        if (icon.Appearance != null && iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity) && iconMetaData.ColorToApply == Color.White && iconMetaData.AlphaToApply == 1.0f && icon.Appearance.Filters.Count == 1 && icon.Appearance.Filters[0].FilterType == "color") {
            DreamFilterColor colorFilter = (DreamFilterColor)icon.Appearance.Filters[0];
            iconMetaData.ColorMatrixToApply = colorFilter.Color;
            goFastOverride = true;
        }

        if (goFastOverride || icon.Appearance == null || icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            iconDrawAction = () => {
                handle.UseShader(GetBlendAndColorShader(iconMetaData));
                handle.SetTransform(tmpTranslation);
                handle.DrawTextureRect(frame, drawBounds);
                handle.UseShader(null);
            };

            if (iconMetaData.MouseOpacity != MouseOpacity.Transparent) {
                mouseMapDrawAction = () => {
                    handle.UseShader(_blockColorInstance);
                    handle.SetTransform(tmpTranslation);
                    handle.DrawTextureRect(frame,
                        drawBounds,
                        targetColor);
                    handle.UseShader(null);
                };
            } else {
                mouseMapDrawAction = () => {};
            }

            return (iconDrawAction, mouseMapDrawAction);
        } else { //Slower path for filtered icons
            //first we do ping pong rendering for the multiple filters
            // TODO: This should determine the size from the filters and their settings, not just double the original
            IRenderTexture ping = RentRenderTarget(frame.Size * 2);
            IRenderTexture pong = RentRenderTarget(frame.Size * 2);

            handle.RenderInRenderTarget(pong, () => {
                //we can use the color matrix shader here, since we don't need to blend
                //also because blend mode is none, we don't need to clear
                ColorMatrix colorMatrix;
                if (iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity))
                    colorMatrix = new ColorMatrix(iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                else
                    colorMatrix = iconMetaData.ColorMatrixToApply;

                ShaderInstance colorShader = _colorInstance.Duplicate();
                colorShader.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
                colorShader.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
                colorShader.SetParameter("isPlaneMaster",iconMetaData.IsPlaneMaster);
                handle.UseShader(colorShader);

                handle.DrawTextureRect(frame,
                    new Box2(Vector2.Zero + (frame.Size / 2), frame.Size + (frame.Size / 2)));
                handle.UseShader(null);
            }, Color.Black.WithAlpha(0));

            foreach (DreamFilter filterId in icon.Appearance.Filters) {
                ShaderInstance s = _appearanceSystem.GetFilterShader(filterId, _renderSourceLookup);

                handle.RenderInRenderTarget(ping, () => {
                    handle.UseShader(s);
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                    handle.UseShader(null);
                }, Color.Black.WithAlpha(0));

                (ping, pong) = (pong, ping);
            }

            if (icon.Appearance?.Filters.Count % 2 == 0) //if we have an even number of filters, we need to flip
                tmpTranslation = Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, un-translate
                                    iconMetaData.TransformToApply * _flipMatrix *
                                    Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2));

            //then we return the Action that draws the actual icon with filters applied
            iconDrawAction = () => {
                //note we apply the color *before* the filters, so we use override here
                handle.UseShader(GetBlendAndColorShader(iconMetaData, colorOverride: Color.White));

                handle.SetTransform(tmpTranslation);
                handle.DrawTextureRect(pong.Texture,
                    new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)));
                handle.UseShader(null);
            };

            if (iconMetaData.MouseOpacity != MouseOpacity.Transparent) {
                mouseMapDrawAction = () => {
                    handle.UseShader(_blockColorInstance);
                    handle.SetTransform(tmpTranslation);
                    handle.DrawTextureRect(pong.Texture,
                        new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                        targetColor);
                    handle.UseShader(null);
                };
            } else {
                mouseMapDrawAction = () => {};
            }

            ReturnRenderTarget(ping);
            _renderTargetsToReturn.Push(pong);
            return (iconDrawAction, mouseMapDrawAction);
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
                plane.RenderTarget.Dispose();
                plane.RenderTarget = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
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
            if (plane.IconDrawActions.Count == 0 && plane.MouseMapDrawActions.Count == 0) {
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
        return plane;
    }

    private void ProcessSprites(DrawingHandleWorld handle, Vector2i viewportSize, Box2 worldAABB) {
        using var _ = _prof.Group("process sprites / draw render targets");

        //all sprites with render targets get handled first - these are ordered by sprites.Sort(), so we can just iterate normally
        foreach (var sprite in _spriteContainer) {
            var plane = GetPlane(sprite.Plane, viewportSize);

            if (!string.IsNullOrEmpty(sprite.RenderTarget)) {
                //if this sprite has a render target, draw it to a slate instead. If it needs to be drawn on the map, a second sprite instance will already have been created for that purpose
                if (!_renderSourceLookup.TryGetValue(sprite.RenderTarget, out var tmpRenderTarget)) {
                    tmpRenderTarget = RentRenderTarget(viewportSize);
                    ClearRenderTarget(tmpRenderTarget, handle, new Color());
                    _renderSourceLookup.Add(sprite.RenderTarget, tmpRenderTarget);
                    _renderTargetsToReturn.Push(tmpRenderTarget);
                }

                if (sprite.IsPlaneMaster) { //if this is also a plane master
                    plane.Master = sprite;
                } else { //if not a plane master, draw the sprite to the render target
                    //note we don't draw this to the mouse-map because that's handled when the RenderTarget is used as a source later
                    DrawIconNow(handle, tmpRenderTarget, sprite, ((worldAABB.Size/2)-sprite.Position)-new Vector2(0.5f,0.5f), null, true); //draw the sprite centered on the RenderTarget
                }
            } else { //We are no longer dealing with RenderTargets, just regular old planes, so we collect the draw actions for batching
                //if this is a plane master then we don't render it, we just set it as the plane's master
                if (sprite.IsPlaneMaster) {
                    sprite.Position = Vector2.Zero; //plane masters should not have a position offset

                    plane.Master = sprite;
                    continue;
                }

                //add this sprite for rendering
                (Action,Action) drawActions;
                if (!string.IsNullOrEmpty(sprite.RenderSource) && _renderSourceLookup.TryGetValue(sprite.RenderSource, out var renderSourceTexture)) {
                    drawActions = DrawIconAction(handle, sprite, (-worldAABB.BottomLeft)-(worldAABB.Size/2)+new Vector2(0.5f,0.5f), renderSourceTexture.Texture);
                } else {
                    drawActions = DrawIconAction(handle, sprite, -worldAABB.BottomLeft);
                }

                plane.IconDrawActions.Add(drawActions.Item1);
                plane.MouseMapDrawActions.Add(drawActions.Item2);
            }
        }
    }

    private void DrawPlanes(DrawingHandleWorld handle) {
        if (!MouseMapRenderEnabled) { // No need to render the map if we're drawing the mouse map over it
            using var _ = _prof.Group("draw planes map");

            handle.RenderInRenderTarget(_baseRenderTarget!, () => {
                foreach (int planeIndex in _planes.Keys.Order()) {
                    var plane = _planes[planeIndex];

                    plane.Draw(handle);

                    if (plane.Master != null) {
                        DrawIconNow(handle, null, plane.Master, Vector2.Zero, plane.RenderTarget.Texture, noMouseMap: true);
                    } else {
                        var renderBox = new Box2(
                            new Vector2(0, -_baseRenderTarget!.Size.Y),
                            new Vector2(_baseRenderTarget.Size.X, 0)
                        );

                        handle.SetTransform(_flipMatrix);
                        handle.DrawTextureRect(plane.RenderTarget.Texture, renderBox);
                    }
                }
            }, null);
        }

        using (_prof.Group("draw planes mouse map")) {
            handle.RenderInRenderTarget(_mouseMapRenderTarget!, () => {
                foreach (int planeIndex in _planes.Keys.Order())
                    _planes[planeIndex].DrawMouseMap();
            }, null);
        }
    }

    private ViewAlgorithm.Tile?[,] CalculateTileVisibility(MapGridComponent grid, HashSet<EntityUid> entities, TileRef eyeTile, int seeVis) {
        using var _ = _prof.Group("visible turfs");

        var eyeWorldPos = grid.GridTileToWorld(eyeTile.GridIndices);
        var tileRefs = grid.GetTilesIntersecting(Box2.CenteredAround(eyeWorldPos.Position, new Vector2(17, 17)));

        // Gather up all the data the view algorithm needs
        foreach (TileRef tileRef in tileRefs) {
            var delta = tileRef.GridIndices - eyeTile.GridIndices;
            var appearance = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId).Appearance;
            if (appearance == null)
                continue;

            var tile = new ViewAlgorithm.Tile {
                Opaque = appearance.Opacity,
                Luminosity = 0,
                DeltaX = delta.X,
                DeltaY = delta.Y
            };

            _tileInfo[delta.X + 8, delta.Y + 8] = tile;
        }

        // Apply entities' opacity
        foreach (EntityUid entity in entities) {
            // TODO use a sprite tree.
            if (!_spriteQuery.TryGetComponent(entity, out var sprite))
                continue;
            if (!sprite.IsVisible(seeInvis: seeVis))
                continue;
            if (sprite.Icon.Appearance == null) //appearance hasn't loaded yet
                continue;

            var worldPos = _transformSystem.GetWorldPosition(entity, _xformQuery);
            var tilePos = grid.WorldToTile(worldPos) - eyeTile.GridIndices + 8;
            if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= 17 || tilePos.Y >= 17)
                continue;

            var tile = _tileInfo[tilePos.X, tilePos.Y];
            if (tile != null)
                tile.Opaque |= sprite.Icon.Appearance.Opacity;
        }

        ViewAlgorithm.CalculateVisibility(_tileInfo);
        return _tileInfo;
    }

    private void CollectVisibleSprites(ViewAlgorithm.Tile?[,] tiles, MapGridComponent grid, TileRef eyeTile, HashSet<EntityUid> entities, int seeVis, SightFlags sight, Box2 worldAABB) {
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
            TileRef tileRef = grid.GetTileRef(tilePos);
            MapCoordinates worldPos = grid.GridTileToWorld(tilePos);

            tValue = 0;
            ProcessIconComponents(_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), worldPos.Position - Vector2.One, EntityUid.Invalid, false, ref tValue, _spriteContainer);
        }

        // Visible entities
        using (var _ = _prof.Group("process entities")) {
            foreach (EntityUid entity in entities) {
                // TODO use a sprite tree.
                if (!_spriteQuery.TryGetComponent(entity, out var sprite))
                    continue;
                if (!sprite.IsVisible(seeInvis: seeVis))
                    continue;

                var worldPos = _transformSystem.GetWorldPosition(entity, _xformQuery);

                // Check for visibility if the eye doesn't have SEE_OBJS or SEE_MOBS
                // TODO: Differentiate between objs and mobs
                if ((sight & (SightFlags.SeeObjs|SightFlags.SeeMobs)) == 0) {
                    var tilePos = grid.WorldToTile(worldPos) - eyeTile.GridIndices + 8;
                    if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= 17 || tilePos.Y >= 17)
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
                if (!sprite.IsVisible(checkWorld: false, seeInvis: seeVis))
                    continue;
                if (sprite.ScreenLocation.MapControl != null) // Don't render screen objects meant for other map controls
                    continue;

                Vector2 position = sprite.ScreenLocation.GetViewPosition(worldAABB.BottomLeft , EyeManager.PixelsPerMeter);
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

    private void DrawIconNow(DrawingHandleWorld handle, IRenderTarget? renderTarget, RendererMetaData iconMetaData, Vector2 positionOffset, Texture? textureOverride = null, bool noMouseMap = false) {
        (Action iconDrawAction, Action mouseMapDrawAction) = DrawIconAction(handle, iconMetaData, positionOffset, textureOverride);

        if (renderTarget != null) {
            handle.RenderInRenderTarget(renderTarget, iconDrawAction, null);
        } else {
            iconDrawAction();
        }

        //action should be NOP if this is transparent, but save a RiRT call anyway since we can
        if (!(noMouseMap || iconMetaData.MouseOpacity != MouseOpacity.Transparent)) {
            handle.RenderInRenderTarget(_mouseMapRenderTarget!, mouseMapDrawAction, null);
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

    public bool IsPlaneMaster => (AppearanceFlags & AppearanceFlags.PlaneMaster) != 0;

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
        if (!string.IsNullOrEmpty(RenderSource) && RenderSource == other.RenderTarget) {
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
