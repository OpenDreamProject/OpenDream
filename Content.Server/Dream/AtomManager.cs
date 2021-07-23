using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Server.Dream {
    class AtomManager : IAtomManager {
        [Dependency] IEntityManager _entityManager = null;

        private Dictionary<DreamObject, IEntity> _atomToEntity = new();

        public IEntity CreateAtomEntity(DreamObject atom) {
            IEntity entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            CreateSpriteComponent(entity, atom);
            _atomToEntity.Add(atom, entity);
            return entity;
        }

        public IEntity GetAtomEntity(DreamObject atom) {
            return _atomToEntity[atom];
        }

        public void DeleteAtomEntity(DreamObject atom) {
            GetAtomEntity(atom).Delete();
        }

        private void CreateSpriteComponent(IEntity entity, DreamObject atom) {
            DMISpriteComponent sprite = entity.AddComponent<DMISpriteComponent>();

            if (atom.GetVariable("icon").TryGetValueAsDreamResource(out DreamResource icon)) {
                sprite.Icon = new ResourcePath(icon.ResourcePath);
            }

            if (atom.GetVariable("icon_state").TryGetValueAsString(out string iconState)) {
                sprite.IconState = iconState;
            }

            if (atom.GetVariable("color").TryGetValueAsString(out string color)) {
                //TODO
            }

            if (atom.GetVariable("dir").TryGetValueAsInteger(out int dir)) {
                //TODO
            }

            if (atom.GetVariable("invisibility").TryGetValueAsInteger(out int invisibility)) {
                //TODO
            }

            if (atom.GetVariable("mouse_opacity").TryGetValueAsInteger(out int mouseOpacity)) {
               //TODO
            }

            atom.GetVariable("pixel_x").TryGetValueAsInteger(out int pixelX);
            atom.GetVariable("pixel_y").TryGetValueAsInteger(out int pixelY);
            //TODO: Pixel offset

            //TODO: Layers
        }
    }
}
