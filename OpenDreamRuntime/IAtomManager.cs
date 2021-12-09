using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace OpenDreamRuntime {
    interface IAtomManager {
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; }
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; }

        public EntityUid CreateAtomEntity(DreamObject atom);
        public EntityUid GetAtomEntity(DreamObject atom);
        public DreamObject GetAtomFromEntity(EntityUid entity);
        public void DeleteAtomEntity(DreamObject atom);
        public IconAppearance? GetAppearance(DreamObject atom);
        public IconAppearance CreateAppearanceFromAtom(DreamObject atom);
    }
}
