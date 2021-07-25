using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Collections.Generic;

namespace Content.Server.Dream {
    class AtomManager : IAtomManager {
        [Dependency] IEntityManager _entityManager = null;

        private Dictionary<DreamObject, IEntity> _atomToEntity = new();

        public IEntity CreateAtomEntity(DreamObject atom) {
            IEntity entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            entity.AddComponent<DMISpriteComponent>().SetAppearanceFromAtom(atom);
            _atomToEntity.Add(atom, entity);
            return entity;
        }

        public IEntity GetAtomEntity(DreamObject atom) {
            return _atomToEntity[atom];
        }

        public void DeleteAtomEntity(DreamObject atom) {
            GetAtomEntity(atom).Delete();
        }
    }
}
