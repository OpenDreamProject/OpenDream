using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using OpenDreamShared.Rendering;
using Robust.Shared.Profiling;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering world atoms
/// </summary>
sealed class DreamViewOverlay : Overlay {
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly ProfManager _prof = default!;
    private EntityQuery<DMISpriteComponent> spriteQuery;
    private EntityQuery<TransformComponent> xformQuery;
    private EntityQuery<DreamMobSightComponent> mobSightQuery;
    private ShaderInstance _blockColorInstance;
    private ShaderInstance _colorInstance;
    private Dictionary<BlendMode, ShaderInstance> _blendmodeInstances;

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();
    private EntityLookupSystem _lookupSystem;
    private ClientAppearanceSystem _appearanceSystem;
    private ClientScreenOverlaySystem _screenOverlaySystem;
    private SharedTransformSystem _transformSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    public bool ScreenOverlayEnabled = true;
    public bool RenderTurfEnabled = true;
    public bool RenderEntityEnabled = true;
    public bool RenderPlayerEnabled = true;
    public bool MouseMapRenderEnabled = false;
    private IRenderTexture _mouseMapRenderTarget;
    public Texture MouseMap;
    public Dictionary<Color, RendererMetaData> MouseMapLookup = new();
    private Dictionary<String, IRenderTexture> _renderSourceLookup = new();
    private Stack<IRenderTexture> _renderTargetsToReturn = new();
    private Stack<RendererMetaData> _rendererMetaDataRental = new();
    private Stack<RendererMetaData> _rendererMetaDataToReturn = new();
    private Matrix3 _flipMatrix;
    private const LookupFlags MapLookupFlags = LookupFlags.Approximate | LookupFlags.Uncontained;

    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
        Logger.Debug("Loading shaders...");
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        _blockColorInstance = protoManager.Index<ShaderPrototype>("blockcolor").InstanceUnique();
        _colorInstance = protoManager.Index<ShaderPrototype>("color").InstanceUnique();
        _blendmodeInstances = new(4);
        _blendmodeInstances.Add(BlendMode.BLEND_DEFAULT, protoManager.Index<ShaderPrototype>("blend_overlay").InstanceUnique()); //BLEND_DEFAULT
        _blendmodeInstances.Add(BlendMode.BLEND_OVERLAY, protoManager.Index<ShaderPrototype>("blend_overlay").InstanceUnique()); //BLEND_OVERLAY (same as BLEND_DEFAULT)
        _blendmodeInstances.Add(BlendMode.BLEND_ADD, protoManager.Index<ShaderPrototype>("blend_add").InstanceUnique()); //BLEND_ADD
        _blendmodeInstances.Add(BlendMode.BLEND_SUBTRACT, protoManager.Index<ShaderPrototype>("blend_subtract").InstanceUnique()); //BLEND_SUBTRACT
        _blendmodeInstances.Add(BlendMode.BLEND_MULTIPLY, protoManager.Index<ShaderPrototype>("blend_multiply").InstanceUnique()); //BLEND_MULTIPLY
        _blendmodeInstances.Add(BlendMode.BLEND_INSET_OVERLAY, protoManager.Index<ShaderPrototype>("blend_inset_overlay").InstanceUnique()); //BLEND_INSET_OVERLAY //TODO

        spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        mobSightQuery = _entityManager.GetEntityQuery<DreamMobSightComponent>();
        _flipMatrix = Matrix3.Identity;
        _flipMatrix.R1C1 = -1;
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        using var _ = _prof.Group("Dream View Overlay");

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));

        //get our mouse map ready for drawing, and clear the hash table
        _mouseMapRenderTarget = RentRenderTarget((Vector2i)(args.Viewport.Size / args.Viewport.RenderScale));
        ClearRenderTarget(_mouseMapRenderTarget, args.WorldHandle, Color.Transparent);
        MouseMapLookup.Clear();
        //Main drawing of sprites happens here
        try {
            DrawAll(args, eye.Value);
        } catch (Exception e) {
            Logger.Error($"Error occurred while rendering frame. Error details:\n{e.Message}\n{e.StackTrace}");
        }

        //store our mouse map's image and return the render target
        MouseMap = _mouseMapRenderTarget.Texture;
        ReturnRenderTarget(_mouseMapRenderTarget);

        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();

        //some render targets need to be kept until the end of the render cycle, so return them here.
        _renderSourceLookup.Clear();

        while(_renderTargetsToReturn.Count > 0)
            ReturnRenderTarget(_renderTargetsToReturn.Pop());

        //RendererMetaData objects get reused instead of GC'd
        while( _rendererMetaDataToReturn.Count > 0)
            _rendererMetaDataRental.Push(_rendererMetaDataToReturn.Pop());
    }

    private void DrawAll(OverlayDrawArgs args, EntityUid eye) {
        _transformSystem ??= _entitySystem.GetEntitySystem<SharedTransformSystem>();
        _lookupSystem ??= _entitySystem.GetEntitySystem<EntityLookupSystem>();
        _appearanceSystem ??= _entitySystem.GetEntitySystem<ClientAppearanceSystem>();
        _screenOverlaySystem ??= _entitySystem.GetEntitySystem<ClientScreenOverlaySystem>();

        if (!xformQuery.TryGetComponent(eye, out var eyeTransform))
            return;

        Box2 screenArea = Box2.CenteredAround(eyeTransform.WorldPosition, args.WorldAABB.Size);

        _mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid);

        HashSet<EntityUid> entities;
        using (_prof.Group("lookup")) {
            //TODO use a sprite tree.
            //the scaling is to attempt to prevent pop-in, by rendering sprites that are *just* offscreen
            entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, screenArea.Scale(1.2f), MapLookupFlags);
        }

        List<RendererMetaData> sprites = new(entities.Count + 1);

        mobSightQuery.TryGetComponent(eye, out var mobSight);
        int seeVis = mobSight?.SeeInvisibility ?? 127;
        SightFlags sight = mobSight?.Sight ?? 0;

        //self icon
        if (spriteQuery.TryGetComponent(eye, out var player) && xformQuery.TryGetComponent(player.Owner, out var playerTransform)){
            if(RenderPlayerEnabled && player.IsVisible(mapManager: _mapManager, seeInvis: seeVis))
                sprites.AddRange(ProcessIconComponents(player.Icon, _transformSystem.GetWorldPosition(playerTransform.Owner, xformQuery) - 0.5f, player.Owner, false));
        }

        // Hardcoded for a 15x15 view (with 1 tile buffer on each side)
        var tiles = new ViewAlgorithm.Tile?[17, 17];

        if (grid != null) {
            var eyeTile = grid.GetTileRef(eyeTransform.MapPosition);

            //visible turfs
            if(RenderTurfEnabled) {
                using var _ = _prof.Group("visible turfs");

                var eyeWorldPos = grid.GridTileToWorld(eyeTile.GridIndices);
                var tileRefs = grid.GetTilesIntersecting(Box2.CenteredAround(eyeWorldPos.Position, (17, 17)));

                // Gather up all the data the view algorithm needs
                foreach (TileRef tileRef in tileRefs) {
                    var delta = tileRef.GridIndices - eyeTile.GridIndices;
                    var appearance = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId).Appearance;

                    var tile = new ViewAlgorithm.Tile {
                        Opaque = appearance.Opacity,
                        Luminosity = 0,
                        DeltaX = delta.X,
                        DeltaY = delta.Y
                    };

                    tiles[delta.X + 8, delta.Y + 8] = tile;
                }

                // Apply entities' opacity
                foreach (EntityUid entity in entities) {
                    // TODO use a sprite tree.
                    if (!spriteQuery.TryGetComponent(entity, out var sprite))
                        continue;
                    if (!sprite.IsVisible(mapManager: _mapManager, seeInvis: seeVis))
                        continue;

                    var worldPos = _transformSystem.GetWorldPosition(entity, xformQuery);
                    var tilePos = grid.WorldToTile(worldPos) - eyeTile.GridIndices + 8;
                    if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= 17 || tilePos.Y >= 17)
                        continue;

                    var tile = tiles[tilePos.X, tilePos.Y];
                    if (tile != null)
                        tile.Opaque |= sprite.Icon.Appearance.Opacity;
                }

                ViewAlgorithm.CalculateVisibility(tiles);

                // Collect visible turf sprites
                foreach (var tile in tiles) {
                    if (tile == null)
                        continue;
                    if (tile.IsVisible == false && (sight & SightFlags.SeeTurfs) == 0)
                        continue;

                    Vector2i tilePos = eyeTile.GridIndices + (tile.DeltaX, tile.DeltaY);
                    TileRef tileRef = grid.GetTileRef(tilePos);
                    MapCoordinates worldPos = grid.GridTileToWorld(tilePos);

                    sprites.AddRange(ProcessIconComponents(_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), worldPos.Position - 1, EntityUid.Invalid, false));
                }
            }

            //visible entities
            if (RenderEntityEnabled) {
                using var _ = _prof.Group("process entities");

                foreach (EntityUid entity in entities) {
                    if(entity == eye)
                        continue; //don't render the player twice

                    // TODO use a sprite tree.
                    if (!spriteQuery.TryGetComponent(entity, out var sprite))
                        continue;
                    if (!sprite.IsVisible(mapManager: _mapManager, seeInvis: seeVis))
                        continue;

                    var worldPos = _transformSystem.GetWorldPosition(entity, xformQuery);

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

                    sprites.AddRange(ProcessIconComponents(sprite.Icon, worldPos - 0.5f, sprite.Owner, false));
                }
            }
        }

        //screen objects
        if(ScreenOverlayEnabled){
            using var _ = _prof.Group("screen objects");
            foreach (DMISpriteComponent sprite in _screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager, seeInvis: seeVis))
                    continue;
                if (sprite.ScreenLocation.MapControl != null) // Don't render screen objects meant for other map controls
                    continue;
                Vector2 position = sprite.ScreenLocation.GetViewPosition(screenArea.BottomLeft, EyeManager.PixelsPerMeter);
                Vector2 iconSize = sprite.Icon.DMI == null ? Vector2.Zero : sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;
                for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                    for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                        sprites.AddRange(ProcessIconComponents(sprite.Icon, position + iconSize * (x, y), sprite.Owner, true));
                    }
                }
            }
        }

        //early return if there's nothing to do
        if(sprites.Count == 0)
            return;

        using (_prof.Group("sort sprites")) {
            sprites.Sort();
        }

        //dict of planes to their planemaster (if they have one) and list of mousmap and sprite draw actions on that plane in order, keyed by plane number
        Dictionary<float, (RendererMetaData?, List<(Action, Action)>)> planesList = new(sprites.Count);

        //all sprites with render targets get handled first - these are ordered by sprites.Sort(), so we can just iterate normally

        var profGroup = _prof.Group("draw sprites");
        for(var i = 0; i < sprites.Count; i++){
            RendererMetaData sprite = sprites[i];

            if(!string.IsNullOrEmpty(sprite.RenderTarget)) {
                //if this sprite has a render target, draw it to a slate instead. If it needs to be drawn on the map, a second sprite instance will already have been created for that purpose
                IRenderTexture tmpRenderTarget;
                if(!_renderSourceLookup.TryGetValue(sprite.RenderTarget, out tmpRenderTarget)){
                    tmpRenderTarget = RentRenderTarget((Vector2i)(args.Viewport.Size / args.Viewport.RenderScale));
                    ClearRenderTarget(tmpRenderTarget, args.WorldHandle, new Color());
                    _renderSourceLookup.Add(sprite.RenderTarget, tmpRenderTarget);
                    _renderTargetsToReturn.Push(tmpRenderTarget);
                }

                if((sprite.AppearanceFlags & AppearanceFlags.PLANE_MASTER) != 0){ //if this is also a PLANE_MASTER
                    if(!planesList.TryGetValue(sprite.Plane, out (RendererMetaData?, List<(Action, Action)>) planeEntry)){
                        //if the plane hasn't already been created, store this sprite as the plane_master
                        planesList[sprite.Plane] = (sprite, new List<(Action, Action)>());
                    } else {
                        //the plane has already been created, so just replace the planesList entry with this sprite as its plane master
                        planesList[sprite.Plane] = (sprite, planeEntry.Item2);
                    }
                } else {//if not a PLANE_MASTER, draw the sprite to the render target
                    //note we don't draw this to the mousemap because that's handled when the RenderTarget is used as a source later
                    DrawIconNow(args.WorldHandle, tmpRenderTarget, sprite, ((args.WorldAABB.Size/2)-sprite.Position)-new Vector2(0.5f,0.5f), null, true); //draw the sprite centered on the RenderTarget
                }
            } else { //We are no longer dealing with RenderTargets, just regular old planes, so we collect the draw actions for batching
                if(!planesList.TryGetValue(sprite.Plane, out (RendererMetaData?, List<(Action, Action)>) planeEntry)){
                    //this plane doesn't exist yet, it's probably the first reference to it.
                    //Lets create it
                    planeEntry = (null, new List<(Action, Action)>());
                    planesList[sprite.Plane] = planeEntry;
                }

                if((sprite.AppearanceFlags & AppearanceFlags.PLANE_MASTER) != 0){ //if this is a PLANE_MASTER, we don't render it, we just set the planeMaster value and move on
                    sprite.Position = Vector2.Zero; //plane masters should not have a position offset
                    planesList[sprite.Plane] = (sprite, planeEntry.Item2);
                    continue;
                }

                //add this sprite for rendering
                (Action,Action) drawActions;
                if(!string.IsNullOrEmpty(sprite.RenderSource) && _renderSourceLookup.TryGetValue(sprite.RenderSource, out var renderSourceTexture)) {
                    drawActions = DrawiconAction(args.WorldHandle, sprite, (-screenArea.BottomLeft)-(args.WorldAABB.Size/2)+new Vector2(0.5f,0.5f), renderSourceTexture.Texture);
                } else {
                    drawActions = DrawiconAction(args.WorldHandle, sprite, -screenArea.BottomLeft);
                }
                planeEntry.Item2.Add(drawActions);
                planesList[sprite.Plane] = planeEntry;
            }
        }
        profGroup.Dispose();

        //Final draw
        //At this point, all the sprites have been organised on their planes, render targets have been drawn, now we just draw it all together!
        IRenderTexture baseTarget = RentRenderTarget((Vector2i)(args.Viewport.Size / args.Viewport.RenderScale));
        ClearRenderTarget(baseTarget, args.WorldHandle, new Color());

        //unfortunately, order is undefined when grabbing keys from a dictionary, so we have to sort them
        List<float> planeKeys = new List<float>(planesList.Keys);

        using (_prof.Group("sort planeKeys")) {
            planeKeys.Sort();
        }

        profGroup = _prof.Group("draw planeKeys");
        foreach(float plane in planeKeys){
            (RendererMetaData?, List<(Action,Action)>) planeEntry = planesList[plane];
            IRenderTexture planeTarget = baseTarget;
            if(planeEntry.Item1 != null){
                //we got a plane master here, so rent out a texture and draw to that
                planeTarget = RentRenderTarget((Vector2i)(args.Viewport.Size / args.Viewport.RenderScale));
                ClearRenderTarget(planeTarget, args.WorldHandle, new Color());
            }

            List<Action> mouseMapActions = new(planeEntry.Item2.Count);
            List<Action> iconActions = new(planeEntry.Item2.Count);
            foreach((Action iconAction, Action mouseMapAction) in planeEntry.Item2) {
                mouseMapActions.Add(mouseMapAction);
                iconActions.Add(iconAction);
            }

            args.WorldHandle.RenderInRenderTarget(planeTarget, () => {
                foreach(Action a in iconActions)
                    a();
                }, null);
            args.WorldHandle.RenderInRenderTarget(_mouseMapRenderTarget, () => {
                foreach(Action a in mouseMapActions)
                    a();
                }, null);

            if(planeEntry.Item1 != null){
                //this was a plane master, so draw the plane onto the baseTarget and return it
                DrawIconNow(args.WorldHandle, baseTarget, planeEntry.Item1, Vector2.Zero, planeTarget.Texture, noMouseMap: true);
                ReturnRenderTarget(planeTarget);
            }
        }
        profGroup.Dispose();

        if(MouseMapRenderEnabled) //if this is enabled, we just draw the mouse map
            args.WorldHandle.DrawTexture(_mouseMapRenderTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), null);
        else
            args.WorldHandle.DrawTexture(baseTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), null); //draw the basetarget onto the world
        ReturnRenderTarget(baseTarget);
    }

    //handles underlays, overlays, appearance flags, images. Returns a list of icons and metadata for them to be sorted, so they can be drawn with DrawIcon()
    private List<RendererMetaData> ProcessIconComponents(DreamIcon icon, Vector2 position, EntityUid uid, Boolean isScreen, RendererMetaData? parentIcon = null, bool keepTogether = false, int tieBreaker = 0) {
        if(icon.Appearance is null) //in the event that appearance hasn't loaded yet
            return new List<RendererMetaData>(0);
        List<RendererMetaData> result = new(icon.Underlays.Count + icon.Overlays.Count + 1);
        RendererMetaData current = RentRendererMetaData();
        current.MainIcon = icon;
        current.Position = position + (icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter);
        current.UID = uid;
        current.ClickUID = uid;
        current.IsScreen = isScreen;
        current.TieBreaker = tieBreaker;
        current.RenderSource = icon.Appearance.RenderSource;
        current.RenderTarget = icon.Appearance.RenderTarget;
        current.AppearanceFlags = icon.Appearance.AppearanceFlags;
        current.BlendMode = icon.Appearance.BlendMode;
        current.MouseOpacity = icon.Appearance.MouseOpacity;

        Matrix3 iconAppearanceTransformMatrix = new(new float[] { //reverse rotation transforms because of 180 flip from rendertarget->world transform
                icon.Appearance.Transform[0], -icon.Appearance.Transform[1], icon.Appearance.Transform[4],
                -icon.Appearance.Transform[2], icon.Appearance.Transform[3], icon.Appearance.Transform[5],
                0, 0, 1
            });

        if(parentIcon != null){
            current.ClickUID = parentIcon.ClickUID;
            if((icon.Appearance.AppearanceFlags & AppearanceFlags.RESET_COLOR) != 0 || keepTogether){ //RESET_COLOR
                current.ColorToApply = icon.Appearance.Color;
                current.ColorMatrixToApply = icon.Appearance.ColorMatrix;
            }
            else{
                current.ColorToApply = parentIcon.ColorToApply;
                current.ColorMatrixToApply = parentIcon.ColorMatrixToApply;
            }

            if((icon.Appearance.AppearanceFlags & AppearanceFlags.RESET_ALPHA) !=0 || keepTogether) //RESET_ALPHA
                current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            else
                current.AlphaToApply = parentIcon.AlphaToApply;

            if((icon.Appearance.AppearanceFlags & AppearanceFlags.RESET_TRANSFORM) != 0 || keepTogether) //RESET_TRANSFORM
                current.TransformToApply = iconAppearanceTransformMatrix;
            else
                current.TransformToApply = parentIcon.TransformToApply;

            if((icon.Appearance.Plane < -10000)) //FLOAT_PLANE - Note: yes, this really is how it works. Yes it's dumb as shit.
                current.Plane = parentIcon.Plane + (icon.Appearance.Plane + 32767);
            else
                current.Plane = icon.Appearance.Plane;

            if(icon.Appearance.Layer < 0) //FLOAT_LAYER
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
        if(current.Layer >= 20000){
            current.Layer -= 40000;
            current.IsScreen = false; //BACKGROUND_LAYER renders behind everything on that plane
        }

        keepTogether = keepTogether || ((current.AppearanceFlags & AppearanceFlags.KEEP_TOGETHER) != 0); //KEEP_TOGETHER

        //if the rendertarget starts with *, we don't render it. If it doesn't we create a placeholder rendermetadata to position it correctly
        if(!string.IsNullOrEmpty(current.RenderTarget) && current.RenderTarget[0]!='*') {
            RendererMetaData renderTargetPlaceholder = RentRendererMetaData();
            //transform, color, alpha, filters - they should all already have been applied, so we leave them null in the placeholder
            renderTargetPlaceholder.Position = current.Position;
            renderTargetPlaceholder.UID = current.UID;
            renderTargetPlaceholder.ClickUID = current.UID;
            renderTargetPlaceholder.IsScreen = current.IsScreen;
            renderTargetPlaceholder.TieBreaker = current.TieBreaker;
            renderTargetPlaceholder.Plane = current.Plane;
            renderTargetPlaceholder.Layer = current.Layer;
            renderTargetPlaceholder.RenderSource = current.RenderTarget;
            renderTargetPlaceholder.MouseOpacity = current.MouseOpacity;
            result.Add(renderTargetPlaceholder);
        }

        //TODO check for images with override here
        /*foreach(image in client.images){
            if(image.override && image.location == icon.owner)
                current.MainIcon = image
            else
                add like overlays?
        }*/

        //TODO vis_contents
        //click uid should be set to current.uid again
        //dont forget the vis_flags

        //underlays - colour, alpha, and transform are inherited, but filters aren't
        int underlayTiebreaker = -icon.Underlays.Count+tieBreaker;
        foreach (DreamIcon underlay in icon.Underlays) {
            underlayTiebreaker--;

            if(!keepTogether || (underlay.Appearance.AppearanceFlags & AppearanceFlags.KEEP_APART) != 0) //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
                result.AddRange(ProcessIconComponents(underlay, current.Position, uid, isScreen, current, false, underlayTiebreaker));
            else {
                current.KeepTogetherGroup ??= new();
                current.KeepTogetherGroup.AddRange(ProcessIconComponents(underlay, current.Position, uid, isScreen, current, keepTogether, underlayTiebreaker));
            }
        }

        //overlays - colour, alpha, and transform are inherited, but filters aren't
        int overlayTiebreaker = icon.Overlays.Count+tieBreaker;
        foreach (DreamIcon overlay in icon.Overlays) {
            overlayTiebreaker++;

            if(!keepTogether || (overlay.Appearance.AppearanceFlags & AppearanceFlags.KEEP_APART) != 0) //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
                result.AddRange(ProcessIconComponents(overlay, current.Position, uid, isScreen, current, false, overlayTiebreaker));
            else {
                current.KeepTogetherGroup ??= new();
                current.KeepTogetherGroup.AddRange(ProcessIconComponents(overlay, current.Position, uid, isScreen, current, keepTogether, overlayTiebreaker));
            }
        }

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //flatten keeptogethergroup. Done here so we get implicit recursive iteration down the tree.
        if(current.KeepTogetherGroup != null && current.KeepTogetherGroup.Count > 0){
            List<RendererMetaData> flatKTGroup = new List<RendererMetaData>(current.KeepTogetherGroup.Count);
            foreach(RendererMetaData ktItem in current.KeepTogetherGroup){
                if(ktItem.KeepTogetherGroup != null)
                    flatKTGroup.AddRange(ktItem.KeepTogetherGroup);
                flatKTGroup.Add(ktItem);
                ktItem.KeepTogetherGroup = null; //might need to be Clear()
            }
            current.KeepTogetherGroup = flatKTGroup;
        }

        result.Add(current);
        return result;
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


    private ShaderInstance GetBlendAndColorShader(RendererMetaData iconMetaData, Color? colorOverride = null, BlendMode? blendOverride = null) {
        Color RGBA = colorOverride == null ? iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply) : colorOverride.Value;
        ColorMatrix colorMatrix;
        if(colorOverride != null || iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity))
            colorMatrix = new ColorMatrix(RGBA);
        else
            colorMatrix = iconMetaData.ColorMatrixToApply;
        ShaderInstance blendAndColor;
        if(blendOverride != null || !_blendmodeInstances.TryGetValue(iconMetaData.BlendMode, out blendAndColor))
            blendAndColor = _blendmodeInstances[blendOverride == null ? BlendMode.BLEND_DEFAULT : blendOverride.Value];
        blendAndColor = blendAndColor.Duplicate();
        blendAndColor.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
        blendAndColor.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
        return blendAndColor;
    }

    private (Action, Action) DrawiconAction(DrawingHandleWorld handle, RendererMetaData iconMetaData, Vector2 positionOffset, Texture textureOverride = null) {
        DreamIcon icon = iconMetaData.MainIcon;

        Vector2 position = iconMetaData.Position + positionOffset;
        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;

        Texture frame;
        if(textureOverride != null) {
            frame = textureOverride;
            //we flip this because GL's coordinate system is bottom-left first, and so render target textures are upside down
            iconMetaData.TransformToApply = iconMetaData.TransformToApply * _flipMatrix;
        }
        else
            frame = icon.CurrentFrame;

        //KEEP_TOGETHER groups
        if(iconMetaData.KeepTogetherGroup != null && iconMetaData.KeepTogetherGroup.Count > 0) {
            //store the parent's transform, color, blend, and alpha - then clear them for drawing to the render target
            Matrix3 ktParentTransform = iconMetaData.TransformToApply;
            iconMetaData.TransformToApply = Matrix3.Identity;
            Color ktParentColor = iconMetaData.ColorToApply;
            iconMetaData.ColorToApply = Color.White;
            float ktParentAlpha = iconMetaData.AlphaToApply;
            iconMetaData.AlphaToApply = 1f;
            BlendMode ktParentBlendMode = iconMetaData.BlendMode;
            iconMetaData.BlendMode = BlendMode.BLEND_DEFAULT;

            List<RendererMetaData> ktItems = new List<RendererMetaData>(iconMetaData.KeepTogetherGroup.Count+1);
            ktItems.Add(iconMetaData);
            ktItems.AddRange(iconMetaData.KeepTogetherGroup);
            iconMetaData.KeepTogetherGroup.Clear();

            ktItems.Sort();
            //draw it onto an additional render target that we can return immediately for correction of transform
            // TODO: Use something better than a hardcoded 64x64 fallback
            IRenderTexture tempTexture = RentRenderTarget(frame?.Size ?? (64,64));
            ClearRenderTarget(tempTexture, handle, Color.Transparent);

            foreach(RendererMetaData ktItem in ktItems){
                DrawIconNow(handle, tempTexture, ktItem, -ktItem.Position);
            }
            //but keep the handle to the final KT group's render target so we don't override it later in the render cycle
            IRenderTexture ktTexture = RentRenderTarget(tempTexture.Size);
            handle.RenderInRenderTarget(ktTexture , () => {
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
        if(frame == null)
            return (()=>{},()=>{});

        Action iconDrawAction;
        Action mouseMapDrawAction;

        //setup the mousemaplookup shader for use in DrawIcon()
        byte[] rgba = BitConverter.GetBytes(iconMetaData.GetHashCode());
        Color targetColor = new Color(rgba[0],rgba[1],rgba[2],255); //TODO - this could result in misclicks due to hash-collision since we ditch a whole byte.
        MouseMapLookup[targetColor] = iconMetaData;

        Matrix3 tmpTranslation = Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                 iconMetaData.TransformToApply *
                                 Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2));
        Box2 drawBounds = new Box2(pixelPosition, pixelPosition+frame.Size);

        //go fast when the only filter is color, and we don't have more color things to consider
        bool goFastOverride = false;
        if(icon.Appearance != null && iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity) && iconMetaData.ColorToApply == Color.White && iconMetaData.AlphaToApply == 1.0f && icon.Appearance.Filters.Count == 1 && icon.Appearance.Filters[0].FilterType == "color"){
            DreamFilterColor colorFilter = (DreamFilterColor)icon.Appearance.Filters[0];
            iconMetaData.ColorMatrixToApply = colorFilter.Color;
            goFastOverride = true;
        }


        if(goFastOverride || icon.Appearance == null || icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            iconDrawAction = () => {
                    handle.UseShader(GetBlendAndColorShader(iconMetaData));
                    handle.SetTransform(tmpTranslation);
                    handle.DrawTextureRect(frame,
                        drawBounds,
                        null);
                    handle.UseShader(null);
                };
            if(iconMetaData.MouseOpacity != MouseOpacity.Transparent) {
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
            IRenderTexture tmpHolder;

            handle.RenderInRenderTarget(pong,
                () => {
                    //we can use the color matrix shader here, since we don't need to blend
                    //also because blend mode is none, we don't need to drawrect clear
                    ColorMatrix colorMatrix;
                    if(iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity))
                        colorMatrix = new ColorMatrix(iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                    else
                        colorMatrix = iconMetaData.ColorMatrixToApply;
                    ShaderInstance colorShader = _colorInstance.Duplicate();
                    colorShader.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
                    colorShader.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
                    handle.UseShader(colorShader);
                    handle.DrawTextureRect(frame,
                        new Box2(Vector2.Zero + (frame.Size / 2), frame.Size + (frame.Size / 2)),
                        null);
                    handle.UseShader(null);
                }, Color.Black.WithAlpha(0));


            foreach (DreamFilter filterId in icon.Appearance.Filters) {
                ShaderInstance s = _appearanceSystem.GetFilterShader(filterId, _renderSourceLookup);

                handle.RenderInRenderTarget(ping, () => {
                    handle.UseShader(s);
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                    handle.UseShader(null);
                }, Color.Black.WithAlpha(0));


                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
            }
            if(icon.Appearance?.Filters.Count % 2 == 0) //if we have an even number of filters, we need to flip
                tmpTranslation = Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                    iconMetaData.TransformToApply * _flipMatrix *
                                    Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2));

            //then we return the Action that draws the actual icon with filters applied
            iconDrawAction = () => {
                    //note we apply the color *before* the filters, so we use override here
                    handle.UseShader(GetBlendAndColorShader(iconMetaData, colorOverride: Color.White));
                    handle.SetTransform(tmpTranslation);
                    handle.DrawTextureRect(pong.Texture,
                        new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                        null);
                    handle.UseShader(null);
                };
            if(iconMetaData.MouseOpacity != MouseOpacity.Transparent) {
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

    private void DrawIconNow(DrawingHandleWorld handle, IRenderTarget renderTarget, RendererMetaData iconMetaData, Vector2 positionOffset, Texture textureOverride = null, bool noMouseMap = false) {

        (Action iconDrawAction, Action mouseMapDrawAction) = DrawiconAction(handle, iconMetaData, positionOffset, textureOverride);

        handle.RenderInRenderTarget(renderTarget, () => {
                iconDrawAction();
            }, null);
        //action should be NOP if this is transparent, but save a RiRT call anyway since we can
        if(!(noMouseMap || iconMetaData.MouseOpacity != MouseOpacity.Transparent)) {
            handle.RenderInRenderTarget(_mouseMapRenderTarget, () => {
                mouseMapDrawAction();
            }, null);
        }
    }

    private RendererMetaData RentRendererMetaData(){
        RendererMetaData result;
        if(_rendererMetaDataRental.Count == 0)
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
    public DreamIcon MainIcon;
    public Vector2 Position;
    public int Plane; //true plane value may be different from appearance plane value, due to special flags
    public float Layer; //ditto for layer
    public EntityUid UID;
    public EntityUid ClickUID; //the UID of the object clicks on this should be passed to (ie, for overlays)
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

    public RendererMetaData() {
        Reset();
    }

    public void Reset(){
        MainIcon = new DreamIcon();
        Position = Vector2.Zero;
        Plane = 0;
        Layer = 0;
        UID = EntityUid.Invalid;
        ClickUID = EntityUid.Invalid;
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
        BlendMode = BlendMode.BLEND_DEFAULT;
        MouseOpacity = MouseOpacity.Transparent;
    }

    public int CompareTo(RendererMetaData other) {
        int val = 0;

        //Render target and source ordering is done first.
        //Anything with a render target goes first
        //Anything with a render source which points to a render target must come *after* that render_target
        val = (!string.IsNullOrEmpty(RenderTarget)).CompareTo(!string.IsNullOrEmpty(other.RenderTarget));
        if (val != 0) {
            return -val;
        }
        if(!string.IsNullOrEmpty(RenderSource)) {
            if(RenderSource == other.RenderTarget)
                return 1;
        }

        //We now return to your regularly scheduled sprite render order

        //Plane
        val =  this.Plane.CompareTo(other.Plane);
        if (val != 0) {
            return val;
        }
        val = ((this.AppearanceFlags & AppearanceFlags.PLANE_MASTER) != 0).CompareTo((other.AppearanceFlags & AppearanceFlags.PLANE_MASTER) != 0); //appearance_flags & PLANE_MASTER
        //PLANE_MASTER objects go first for any given plane
        if (val != 0) {
            return -val; //sign flip because we want 1 < -1
        }
        //subplane (ie, HUD vs not HUD)
        val = this.IsScreen.CompareTo(other.IsScreen);
        if (val != 0) {
            return val;
        }
        //depending on world.map_format, either layer or physical position
        //TODO
        val = this.Layer.CompareTo(other.Layer);
        if (val != 0) {
            return val;
        }
        //despite assurances to the contrary by the dmref, position is in fact used for draw order in topdown mode
        val = this.Position.X.CompareTo(other.Position.X);
        if (val != 0) {
            return val;
        }
        val = this.Position.Y.CompareTo(other.Position.Y);
        if (val != 0) {
            return -val;
        }
        //Finally, tie-breaker - in BYOND, this is order of creation of the sprites
        //for us, we use EntityUID, with a tie-breaker (for underlays/overlays)
        val = this.UID.CompareTo(other.UID);
        if (val != 0) {
            return val;
        }

        val = this.TieBreaker.CompareTo(other.TieBreaker);
        if (val != 0) {
            return val;
        }
        return val;
    }
}


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

#region Render Toggle Commands
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

public sealed class ToggleTurfRenderCommand : IConsoleCommand {
    public string Command => "toggleturfrender";
    public string Description => "Toggle rendering of turfs";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.RenderTurfEnabled = !screenOverlay.RenderTurfEnabled;
        }
    }
}

public sealed class ToggleEntityRenderCommand : IConsoleCommand {
    public string Command => "toggleentityrender";
    public string Description => "Toggle rendering of entities";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.RenderEntityEnabled = !screenOverlay.RenderEntityEnabled;
        }
    }
}

public sealed class TogglePlayerRenderCommand : IConsoleCommand {
    public string Command => "toggleplayerrender";
    public string Description => "Toggle rendering of the player";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.RenderPlayerEnabled = !screenOverlay.RenderPlayerEnabled;
        }
    }
}


#endregion
