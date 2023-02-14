using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
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

        private EntityUid CreateMovableEntity(DreamObject atom) {
            EntityUid entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            DMISpriteComponent sprite = _entityManager.AddComponent<DMISpriteComponent>(entity);
            sprite.SetAppearance(CreateAppearanceFromAtom(atom));

            if (_entityManager.TryGetComponent(entity, out MetaDataComponent? metaData)) {
                atom.GetVariable("desc").TryGetValueAsString(out string desc);
                metaData.EntityName = atom.GetDisplayName();
                metaData.EntityDescription = desc;
            }

            _atomToEntity.Add(atom, entity);
            _entityToAtom.Add(entity, atom);
            return entity;
        }

        public EntityUid GetMovableEntity(DreamObject movable)
        {
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

            if (atom.GetVariable("icon").TryGetValueAsDreamResource(out DreamResource? icon)) {
                appearance.Icon = icon.Id;
            }

            if (atom.GetVariable("icon_state").TryGetValueAsString(out string? iconState)) {
                appearance.IconState = iconState;
            }

            if (atom.GetVariable("color").TryGetValueAsString(out string? color)) {
                appearance.SetColor(color);
            }

            if (atom.GetVariable("dir").TryGetValueAsInteger(out int dir)) {
                appearance.Direction = (AtomDirection)dir;
            }

            if (atom.GetVariable("invisibility").TryGetValueAsInteger(out int invisibility)) {
                appearance.Invisibility = invisibility;
            }

            if (atom.GetVariable("opacity").TryGetValueAsInteger(out int opacity)) {
                appearance.Opacity = (opacity != 0);
            }

            if (atom.GetVariable("mouse_opacity").TryGetValueAsInteger(out int mouseOpacity)) {
                appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
            }

            atom.GetVariable("pixel_x").TryGetValueAsInteger(out int pixelX);
            atom.GetVariable("pixel_y").TryGetValueAsInteger(out int pixelY);
            appearance.PixelOffset = new Vector2i(pixelX, pixelY);

            if (atom.GetVariable("layer").TryGetValueAsFloat(out float layer)) {
                appearance.Layer = layer;
            }

            return appearance;
        }

        public IconAppearance CreateAppearanceFromDefinition(DreamObjectDefinition def) {
            IconAppearance appearance = new IconAppearance();

            if (def.TryGetVariable("icon", out var iconVar) && iconVar.TryGetValueAsDreamResource(out DreamResource icon)) {
                appearance.Icon = icon.Id;
            }

            if (def.TryGetVariable("icon_state", out var stateVar) && stateVar.TryGetValueAsString(out var iconState)) {
                appearance.IconState = iconState;
            }

            if (def.TryGetVariable("color", out var colorVar) && colorVar.TryGetValueAsString(out var color)) {
                appearance.SetColor(color);
            }

            if (def.TryGetVariable("dir", out var dirVar) && dirVar.TryGetValueAsInteger(out int dir)) {
                appearance.Direction = (AtomDirection)dir;
            }

            if (def.TryGetVariable("invisibility", out var invisVar) && invisVar.TryGetValueAsInteger(out int invisibility)) {
                appearance.Invisibility = invisibility;
            }

            if (def.TryGetVariable("mouse_opacity", out var mouseVar) && mouseVar.TryGetValueAsInteger(out int mouseOpacity)) {
                appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
            }

            def.TryGetVariable("pixel_x", out var xVar);
            xVar.TryGetValueAsInteger(out int pixelX);
            def.TryGetVariable("pixel_y", out var yVar);
            yVar.TryGetValueAsInteger(out int pixelY);
            appearance.PixelOffset = new Vector2i(pixelX, pixelY);

            if (def.TryGetVariable("layer", out var layerVar) && layerVar.TryGetValueAsFloat(out float layer)) {
                appearance.Layer = layer;
            }

            return appearance;
        }
    }

    public interface IAtomManager {
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; }
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; }

        public EntityUid GetMovableEntity(DreamObject movable);

        public bool TryGetMovableEntity(DreamObject movable, out EntityUid entity);
        public bool TryGetMovableFromEntity(EntityUid entity, [NotNullWhen(true)] out DreamObject? movable);
        public void DeleteMovableEntity(DreamObject movable);

        public IconAppearance? MustGetAppearance(DreamObject atom);

        public bool TryGetAppearance(DreamObject atom, out IconAppearance? appearance);
        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update);
        public void AnimateAppearance(DreamObject atom, TimeSpan duration, Action<IconAppearance> animate);
        public IconAppearance CreateAppearanceFromAtom(DreamObject atom);
        public IconAppearance CreateAppearanceFromDefinition(DreamObjectDefinition def);
    }
}
