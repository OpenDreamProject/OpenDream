using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Map;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace OpenDreamRuntime {
    public sealed class AtomManager {
        public List<DreamObjectArea> Areas { get; } = new();
        public List<DreamObjectTurf> Turfs { get; } = new();
        public List<DreamObjectMovable> Movables { get; } = new();
        public List<DreamObjectMovable> Objects { get; } = new();
        public List<DreamObjectMob> Mobs { get; } = new();
        public int AtomCount => Areas.Count + Turfs.Count + Movables.Count + Objects.Count + Mobs.Count;

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly DreamObjectTree _objectTree = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly DreamResourceManager _resourceManager = default!;

        private readonly Dictionary<EntityUid, DreamObject> _entityToAtom = new();
        private readonly Dictionary<DreamObjectDefinition, IconAppearance> _definitionAppearanceCache = new();

        private ServerAppearanceSystem AppearanceSystem => _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
        private ServerAppearanceSystem? _appearanceSystem;

        public DreamObject GetAtom(int index) {
            // Order of world.contents:
            //  Mobs + Movables + Areas + Turfs

            if (index < Mobs.Count)
                return Mobs[index];

            // TODO: Movables and objects should be mixed together here
            index -= Mobs.Count;
            if (index < Objects.Count)
                return Objects[index];

            index -= Objects.Count;
            if (index < Movables.Count)
                return Movables[index];

            index -= Movables.Count;
            if (index < Areas.Count)
                return Areas[index];

            index -= Areas.Count;
            if (index < Turfs.Count)
                return Turfs[index];

            throw new IndexOutOfRangeException($"Cannot get atom at index {index}. There are only {AtomCount} atoms.");
        }

        public EntityUid CreateMovableEntity(DreamObjectMovable movable) {
            var entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            DMISpriteComponent sprite = _entityManager.AddComponent<DMISpriteComponent>(entity);
            sprite.SetAppearance(GetAppearanceFromDefinition(movable.ObjectDefinition));

            if (_entityManager.TryGetComponent(entity, out MetaDataComponent? metaData)) {
                metaData.EntityName = movable.GetDisplayName();
                metaData.EntityDescription = movable.Desc ?? string.Empty;
            }

            _entityToAtom.Add(entity, movable);
            return entity;
        }

        public bool TryGetMovableFromEntity(EntityUid entity, [NotNullWhen(true)] out DreamObject? movable) {
            return _entityToAtom.TryGetValue(entity, out movable);
        }

        public void DeleteMovableEntity(DreamObjectMovable movable) {
            _entityToAtom.Remove(movable.Entity);
            _entityManager.DeleteEntity(movable.Entity);
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
                case "glide_size":
                case "render_source":
                case "render_target":
                case "transform":
                    return true;

                // Get/SetAppearanceVar doesn't handle these
                case "overlays":
                case "underlays":
                case "filters":
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
                    value.TryGetValueAsInteger(out var dir);

                    if (dir <= 0) // Ignore any sets <= 0 or non-number
                        break;

                    if (dir > 0xFF) // Clamp to 1 byte
                        dir = 0xFF;

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
                    value.TryGetValueAsInteger(out int blendMode);
                    appearance.BlendMode = Enum.IsDefined((BlendMode)blendMode) ? (BlendMode)blendMode : BlendMode.Default;
                    break;
                case "appearance_flags":
                    value.TryGetValueAsInteger(out int flagsVar);
                    appearance.AppearanceFlags = (AppearanceFlags) flagsVar;
                    break;
                case "alpha":
                    value.TryGetValueAsFloat(out float floatAlpha);
                    appearance.Alpha = (byte) floatAlpha;
                    break;
                case "glide_size":
                    value.TryGetValueAsFloat(out float glideSize);
                    appearance.GlideSize = (byte) glideSize;
                    break;
                case "render_source":
                    value.TryGetValueAsString(out appearance.RenderSource);
                    break;
                case "render_target":
                    value.TryGetValueAsString(out appearance.RenderTarget);
                    break;
                case "transform":
                    float[] transformArray = value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var transform)
                        ? DreamObjectMatrix.MatrixToTransformFloatArray(transform)
                        : DreamObjectMatrix.IdentityMatrixArray;

                    appearance.Transform = transformArray;
                    break;
                // TODO: overlays, underlays, filters
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
                case "glide_size":
                    return new(appearance.GlideSize);
                case "render_source":
                    return (appearance.RenderSource != null)
                        ? new DreamValue(appearance.RenderSource)
                        : DreamValue.Null;
                case "render_target":
                    return (appearance.RenderTarget != null)
                        ? new DreamValue(appearance.RenderTarget)
                        : DreamValue.Null;
                case "transform":
                    var transform = appearance.Transform;
                    var matrix = DreamObjectMatrix.MakeMatrix(_objectTree,
                        transform[0], transform[2], transform[4],
                        transform[1], transform[3], transform[5]);

                    return new(matrix);
                // TODO: overlays, underlays, filters
                //       Those are handled separately by whatever is calling GetAppearanceVar currently
                default:
                    throw new ArgumentException($"Invalid appearance var {varName}");
            }
        }

        /// <summary>
        /// Gets an atom's appearance.
        /// </summary>
        /// <param name="atom">The atom to find the appearance of.</param>
        public IconAppearance? MustGetAppearance(DreamObject atom) {
            return atom switch {
                DreamObjectTurf turf => AppearanceSystem.MustGetAppearance(turf.AppearanceId),
                DreamObjectMovable movable => movable.SpriteComponent.Appearance,
                DreamObjectArea => new IconAppearance(),
                DreamObjectImage image => image.Appearance,
                _ => throw new Exception($"Cannot get appearance of {atom}")
            };
        }

        /// <summary>
        /// Optionally looks up for an appearance. Does not try to create a new one when one is not found for this atom.
        /// </summary>
        public bool TryGetAppearance(DreamObject atom, [NotNullWhen(true)] out IconAppearance? appearance) {
            if (atom is DreamObjectTurf turf)
                appearance = AppearanceSystem.MustGetAppearance(turf.AppearanceId);
            else if (atom is DreamObjectMovable movable)
                appearance = movable.SpriteComponent.Appearance;
            else if (atom is DreamObjectImage image)
                appearance = image.Appearance;
            else
                appearance = null;

            return appearance is not null;
        }

        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update) {
            var appearance = MustGetAppearance(atom);
            appearance = (appearance != null) ? new(appearance) : new(); // Clone the appearance

            update(appearance);
            SetAtomAppearance(atom, appearance);
        }

        public void SetAtomAppearance(DreamObject atom, IconAppearance appearance) {
            if (atom is DreamObjectTurf turf) {
                _dreamMapManager.SetTurfAppearance(turf, appearance);
            } else if (atom is DreamObjectMovable movable) {
                movable.SpriteComponent.SetAppearance(appearance);
            } else if (atom is DreamObjectImage image) {
                image.Appearance = appearance;
            }
        }

        public void AnimateAppearance(DreamObjectAtom atom, TimeSpan duration, Action<IconAppearance> animate) {
            if (atom is not DreamObjectMovable movable)
                return; //Animating non-movables is unimplemented

            IconAppearance appearance = new IconAppearance(movable.SpriteComponent.Appearance);

            animate(appearance);

            // Don't send the updated appearance to clients, they will animate it
            movable.SpriteComponent.SetAppearance(appearance, dirty: false);

            NetEntity ent = _entityManager.GetNetEntity(movable.Entity);

            AppearanceSystem.Animate(ent, appearance, duration);
        }

        public bool TryCreateAppearanceFrom(DreamValue value, [NotNullWhen(true)] out IconAppearance? appearance) {
            if (value.TryGetValueAsAppearance(out var copyFromAppearance)) {
                appearance = new(copyFromAppearance);
                return true;
            }

            if (value.TryGetValueAsDreamObject<DreamObjectImage>(out var copyFromImage)) {
                appearance = new(copyFromImage.Appearance!);
                return true;
            }

            if (value.TryGetValueAsType(out var copyFromType)) {
                appearance = GetAppearanceFromDefinition(copyFromType.ObjectDefinition);
                return true;
            }

            if (value.TryGetValueAsDreamObject<DreamObjectAtom>(out var copyFromAtom)) {
                appearance = new(MustGetAppearance(copyFromAtom));
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

        public IconAppearance GetAppearanceFromDefinition(DreamObjectDefinition def) {
            if (_definitionAppearanceCache.TryGetValue(def, out var appearance))
                return appearance;

            def.TryGetVariable("icon", out var iconVar);
            def.TryGetVariable("icon_state", out var stateVar);
            def.TryGetVariable("color", out var colorVar);
            def.TryGetVariable("alpha", out var alphaVar);
            def.TryGetVariable("glide_size", out var glideSizeVar);
            def.TryGetVariable("dir", out var dirVar);
            def.TryGetVariable("invisibility", out var invisibilityVar);
            def.TryGetVariable("opacity", out var opacityVar);
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
            SetAppearanceVar(appearance, "glide_size", glideSizeVar);
            SetAppearanceVar(appearance, "dir", dirVar);
            SetAppearanceVar(appearance, "invisibility", invisibilityVar);
            SetAppearanceVar(appearance, "opacity", opacityVar);
            SetAppearanceVar(appearance, "mouse_opacity", mouseVar);
            SetAppearanceVar(appearance, "pixel_x", xVar);
            SetAppearanceVar(appearance, "pixel_y", yVar);
            SetAppearanceVar(appearance, "layer", layerVar);
            SetAppearanceVar(appearance, "plane", planeVar);
            SetAppearanceVar(appearance, "render_source", renderSourceVar);
            SetAppearanceVar(appearance, "render_target", renderTargetVar);
            SetAppearanceVar(appearance, "blend_mode", blendModeVar);
            SetAppearanceVar(appearance, "appearance_flags", appearanceFlagsVar);

            if (def.TryGetVariable("transform", out var transformVar) && transformVar.TryGetValueAsDreamObject<DreamObjectMatrix>(out var transformMatrix)) {
                appearance.Transform = DreamObjectMatrix.MatrixToTransformFloatArray(transformMatrix);
            }

            _definitionAppearanceCache.Add(def, appearance);
            return appearance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int X, int Y, int Z) GetAtomPosition(DreamObjectAtom atom) {
            return atom switch {
                DreamObjectMovable { Position: var pos, Z: var z } => (pos.X, pos.Y, z),
                DreamObjectTurf turf => (turf.X, turf.Y, turf.Z),
                DreamObjectArea area => (area.X, area.Y, area.Z),
                _ => ThrowCantGetPosition(atom)
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (int X, int Y, int) ThrowCantGetPosition(DreamObjectAtom atom)
        {
            throw new Exception($"Cannot get the position of {atom}");
        }
    }
}
