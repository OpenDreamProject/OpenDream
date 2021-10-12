using System.Collections.Generic;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace OpenDreamRuntime {
    class AtomManager : IAtomManager {
        //TODO: Maybe turn these into a special DreamList, similar to DreamListVars?
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; } = new();
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; } = new();

        [Dependency] private readonly IEntityManager _entityManager = default!;

        private Dictionary<DreamObject, IEntity> _atomToEntity = new();
        private Dictionary<IEntity, DreamObject> _entityToAtom = new();

        public IEntity CreateAtomEntity(DreamObject atom) {
            IEntity entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            DMISpriteComponent sprite = entity.AddComponent<DMISpriteComponent>();
            sprite.Appearance = CreateAppearanceFromAtom(atom);

            _atomToEntity.Add(atom, entity);
            _entityToAtom.Add(entity, atom);
            return entity;
        }

        public IEntity GetAtomEntity(DreamObject atom) {
            return _atomToEntity[atom];
        }

        public DreamObject GetAtomFromEntity(IEntity entity) {
            _entityToAtom.TryGetValue(entity, out DreamObject atom);

            return atom;
        }

        public void DeleteAtomEntity(DreamObject atom) {
            IEntity entity = GetAtomEntity(atom);

            _entityToAtom.Remove(entity);
            _atomToEntity.Remove(atom);
            entity.Delete();
        }

        public IconAppearance? GetAppearance(DreamObject atom) {
            return GetAtomEntity(atom).GetComponent<DMISpriteComponent>().Appearance;
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
    }
}
