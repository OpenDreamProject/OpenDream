using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace OpenDreamRuntime {
    interface IAtomManager {
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; }
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; }

        public IEntity CreateAtomEntity(DreamObject atom);
        public IEntity GetAtomEntity(DreamObject atom);
        public DreamObject GetAtomFromEntity(IEntity entity);
        public void DeleteAtomEntity(DreamObject atom);
        public IconAppearance? GetAppearance(DreamObject atom);
        public IconAppearance CreateAppearanceFromAtom(DreamObject atom);
    }
}
