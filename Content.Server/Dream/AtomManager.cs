using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Collections.Generic;

namespace Content.Server.Dream {
    class AtomManager : IAtomManager {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private Dictionary<DreamObject, IEntity> _atomToEntity = new();
        private Dictionary<IEntity, DreamObject> _entityToAtom = new();

        public IEntity CreateAtomEntity(DreamObject atom) {
            IEntity entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

            entity.AddComponent<DMISpriteComponent>().SetAppearanceFromAtom(atom);
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
    }
}
