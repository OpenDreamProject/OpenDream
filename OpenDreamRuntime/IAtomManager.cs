using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime {
    interface IAtomManager {
        public Dictionary<DreamList, DreamObject> OverlaysListToAtom { get; }
        public Dictionary<DreamList, DreamObject> UnderlaysListToAtom { get; }

        public EntityUid CreateAtomEntity(DreamObject atom);
        public EntityUid GetAtomEntity(DreamObject atom);
        public DreamObject GetAtomFromEntity(EntityUid entity);
        public void DeleteAtomEntity(DreamObject atom);
        public IconAppearance? GetAppearance(DreamObject atom);
        public void UpdateAppearance(DreamObject atom, Action<IconAppearance> update);
        public void AnimateAppearance(DreamObject atom, TimeSpan duration, Action<IconAppearance> animate);
        public IconAppearance CreateAppearanceFromAtom(DreamObject atom);
    }
}
