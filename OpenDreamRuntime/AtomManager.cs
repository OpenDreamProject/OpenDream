using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.Map;

namespace OpenDreamRuntime {
    internal sealed class AtomManager : IAtomManager {
        //TODO: Maybe turn these into a special DreamList, similar to DreamListVars?
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; } = new();
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; } = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;

        private readonly Dictionary<DreamObject, EntityUid> _atomToEntity = new();
        private readonly Dictionary<EntityUid, DreamObject> _entityToAtom = new();

        private ServerAppearanceSystem? _appearanceSystem;

        public EntityUid CreateMovableEntity(DreamObject atom) {
            if (_atomToEntity.TryGetValue(atom, out var entity))
                return entity;

            entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            DMISpriteComponent sprite = _entityManager.AddComponent<DMISpriteComponent>(entity);
            sprite.SetAppearance(CreateAppearanceFromDefinition(atom.ObjectDefinition));

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

        public IconAppearance CreateAppearanceFromAtom(DreamObject atom) {
            IconAppearance appearance = new IconAppearance();

            _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
            _appearanceSystem.SetAppearanceVar(appearance, "icon", atom.GetVariable("icon"));
            _appearanceSystem.SetAppearanceVar(appearance, "icon_state", atom.GetVariable("icon_state"));
            _appearanceSystem.SetAppearanceVar(appearance, "color", atom.GetVariable("color"));
            _appearanceSystem.SetAppearanceVar(appearance, "alpha", atom.GetVariable("alpha"));
            _appearanceSystem.SetAppearanceVar(appearance, "dir", atom.GetVariable("dir"));
            _appearanceSystem.SetAppearanceVar(appearance, "invisibility", atom.GetVariable("invisibility"));
            _appearanceSystem.SetAppearanceVar(appearance, "opacity", atom.GetVariable("opacity"));
            _appearanceSystem.SetAppearanceVar(appearance, "mouse_opacity", atom.GetVariable("mouse_opacity"));
            _appearanceSystem.SetAppearanceVar(appearance, "pixel_x", atom.GetVariable("pixel_x"));
            _appearanceSystem.SetAppearanceVar(appearance, "pixel_y", atom.GetVariable("pixel_y"));
            _appearanceSystem.SetAppearanceVar(appearance, "layer", atom.GetVariable("layer"));
            _appearanceSystem.SetAppearanceVar(appearance, "plane", atom.GetVariable("plane"));
            _appearanceSystem.SetAppearanceVar(appearance, "blend_mode", atom.GetVariable("blend_mode"));
            _appearanceSystem.SetAppearanceVar(appearance, "render_source", atom.GetVariable("render_source"));
            _appearanceSystem.SetAppearanceVar(appearance, "render_target", atom.GetVariable("render_target"));
            _appearanceSystem.SetAppearanceVar(appearance, "appearance_flags", atom.GetVariable("appearance_flags"));

            if (atom.GetVariable("transform").TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out var transformMatrix)) {
                appearance.Transform = DreamMetaObjectMatrix.MatrixToTransformFloatArray(transformMatrix);
            }

            return appearance;
        }

        public IconAppearance CreateAppearanceFromDefinition(DreamObjectDefinition def) {
            IconAppearance appearance = new IconAppearance();

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

            _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
            _appearanceSystem.SetAppearanceVar(appearance, "icon", iconVar);
            _appearanceSystem.SetAppearanceVar(appearance, "icon_state", stateVar);
            _appearanceSystem.SetAppearanceVar(appearance, "color", colorVar);
            _appearanceSystem.SetAppearanceVar(appearance, "alpha", alphaVar);
            _appearanceSystem.SetAppearanceVar(appearance, "dir", dirVar);
            _appearanceSystem.SetAppearanceVar(appearance, "invisibility", invisibilityVar);
            _appearanceSystem.SetAppearanceVar(appearance, "mouse_opacity", mouseVar);
            _appearanceSystem.SetAppearanceVar(appearance, "pixel_x", xVar);
            _appearanceSystem.SetAppearanceVar(appearance, "pixel_y", yVar);
            _appearanceSystem.SetAppearanceVar(appearance, "layer", layerVar);
            _appearanceSystem.SetAppearanceVar(appearance, "plane", planeVar);
            _appearanceSystem.SetAppearanceVar(appearance, "render_source", renderSourceVar);
            _appearanceSystem.SetAppearanceVar(appearance, "render_target", renderTargetVar);
            _appearanceSystem.SetAppearanceVar(appearance, "blend_mode", blendModeVar);
            _appearanceSystem.SetAppearanceVar(appearance, "appearance_flags", appearanceFlagsVar);

            if (def.TryGetVariable("transform", out var transformVar) && transformVar.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out var transformMatrix)) {
                appearance.Transform = DreamMetaObjectMatrix.MatrixToTransformFloatArray(transformMatrix);
            }

            return appearance;
        }
    }

    public interface IAtomManager {
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; }
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; }

        public EntityUid CreateMovableEntity(DreamObject movable);
        public EntityUid GetMovableEntity(DreamObject movable);

        public bool TryGetMovableEntity(DreamObject movable, out EntityUid entity);
        public bool TryGetMovableFromEntity(EntityUid entity, [NotNullWhen(true)] out DreamObject? movable);
        public void DeleteMovableEntity(DreamObject movable);

        public IconAppearance? MustGetAppearance(DreamObject atom);

        public bool TryGetAppearance(DreamObject atom, [NotNullWhen(true)] out IconAppearance? appearance);
        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update);
        public void AnimateAppearance(DreamObject atom, TimeSpan duration, Action<IconAppearance> animate);
        public IconAppearance CreateAppearanceFromAtom(DreamObject atom);
        public IconAppearance CreateAppearanceFromDefinition(DreamObjectDefinition def);
    }
}
