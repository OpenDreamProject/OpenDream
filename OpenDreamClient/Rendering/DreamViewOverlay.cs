using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

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
    private EntityQuery<DMISpriteComponent> spriteQuery;
    private EntityQuery<TransformComponent> xformQuery;
    private ShaderInstance _blockColorInstance;
    private Dictionary<int, ShaderInstance> _blendmodeInstances;

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
    public bool CheckForZFighting = false;
    private IRenderTexture mouseMapRenderTarget;
    public Texture MouseMap;
    public Dictionary<Color, EntityUid> MouseMapLookup = new();
    private Dictionary<String, IRenderTexture> _renderSourceLookup = new();
    private Stack<IRenderTexture> _renderTargetsToReturn = new();
    private Stack<RendererMetaData> _rendererMetaDataRental = new();
    private Stack<RendererMetaData> _rendererMetaDataToReturn = new();

    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
        Logger.Debug("Loading shaders...");
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        _blockColorInstance = protoManager.Index<ShaderPrototype>("blockcolor").InstanceUnique();

        _blendmodeInstances = new(4);
        //_blendmodeInstances.Add(0, protoManager.Index<ShaderPrototype>("empty").InstanceUnique()); //BLEND_DEFAULT, BLEND_OVERLAY - null shaders, because overlay is default behaviour
        _blendmodeInstances.Add(2, protoManager.Index<ShaderPrototype>("blend_add").InstanceUnique()); //BLEND_ADD
        _blendmodeInstances.Add(3, protoManager.Index<ShaderPrototype>("blend_subtract").InstanceUnique()); //BLEND_SUBTRACT
        _blendmodeInstances.Add(4, protoManager.Index<ShaderPrototype>("blend_multiply").InstanceUnique()); //BLEND_MULTIPLY
        _blendmodeInstances.Add(5, protoManager.Index<ShaderPrototype>("blend_inset_overlay").InstanceUnique()); //BLEND_INSET_OVERLAY //TODO

        spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));

        //get our mouse map ready for drawing, and clear the hash table
        mouseMapRenderTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        ClearRenderTarget(mouseMapRenderTarget, args.WorldHandle, Color.Transparent);
        MouseMapLookup.Clear();

        //Main drawing of sprites happens here
        DrawAll(args, eye.Value);

        //store our mouse map's image and return the render target
        MouseMap = mouseMapRenderTarget.Texture;
        ReturnPingPongRenderTarget(mouseMapRenderTarget);

        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();

        //some render targets need to be kept until the end of the render cycle, so return them here.
        _renderSourceLookup.Clear();

        while(_renderTargetsToReturn.Count > 0)
            ReturnPingPongRenderTarget(_renderTargetsToReturn.Pop());

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

        var entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, screenArea.Scale(1.2f)); //the scaling is to attempt to prevent pop-in, by rendering sprites that are *just* offscreen
        List<RendererMetaData> sprites = new(entities.Count + 1);

        //self icon
        if(RenderPlayerEnabled){
            if (spriteQuery.TryGetComponent(eye, out var player) && player.IsVisible(mapManager: _mapManager) && xformQuery.TryGetComponent(player.Owner, out var playerTransform))
                sprites.AddRange(ProcessIconComponents(player.Icon, _transformSystem.GetWorldPosition(playerTransform.Owner, xformQuery) - 0.5f, player.Owner, false));
        }
        //visible entities
        if(RenderEntityEnabled){
            foreach (EntityUid entity in entities) {
                if(entity == eye)
                    continue; //don't render the player twice
                if (!spriteQuery.TryGetComponent(entity, out var sprite))
                    continue;
                if (!sprite.IsVisible(mapManager: _mapManager))
                    continue;
                if(!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                    continue;
                sprites.AddRange(ProcessIconComponents(sprite.Icon, _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f, sprite.Owner, false));
            }
        }

        //visible turfs
        if(RenderTurfEnabled){
            if (_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
                foreach (TileRef tileRef in grid.GetTilesIntersecting(screenArea.Scale(1.2f))) {
                    MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
                    sprites.AddRange(ProcessIconComponents(_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), pos.Position - 1, EntityUid.Invalid, false));
                }
        }

        //screen objects
        if(ScreenOverlayEnabled){
            foreach (DMISpriteComponent sprite in _screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager))
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

        sprites.Sort();
        if(CheckForZFighting){
            for(int i = 1; i < sprites.Count; i++)
                if(sprites[i-1].Position == sprites[i].Position && sprites[i-1].CompareTo(sprites[i]) == 0 && sprites[i].CompareTo(sprites[i-1]) == 0)
                    Logger.Debug($"Z fighting! Objects at {sprites[i].Position} with iconstates {sprites[i].MainIcon?.Appearance?.IconState} and {sprites[i-1].MainIcon?.Appearance?.IconState}");
        }
        //dict of planes to their planemaster (if they have one) and list of sprites drawn on that plane in order, keyed by plane number
        Dictionary<float, (RendererMetaData?, List<RendererMetaData>)> PlanesList = new(sprites.Count);

        //all sprites with render targets get handled first - these are ordered by sprites.Sort(), so we can just iterate normally

        for(var i = 0; i < sprites.Count; i++){
            RendererMetaData sprite = sprites[i];

            if(sprite.RenderTarget.Length > 0){
                //if this sprite has a render target, draw it to a slate instead. If it needs to be drawn on the map, a second sprite instance will already have been created for that purpose
                IRenderTexture tmpRenderTarget;
                if(!_renderSourceLookup.TryGetValue(sprite.RenderTarget, out tmpRenderTarget)){

                    tmpRenderTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
                    ClearRenderTarget(tmpRenderTarget, args.WorldHandle, new Color());
                    _renderSourceLookup.Add(sprite.RenderTarget, tmpRenderTarget);
                    _renderTargetsToReturn.Push(tmpRenderTarget);
                }

                if(((int)sprite.AppearanceFlags & 128) == 128){ //if this is also a PLANE_MASTER
                    if(!PlanesList.TryGetValue(sprite.Plane, out (RendererMetaData?, List<RendererMetaData>) planeEntry)){
                        //if the plane hasn't already been created, store this sprite as the plane_master
                        PlanesList[sprite.Plane] = (sprite, new List<RendererMetaData>());
                    } else {
                        //the plane has already been created, so just replace the planeslist entry with this sprite as its plane master
                        PlanesList[sprite.Plane] = (sprite, planeEntry.Item2);
                    }
                } else {//if not a PLANE_MASTER, draw the sprite to the render target
                    //note we don't draw this to the mousemap because that's handled when the RenderTarget is used as a source later
                    _blockColorInstance.SetParameter("targetColor", new Color()); //Set shader instance's block colour to null for the mouse map
                    DrawIcon(args.WorldHandle, tmpRenderTarget, sprite, ((args.WorldAABB.Size/2)-sprite.Position)-new Vector2(0.5f,0.5f)); //draw the sprite centered on the RenderTarget
                }
            } else { //We are no longer dealing with RenderTargets, just regular old planes
                if(!PlanesList.TryGetValue(sprite.Plane, out (RendererMetaData?, List<RendererMetaData>) planeEntry)){
                    //this plane doesn't exist yet, it's probably the first reference to it.
                    //Lets create it
                    planeEntry = (null, new List<RendererMetaData>());
                    PlanesList[sprite.Plane] = planeEntry;
                }

                if(((int)sprite.AppearanceFlags & 128) == 128){ //if this is a PLANE_MASTER, we don't render it, we just set the planeMaster value and move on
                    sprite.Position = Vector2.Zero; //plane masters should not have a position offset
                    PlanesList[sprite.Plane] = (sprite, planeEntry.Item2);
                    continue;
                }

                //add this sprite for rendering
                planeEntry.Item2.Add(sprite);
                PlanesList[sprite.Plane] = planeEntry;
            }
        }
        //Final draw
        //At this point, all the sprites have been organised on their planes, render targets have been drawn, now we just draw it all together!
        IRenderTexture baseTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        ClearRenderTarget(baseTarget, args.WorldHandle, new Color());

        //unfortunately, order is undefined when grabbing keys from a dictionary, so we have to sort them
        List<float> planeKeys = new List<float>(PlanesList.Keys);
        planeKeys.Sort();
        foreach(float plane in planeKeys){
            (RendererMetaData?, List<RendererMetaData>) planeEntry = PlanesList[plane];
            IRenderTexture planeTarget = baseTarget;
            if(planeEntry.Item1 != null){
                //we got a plane master here, so rent out a texture and draw to that
                planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
                ClearRenderTarget(planeTarget, args.WorldHandle, new Color());
            }

            foreach(RendererMetaData sprite in planeEntry.Item2)
            {
                //setup the mousemaplookup shader for use in DrawIcon()
                byte[] rgba = BitConverter.GetBytes(sprite.GetHashCode());
                Color targetColor = new Color(rgba[0],rgba[1],rgba[2],255); //TODO - this could result in misclicks due to hash-collision since we ditch a whole byte.
                MouseMapLookup[targetColor] = sprite.ClickUID;
                _blockColorInstance.SetParameter("targetColor", targetColor); //Set shader instance's block colour for the mouse map

                if(sprite.RenderSource.Length > 0 && _renderSourceLookup.TryGetValue(sprite.RenderSource, out var renderSourceTexture)){
                    DrawIcon(args.WorldHandle, planeTarget, sprite, (-screenArea.BottomLeft)-(args.WorldAABB.Size/2)+new Vector2(0.5f,0.5f), renderSourceTexture.Texture);
                } else {
                    DrawIcon(args.WorldHandle, planeTarget, sprite, -screenArea.BottomLeft);
                }
            }

            if(planeEntry.Item1 != null){
                //this was a plane master, so draw the plane onto the baseTarget and return it
                DrawIcon(args.WorldHandle, baseTarget, planeEntry.Item1, Vector2.Zero, planeTarget.Texture);
                ReturnPingPongRenderTarget(planeTarget);
            }
        }

        if(MouseMapRenderEnabled) //if this is enabled, we just draw the mouse map
            args.WorldHandle.DrawTexture(mouseMapRenderTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), null);
        else
            args.WorldHandle.DrawTexture(baseTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), null); //draw the basetarget onto the world
        ReturnPingPongRenderTarget(baseTarget);
    }

    //handles underlays, overlays, appearance flags, images. Returns a list of icons and metadata for them to be sorted, so they can be drawn with DrawIcon()
    private List<RendererMetaData> ProcessIconComponents(DreamIcon icon, Vector2 position, EntityUid uid, Boolean isScreen, RendererMetaData? parentIcon = null, bool keepTogether = false, int tieBreaker = 0)
    {
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
        current.BlendMode = (int)icon.Appearance.BlendMode;
        current.MouseOpacity = icon.Appearance.MouseOpacity;


        Matrix3 iconAppearanceTransformMatrix = new(new float[] { //reverse rotation transforms because of 180 flip from rendertarget->world transform
                icon.Appearance.Transform[0], -icon.Appearance.Transform[1], icon.Appearance.Transform[4],
                -icon.Appearance.Transform[2], icon.Appearance.Transform[3], icon.Appearance.Transform[5],
                0, 0, 1
            });

        if(parentIcon != null){
            current.ClickUID = parentIcon.ClickUID;
            if((icon.Appearance.AppearanceFlags & 2) == 2 || keepTogether) //RESET_COLOR
                current.ColorToApply = icon.Appearance.Color;
            else
                current.ColorToApply = parentIcon.ColorToApply;

            if((icon.Appearance.AppearanceFlags & 4) == 4 || keepTogether) //RESET_ALPHA
                current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            else
                current.AlphaToApply = parentIcon.AlphaToApply;

            if((icon.Appearance.AppearanceFlags & 8) == 8 || keepTogether) //RESET_TRANSFORM
                current.TransformToApply = iconAppearanceTransformMatrix;
            else
                current.TransformToApply = parentIcon.TransformToApply;

            if(((int)icon.Appearance.Plane & -32767) == -32767) //FLOAT_PLANE
                current.Plane = parentIcon.Plane + ((int)icon.Appearance.Plane & ~(-32767));
            else
                current.Plane = icon.Appearance.Plane;

            if(icon.Appearance.Layer == -1) //FLOAT_LAYER
                current.Layer = parentIcon.Layer;
            else
                current.Layer = icon.Appearance.Layer;
        } else {
            current.ColorToApply = icon.Appearance.Color;
            current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            current.TransformToApply = iconAppearanceTransformMatrix;
            current.Plane = icon.Appearance.Plane;
            current.Layer = icon.Appearance.Layer;
        }

        keepTogether = keepTogether || ((current.AppearanceFlags & 32) == 32); //KEEP_TOGETHER

        if(current.RenderTarget.Length > 0 && current.RenderTarget[0]!='*'){ //if the rendertarget starts with *, we don't render it. If it doesn't we create a placeholder rendermetadata to position it correctly
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
        } else if(current.RenderTarget.Length > 0) {
            current.RenderTarget = current.RenderTarget.Substring(1); //cut the * off, we're done with it
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

            if(!keepTogether || (underlay.Appearance.AppearanceFlags & 64) == 64) //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
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

            if(!keepTogether || (overlay.Appearance.AppearanceFlags & 64) == 64) //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
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
            foreach(RendererMetaData KTItem in current.KeepTogetherGroup){
                if(KTItem.KeepTogetherGroup != null)
                    flatKTGroup.AddRange(KTItem.KeepTogetherGroup);
                flatKTGroup.Add(KTItem);
                KTItem.KeepTogetherGroup = null; //might need to be Clear()
            }
            current.KeepTogetherGroup = flatKTGroup;
        }
        result.Add(current);
        return result;
    }

    private IRenderTexture RentPingPongRenderTarget(Vector2i size) {
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

    private void ReturnPingPongRenderTarget(IRenderTexture rental) {
        if (!_renderTargetCache.TryGetValue(rental.Size, out var storeList))
            storeList = new List<IRenderTexture>(4);

        storeList.Add(rental);
        _renderTargetCache[rental.Size] = storeList;
    }

    private void ClearRenderTarget(IRenderTexture target, DrawingHandleWorld handle, Color clearColor)
    {
        handle.RenderInRenderTarget(target, () => {}, clearColor);
    }

    private void DrawIcon(DrawingHandleWorld handle, IRenderTarget renderTarget, RendererMetaData iconMetaData, Vector2 positionOffset, Texture textureOverride = null) {
        DreamIcon icon = iconMetaData.MainIcon;

        Vector2 position = iconMetaData.Position + positionOffset;
        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;

        Texture frame;
        if(textureOverride != null) {
            frame = textureOverride;
            //TODO figure out why this is necessary and delete it from existence.
            IRenderTexture TempTexture = RentPingPongRenderTarget(frame.Size);
            ClearRenderTarget(TempTexture, handle, Color.Black.WithAlpha(0));
            handle.RenderInRenderTarget(TempTexture , () => {
                    handle.DrawRect(new Box2(Vector2.Zero, TempTexture.Size), new Color());
                    handle.DrawTextureRect(frame, new Box2(Vector2.Zero, TempTexture.Size));
                }, Color.Transparent);
            frame = TempTexture.Texture;
            ReturnPingPongRenderTarget(TempTexture);
        }
        else
            frame = icon.CurrentFrame;



        if(iconMetaData.KeepTogetherGroup != null && iconMetaData.KeepTogetherGroup.Count > 0)
        {
            //store the parent's transform, color, blend, and alpha - then clear them for drawing to the render target
            Matrix3 KTParentTransform = iconMetaData.TransformToApply;
            iconMetaData.TransformToApply = Matrix3.Identity;
            Color KTParentColor = iconMetaData.ColorToApply;
            iconMetaData.ColorToApply = Color.White;
            float KTParentAlpha = iconMetaData.AlphaToApply;
            iconMetaData.AlphaToApply = 1f;
            int KTParentBlendMode = iconMetaData.BlendMode;
            iconMetaData.BlendMode = 0; //BLEND_DEFAULT

            List<RendererMetaData> KTItems = new List<RendererMetaData>(iconMetaData.KeepTogetherGroup.Count+1);
            KTItems.Add(iconMetaData);
            KTItems.AddRange(iconMetaData.KeepTogetherGroup);
            iconMetaData.KeepTogetherGroup.Clear();

            KTItems.Sort();
            //draw it onto an additional render target that we can return immediately for correction of transform
            IRenderTexture TempTexture = RentPingPongRenderTarget(frame.Size);
            ClearRenderTarget(TempTexture, handle, Color.Transparent);

            foreach(RendererMetaData KTItem in KTItems){
                DrawIcon(handle, TempTexture, KTItem, -KTItem.Position);
            }
            //but keep the handle to the final KT group's render target so we don't override it later in the render cycle
            IRenderTexture KTTexture = RentPingPongRenderTarget(TempTexture.Size);
            handle.RenderInRenderTarget(KTTexture , () => {
                    handle.DrawRect(new Box2(Vector2.Zero, TempTexture.Size), new Color());
                    handle.DrawTextureRect(TempTexture.Texture, new Box2(Vector2.Zero, TempTexture.Size));
                }, Color.Transparent);
            frame = KTTexture.Texture;
            ReturnPingPongRenderTarget(TempTexture);
            //now restore the original color, alpha, blend, and transform so they can be applied to the render target as a whole
            iconMetaData.TransformToApply = KTParentTransform;
            iconMetaData.ColorToApply = KTParentColor;
            iconMetaData.AlphaToApply = KTParentAlpha;
            iconMetaData.BlendMode = KTParentBlendMode;

            _renderTargetsToReturn.Push(KTTexture);
        }

        if(frame != null && (icon.Appearance == null || icon.Appearance.Filters.Count == 0)) {
            //faster path for rendering unfiltered sprites
            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.UseShader(_blendmodeInstances.TryGetValue(iconMetaData.BlendMode, out var value) ? value : null);
                    handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                    handle.DrawTextureRect(frame,
                        new Box2(pixelPosition, pixelPosition+frame.Size),
                        iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                    handle.UseShader(null);
                }, null);
            if(iconMetaData.MouseOpacity != MouseOpacity.Transparent)
                handle.RenderInRenderTarget(mouseMapRenderTarget, () => {
                        handle.UseShader(_blockColorInstance);
                        handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                        handle.DrawTextureRect(frame,
                            new Box2(pixelPosition, pixelPosition+frame.Size),
                            Color.White);
                        handle.UseShader(null);
                }, null);

        } else if (frame != null) {
            IRenderTexture ping = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture pong = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture tmpHolder;

            handle.RenderInRenderTarget(pong,
                () => {
                    handle.DrawTextureRect(frame,
                        new Box2(Vector2.Zero + (frame.Size / 2), frame.Size + (frame.Size / 2)),
                        iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                }, Color.Black.WithAlpha(0));

            foreach (DreamFilter filterId in icon.Appearance.Filters) {
                ShaderInstance s = _appearanceSystem.GetFilterShader(filterId, _renderSourceLookup);

                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.UseShader(s);
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                    handle.UseShader(null);
                }, Color.Black.WithAlpha(0));

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
            }

            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.UseShader(_blendmodeInstances.TryGetValue(iconMetaData.BlendMode, out var value) ? value : null);
                    handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                    handle.DrawTextureRect(pong.Texture,
                        new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                        null);
                    handle.UseShader(null);
                }, null);
            if(iconMetaData.MouseOpacity != MouseOpacity.Transparent)
                handle.RenderInRenderTarget(mouseMapRenderTarget, () => {
                        handle.UseShader(_blockColorInstance);
                        handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                        handle.DrawTextureRect(pong.Texture,
                            new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                            Color.White);
                        handle.UseShader(null);
                    }, null);
            ReturnPingPongRenderTarget(ping);
            ReturnPingPongRenderTarget(pong);
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
    public float Plane; //true plane value may be different from appearance plane value, due to special flags
    public float Layer; //ditto for layer
    public EntityUid UID;
    public EntityUid ClickUID; //the UID of the object clicks on this should be passed to (ie, for overlays)
    public Boolean IsScreen;
    public int TieBreaker; //Used for biasing render order (ie, for overlays)
    public Color ColorToApply;
    public float AlphaToApply;
    public Matrix3 TransformToApply;
    public String RenderSource;
    public String RenderTarget;
    public List<RendererMetaData>? KeepTogetherGroup;
    public int AppearanceFlags;
    public int BlendMode;
    public MouseOpacity MouseOpacity;

    public RendererMetaData(){
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
        AlphaToApply = 1.0f;
        TransformToApply = Matrix3.Identity;
        RenderSource = "";
        RenderTarget = "";
        KeepTogetherGroup = null; //don't actually need to allocate this 90% of the time
        AppearanceFlags = 0;
        BlendMode = 0;
        MouseOpacity = MouseOpacity.Transparent;
    }

    public int CompareTo(RendererMetaData other) {
        int val = 0;

        //Render target and source ordering is done first.
        //Anything with a render target goes first
        //Anything with a render source which points to a render target must come *after* that render_target
        val = (this.RenderTarget.Length > 0).CompareTo(other.RenderTarget.Length > 0);
        if (val != 0) {
            return -val;
        }
        if(this.RenderSource.Length > 0)
        {
            if(this.RenderSource == other.RenderTarget)
                return 1;
        }

        //We now return to your regularly scheduled sprite render order

        //Plane
        val =  this.Plane.CompareTo(other.Plane);
        if (val != 0) {
            return val;
        }
        val = (((int)this.AppearanceFlags & 128) == 128).CompareTo(((int)other.AppearanceFlags & 128) == 128); //appearance_flags & PLANE_MASTER
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
            //special handling for EFFECTS_LAYER and BACKGROUND_LAYER
            int effects_layer = (((int)this.Layer & 5000) == 5000).CompareTo(((int)other.Layer & 5000) == 5000);
            if(effects_layer != 0)
                return effects_layer;
            int background_layer = (((int)this.Layer & 20000) == 20000).CompareTo(((int)other.Layer & 20000) == 20000);
            if(background_layer != 0)
                return -background_layer; //flipped because background_layer flag forces it to the back
            return val;
        }

        //Finally, tie-breaker - in BYOND, this is order of creation of the sprites
        //for us, we use EntityUID, with a tie-breaker (for underlays/overlays)
        val = this.UID.CompareTo(other.UID);
        if (val != 0) {
            return val;
        }

        val = this.TieBreaker.CompareTo(other.TieBreaker);
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

public sealed class ToggleZFightingDebugCommand : IConsoleCommand {
    public string Command => "togglezfighting";
    public string Description => "Toggle checking for instances of z-fighting";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.CheckForZFighting = !screenOverlay.CheckForZFighting;
        }
    }
}
#endregion
