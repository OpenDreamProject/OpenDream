using OpenDreamRuntime.Objects;
using Robust.Shared.GameObjects;

namespace OpenDreamRuntime {
    interface IAtomManager {
        public IEntity CreateAtomEntity(DreamObject atom);
        public IEntity GetAtomEntity(DreamObject atom);
        public DreamObject GetAtomFromEntity(IEntity entity);
        public void DeleteAtomEntity(DreamObject atom);
    }
}
