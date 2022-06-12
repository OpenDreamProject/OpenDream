using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Map;

namespace OpenDreamRuntime {
    sealed class AtomManager : IAtomManager {
        //TODO: Maybe turn these into a special DreamList, similar to DreamListVars?
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; } = new();
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; } = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;

        private Dictionary<DreamObject, EntityUid> _atomToEntity = new();
        private Dictionary<EntityUid, DreamObject> _entityToAtom = new();

        public EntityUid CreateAtomEntity(DreamObject atom) {
            EntityUid entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            DMISpriteComponent sprite = _entityManager.AddComponent<DMISpriteComponent>(entity);
            sprite.SetAppearance(CreateAppearanceFromAtom(atom));

            if (_entityManager.TryGetComponent(entity, out MetaDataComponent metaData)) {
                atom.GetVariable("name").TryGetValueAsString(out string name);
                atom.GetVariable("desc").TryGetValueAsString(out string desc);
                metaData.EntityName = name;
                metaData.EntityDescription = desc;
            }

            _atomToEntity.Add(atom, entity);
            _entityToAtom.Add(entity, atom);
            return entity;
        }

        public EntityUid GetAtomEntity(DreamObject atom)
        {
            return _atomToEntity.ContainsKey(atom) ? _atomToEntity[atom] : CreateAtomEntity(atom);
        }

        public DreamObject GetAtomFromEntity(EntityUid entity) {
            _entityToAtom.TryGetValue(entity, out DreamObject atom);

            return atom;
        }

        public void DeleteAtomEntity(DreamObject atom) {
            EntityUid entity = GetAtomEntity(atom);

            _entityToAtom.Remove(entity);
            _atomToEntity.Remove(atom);
            _entityManager.DeleteEntity(entity);
        }

        public IconAppearance? GetAppearance(DreamObject atom) {
            return _entityManager.GetComponent<DMISpriteComponent>(GetAtomEntity(atom)).Appearance;
        }

        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update) {
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(GetAtomEntity(atom), out var sprite))
                return;
            IconAppearance appearance = new IconAppearance(sprite.Appearance);

            update(appearance);
            sprite.SetAppearance(appearance);
        }

        public void AnimateAppearance(DreamObject atom, TimeSpan duration, Action<IconAppearance> animate) {
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(GetAtomEntity(atom), out var sprite))
                return;
            IconAppearance appearance = new IconAppearance(sprite.Appearance);

            animate(appearance);

            // Don't send the updated appearance to clients, they will animate it
            sprite.SetAppearance(appearance, dirty: false);

            ServerAppearanceSystem appearanceSystem = EntitySystem.Get<ServerAppearanceSystem>();
            appearanceSystem.Animate(GetAtomEntity(atom), appearance, duration);
        }

        public IconAppearance CreateAppearanceFromAtom(DreamObject atom) {
            IconAppearance appearance = new IconAppearance();

            if (atom.GetVariable("icon").TryGetValueAsDreamResource(out DreamResource icon)) {
                appearance.Icon = icon.ResourcePath;
            }

            if (atom.GetVariable("icon_state").TryGetValueAsString(out string iconState)) {
                appearance.IconState = iconState;
            }

            if (atom.GetVariable("color").TryGetValueAsString(out string color)) {
                appearance.SetColor(color);
            }

            if (atom.GetVariable("dir").TryGetValueAsInteger(out int dir)) {
                appearance.Direction = (AtomDirection)dir;
            }

            if (atom.GetVariable("invisibility").TryGetValueAsInteger(out int invisibility)) {
                appearance.Invisibility = invisibility;
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
                appearance.Icon = icon.ResourcePath;
            }

            if (def.TryGetVariable("icon_state", out var stateVar) && stateVar.TryGetValueAsString(out string iconState)) {
                appearance.IconState = iconState;
            }

            if (def.TryGetVariable("color", out var colorVar) && colorVar.TryGetValueAsString(out string color)) {
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
}
