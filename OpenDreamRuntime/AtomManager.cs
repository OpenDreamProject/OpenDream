using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Map;

namespace OpenDreamRuntime {
    internal sealed class AtomManager : IAtomManager {
        public List<DreamObject> Areas { get; } = new();
        public List<DreamObject> Turfs { get; } = new();
        public List<DreamObject> Movables { get; } = new();
        public List<DreamObject> Objects { get; } = new();
        public List<DreamObject> Mobs { get; } = new();
        public int AtomCount => Areas.Count + Turfs.Count + Movables.Count + Objects.Count + Mobs.Count;

        //TODO: Maybe turn these into a special DreamList, similar to DreamListVars?
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; } = new();
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; } = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly DreamResourceManager _resourceManager = default!;

        private readonly Dictionary<DreamObject, EntityUid> _atomToEntity = new();
        private readonly Dictionary<EntityUid, DreamObject> _entityToAtom = new();
        private readonly Dictionary<DreamObjectDefinition, IconAppearance> _definitionAppearanceCache = new();

        private ServerAppearanceSystem? _appearanceSystem;

        public DreamObject GetAtom(int index) {
            if (index < Areas.Count)
                return Areas[index];

            index -= Areas.Count;
            if (index < Turfs.Count)
                return Turfs[index];

            index -= Turfs.Count;
            if (index < Movables.Count)
                return Movables[index];

            index -= Movables.Count;
            if (index < Objects.Count)
                return Objects[index];

            index -= Objects.Count;
            if (index < Mobs.Count)
                return Mobs[index];

            throw new IndexOutOfRangeException($"Cannot get atom at index {index}. There are only {AtomCount} atoms.");
        }

        public EntityUid CreateMovableEntity(DreamObject atom) {
            if (_atomToEntity.TryGetValue(atom, out var entity))
                return entity;

            entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            DMISpriteComponent sprite = _entityManager.AddComponent<DMISpriteComponent>(entity);
            sprite.SetAppearance(GetAppearanceFromDefinition(atom.ObjectDefinition));

            if (_entityManager.TryGetComponent(entity, out MetaDataComponent? metaData)) {
                atom.GetVariable("desc").TryGetValueAsString(out string desc);
                metaData.EntityName = atom.GetDisplayName();
                metaData.EntityDescription = desc;
            }

            _atomToEntity.Add(atom, entity);
            _entityToAtom.Add(entity, atom);
            return entity;
        }

        public EntityUid GetMovableEntity(DreamObject movable) {
            return _atomToEntity.ContainsKey(movable) ? _atomToEntity[movable] : CreateMovableEntity(movable);
        }

        public bool TryGetMovableEntity(DreamObject movable, out EntityUid entity) {
            return _atomToEntity.TryGetValue(movable, out entity);
        }

        public bool TryGetMovableFromEntity(EntityUid entity, [NotNullWhen(true)] out DreamObject? movable) {
            return _entityToAtom.TryGetValue(entity, out movable);
        }

        public void DeleteMovableEntity(DreamObject movable) {
            EntityUid entity = GetMovableEntity(movable);

            _entityToAtom.Remove(entity);
            _atomToEntity.Remove(movable);
            _entityManager.DeleteEntity(entity);
        }

        public bool IsValidAppearanceVar(string name) {
            switch (name) {
                case "icon":
                case "icon_state":
                case "dir":
                case "pixel_x":
                case "pixel_y":
                case "color":
                case "layer":
                case "invisibility":
                case "opacity":
                case "mouse_opacity":
                case "plane":
                case "blend_mode":
                case "appearance_flags":
                case "alpha":
                case "render_source":
                case "render_target":
                    return true;

                // Get/SetAppearanceVar doesn't handle these
                case "overlays":
                case "underlays":
                case "filters":
                case "transform":
                default:
                    return false;
            }
        }

        public void SetAppearanceVar(IconAppearance appearance, string varName, DreamValue value) {
            switch (varName) {
                case "icon":
                    if (_resourceManager.TryLoadIcon(value, out var icon)) {
                        appearance.Icon = icon.Id;
                    } else {
                        appearance.Icon = null;
                    }

                    break;
                case "icon_state":
                    value.TryGetValueAsString(out appearance.IconState);
                    break;
                case "dir":
                    //TODO figure out the weird inconsistencies with this being internally clamped
                    value.TryGetValueAsInteger(out var dir);

                    appearance.Direction = (AtomDirection)dir;
                    break;
                case "pixel_x":
                    value.TryGetValueAsInteger(out appearance.PixelOffset.X);
                    break;
                case "pixel_y":
                    value.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                    break;
                case "color":
                    if(value.TryGetValueAsDreamList(out var list)) {
                        if(DreamProcNativeHelpers.TryParseColorMatrix(list, out var matrix)) {
                            appearance.SetColor(in matrix);
                            break;
                        }

                        throw new ArgumentException($"Cannot set appearance's color to {value}");
                    }

                    value.TryGetValueAsString(out var colorString);
                    colorString ??= "white";
                    appearance.SetColor(colorString);
                    break;
                case "layer":
                    value.TryGetValueAsFloat(out appearance.Layer);
                    break;
                case "invisibility":
                    value.TryGetValueAsInteger(out int vis);
                    vis = Math.Clamp(vis, -127, 127); // DM ref says [0, 101]. BYOND compiler says [-127, 127]
                    appearance.Invisibility = vis;
                    break;
                case "opacity":
                    value.TryGetValueAsInteger(out var opacity);
                    appearance.Opacity = (opacity != 0);
                    break;
                case "mouse_opacity":
                    //TODO figure out the weird inconsistencies with this being internally clamped
                    value.TryGetValueAsInteger(out var mouseOpacity);
                    appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
                    break;
                case "plane":
                    value.TryGetValueAsInteger(out appearance.Plane);
                    break;
                case "blend_mode":
                    value.TryGetValueAsFloat(out float blendMode);
                    appearance.BlendMode = Enum.IsDefined((BlendMode)blendMode) ? (BlendMode)blendMode : BlendMode.BLEND_DEFAULT;
                    break;
                case "appearance_flags":
                    value.TryGetValueAsInteger(out int flagsVar);
                    appearance.AppearanceFlags = (AppearanceFlags) flagsVar;
                    break;
                case "alpha":
                    value.TryGetValueAsFloat(out float floatAlpha);
                    appearance.Alpha = (byte) floatAlpha;
                    break;
                case "render_source":
                    value.TryGetValueAsString(out appearance.RenderSource);
                    break;
                case "render_target":
                    value.TryGetValueAsString(out appearance.RenderTarget);
                    break;
                // TODO: overlays, underlays, filters, transform
                //       Those are handled separately by whatever is calling SetAppearanceVar currently
                default:
                    throw new ArgumentException($"Invalid appearance var {varName}");
            }
        }

        public DreamValue GetAppearanceVar(IconAppearance appearance, string varName) {
            switch (varName) {
                case "icon":
                    if (appearance.Icon == null)
                        return DreamValue.Null;
                    if (!_resourceManager.TryLoadResource(appearance.Icon.Value, out var iconResource))
                        return DreamValue.Null;

                    return new(iconResource);
                case "icon_state":
                    if (appearance.IconState == null)
                        return DreamValue.Null;

                    return new(appearance.IconState);
                case "dir":
                    return new((int) appearance.Direction);
                case "pixel_x":
                    return new(appearance.PixelOffset.X);
                case "pixel_y":
                    return new(appearance.PixelOffset.Y);
                case "color":
                    if(!appearance.ColorMatrix.Equals(ColorMatrix.Identity)) {
                        var matrixList = _objectTree.CreateList(20);
                        foreach (float entry in appearance.ColorMatrix.GetValues())
                            matrixList.AddValue(new DreamValue(entry));
                        return new DreamValue(matrixList);
                    }

                    if (appearance.Color == Color.White) {
                        return DreamValue.Null;
                    }

                    return new DreamValue(appearance.Color.ToHexNoAlpha().ToLower()); // BYOND quirk, does not return the alpha channel for some reason.
                case "layer":
                    return new(appearance.Layer);
                case "invisibility":
                    return new(appearance.Invisibility);
                case "opacity":
                    return appearance.Opacity ? DreamValue.True : DreamValue.False;
                case "mouse_opacity":
                    return new((int)appearance.MouseOpacity);
                case "plane":
                    return new(appearance.Plane);
                case "blend_mode":
                    return new((int) appearance.BlendMode);
                case "appearance_flags":
                    return new((int) appearance.AppearanceFlags);
                case "alpha":
                    return new(appearance.Alpha);
                case "render_source":
                    return new(appearance.RenderSource);
                case "render_target":
                    return new(appearance.RenderTarget);
                // TODO: overlays, underlays, filters, transform
                //       Those are handled separately by whatever is calling GetAppearanceVar currently
                default:
                    throw new ArgumentException($"Invalid appearance var {varName}");
            }
        }

        /// <summary>
        /// Looks for an appearance and, if one does not exist, usually creates a new one. <br/>
        /// If used with a turf, this will fail and throw some kinda KeyNotFoundException down the line.
        /// </summary>
        /// <param name="atom">The atom to find the appearance of.</param>
        public IconAppearance? MustGetAppearance(DreamObject atom) {
            return atom.IsSubtypeOf(_objectTree.Turf)
                ? _dreamMapManager.MustGetTurfAppearance(atom)
                : _entityManager.GetComponent<DMISpriteComponent>(GetMovableEntity(atom)).Appearance;
        }

        /// <summary>
        /// Optionally looks up for an appearance. Does not try to create a new one when one is not found for this atom.
        /// </summary>
        public bool TryGetAppearance(DreamObject atom, [NotNullWhen(true)] out IconAppearance? appearance) {
            if (atom.IsSubtypeOf(_objectTree.Turf))
                _dreamMapManager.TryGetTurfAppearance(atom, out appearance);
            else if (TryGetMovableEntity(atom, out var entity)) // If a movable is already on the map
                appearance = _entityManager.GetComponent<DMISpriteComponent>(entity).Appearance;
            else
                appearance = null;
            return appearance is not null;
        }

        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update) {
            if (atom.IsSubtypeOf(_objectTree.Turf)) {
                IconAppearance appearance = new IconAppearance(_dreamMapManager.MustGetTurfAppearance(atom));
                update(appearance);
                _dreamMapManager.SetTurfAppearance(atom, appearance);
            } else if (atom.IsSubtypeOf(_objectTree.Movable)) {
                if (!_entityManager.TryGetComponent<DMISpriteComponent>(GetMovableEntity(atom), out var sprite))
                    return;

                IconAppearance appearance = new IconAppearance(sprite.Appearance);
                update(appearance);
                sprite.SetAppearance(appearance);
            }
        }

        public void AnimateAppearance(DreamObject atom, TimeSpan duration, Action<IconAppearance> animate) {
            if (!atom.IsSubtypeOf(_objectTree.Movable))
                return; //Animating non-movables is unimplemented
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(GetMovableEntity(atom), out var sprite))
                return;

            IconAppearance appearance = new IconAppearance(sprite.Appearance);

            animate(appearance);

            // Don't send the updated appearance to clients, they will animate it
            sprite.SetAppearance(appearance, dirty: false);

            _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
            _appearanceSystem.Animate(GetMovableEntity(atom), appearance, duration);
        }

        public bool TryCreateAppearanceFrom(DreamValue value, [NotNullWhen(true)] out IconAppearance? appearance) {
            if (value.TryGetValueAsAppearance(out var copyFromAppearance)) {
                appearance = new(copyFromAppearance);
                return true;
            }

            if (value.TryGetValueAsDreamObjectOfType(_objectTree.Image, out var copyFromImage)) {
                appearance = new(DreamMetaObjectImage.ObjectToAppearance[copyFromImage]);
                return true;
            }

            if (value.TryGetValueAsType(out var copyFromType)) {
                appearance = GetAppearanceFromDefinition(copyFromType.ObjectDefinition);
                return true;
            }

            if (value.TryGetValueAsDreamObjectOfType(_objectTree.Atom, out var copyFromAtom)) {
                appearance = CreateAppearanceFromAtom(copyFromAtom);
                return true;
            }

            if (_resourceManager.TryLoadIcon(value, out var iconResource)) {
                appearance = new IconAppearance() {
                    Icon = iconResource.Id
                };

                return true;
            }

            appearance = null;
            return false;
        }

        public IconAppearance CreateAppearanceFromAtom(DreamObject atom) {
            if (TryGetAppearance(atom, out var appearance))
                return new(appearance); // Just return a copy

            appearance = new IconAppearance();
            SetAppearanceVar(appearance, "icon", atom.GetVariable("icon"));
            SetAppearanceVar(appearance, "icon_state", atom.GetVariable("icon_state"));
            SetAppearanceVar(appearance, "color", atom.GetVariable("color"));
            SetAppearanceVar(appearance, "alpha", atom.GetVariable("alpha"));
            SetAppearanceVar(appearance, "dir", atom.GetVariable("dir"));
            SetAppearanceVar(appearance, "invisibility", atom.GetVariable("invisibility"));
            SetAppearanceVar(appearance, "opacity", atom.GetVariable("opacity"));
            SetAppearanceVar(appearance, "mouse_opacity", atom.GetVariable("mouse_opacity"));
            SetAppearanceVar(appearance, "pixel_x", atom.GetVariable("pixel_x"));
            SetAppearanceVar(appearance, "pixel_y", atom.GetVariable("pixel_y"));
            SetAppearanceVar(appearance, "layer", atom.GetVariable("layer"));
            SetAppearanceVar(appearance, "plane", atom.GetVariable("plane"));
            SetAppearanceVar(appearance, "blend_mode", atom.GetVariable("blend_mode"));
            SetAppearanceVar(appearance, "render_source", atom.GetVariable("render_source"));
            SetAppearanceVar(appearance, "render_target", atom.GetVariable("render_target"));
            SetAppearanceVar(appearance, "appearance_flags", atom.GetVariable("appearance_flags"));

            if (atom.GetVariable("transform").TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out var transformMatrix)) {
                appearance.Transform = DreamMetaObjectMatrix.MatrixToTransformFloatArray(transformMatrix);
            }

            return appearance;
        }

        public IconAppearance GetAppearanceFromDefinition(DreamObjectDefinition def) {
            if (_definitionAppearanceCache.TryGetValue(def, out var appearance))
                return appearance;

            def.TryGetVariable("icon", out var iconVar);
            def.TryGetVariable("icon_state", out var stateVar);
            def.TryGetVariable("color", out var colorVar);
            def.TryGetVariable("alpha", out var alphaVar);
            def.TryGetVariable("dir", out var dirVar);
            def.TryGetVariable("invisibility", out var invisibilityVar);
            def.TryGetVariable("mouse_opacity", out var mouseVar);
            def.TryGetVariable("pixel_x", out var xVar);
            def.TryGetVariable("pixel_y", out var yVar);
            def.TryGetVariable("layer", out var layerVar);
            def.TryGetVariable("plane", out var planeVar);
            def.TryGetVariable("render_source", out var renderSourceVar);
            def.TryGetVariable("render_target", out var renderTargetVar);
            def.TryGetVariable("blend_mode", out var blendModeVar);
            def.TryGetVariable("appearance_flags", out var appearanceFlagsVar);

            appearance = new IconAppearance();
            SetAppearanceVar(appearance, "icon", iconVar);
            SetAppearanceVar(appearance, "icon_state", stateVar);
            SetAppearanceVar(appearance, "color", colorVar);
            SetAppearanceVar(appearance, "alpha", alphaVar);
            SetAppearanceVar(appearance, "dir", dirVar);
            SetAppearanceVar(appearance, "invisibility", invisibilityVar);
            SetAppearanceVar(appearance, "mouse_opacity", mouseVar);
            SetAppearanceVar(appearance, "pixel_x", xVar);
            SetAppearanceVar(appearance, "pixel_y", yVar);
            SetAppearanceVar(appearance, "layer", layerVar);
            SetAppearanceVar(appearance, "plane", planeVar);
            SetAppearanceVar(appearance, "render_source", renderSourceVar);
            SetAppearanceVar(appearance, "render_target", renderTargetVar);
            SetAppearanceVar(appearance, "blend_mode", blendModeVar);
            SetAppearanceVar(appearance, "appearance_flags", appearanceFlagsVar);

            if (def.TryGetVariable("transform", out var transformVar) && transformVar.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out var transformMatrix)) {
                appearance.Transform = DreamMetaObjectMatrix.MatrixToTransformFloatArray(transformMatrix);
            }

            _definitionAppearanceCache.Add(def, appearance);
            return appearance;
        }
    }

    public interface IAtomManager {
        public List<DreamObject> Areas { get; }
        public List<DreamObject> Turfs { get; }
        public List<DreamObject> Movables { get; }
        public List<DreamObject> Objects { get; }
        public List<DreamObject> Mobs { get; }
        public int AtomCount { get; }

        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; }
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; }

        public DreamObject GetAtom(int index);

        public EntityUid CreateMovableEntity(DreamObject movable);
        public EntityUid GetMovableEntity(DreamObject movable);

        public bool TryGetMovableEntity(DreamObject movable, out EntityUid entity);
        public bool TryGetMovableFromEntity(EntityUid entity, [NotNullWhen(true)] out DreamObject? movable);
        public void DeleteMovableEntity(DreamObject movable);

        public bool IsValidAppearanceVar(string varName);
        public void SetAppearanceVar(IconAppearance appearance, string varName, DreamValue value);
        public DreamValue GetAppearanceVar(IconAppearance appearance, string varName);

        public IconAppearance? MustGetAppearance(DreamObject atom);

        public bool TryGetAppearance(DreamObject atom, [NotNullWhen(true)] out IconAppearance? appearance);
        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update);
        public void AnimateAppearance(DreamObject atom, TimeSpan duration, Action<IconAppearance> animate);
        public bool TryCreateAppearanceFrom(DreamValue value, [NotNullWhen(true)] out IconAppearance? appearance);
        public IconAppearance CreateAppearanceFromAtom(DreamObject atom);
        public IconAppearance GetAppearanceFromDefinition(DreamObjectDefinition def);
    }
}
