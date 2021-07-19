using Robust.Shared.GameObjects;

namespace Content.Server.Dream {
    interface IAtomManager {
        public IEntity CreateAtomEntity(DreamObject atom);
        public IEntity GetAtomEntity(DreamObject atom);
        public void DeleteAtomEntity(DreamObject atom);
    }
}
