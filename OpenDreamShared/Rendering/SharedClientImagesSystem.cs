using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Rendering {
    [Virtual]
    public class SharedClientImagesSystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class AddClientImageEvent : EntityEventArgs {
            public EntityUid AttachedObject;
            public uint ImageAppearance;

            public AddClientImageEvent(EntityUid attachedObject, uint imageAppearance) {
                AttachedObject = attachedObject;
                ImageAppearance = imageAppearance;
            }
        }

        [Serializable, NetSerializable]
        public sealed class RemoveClientImageEvent : EntityEventArgs {
            public EntityUid AttachedObject;
            public uint ImageAppearance;

            public RemoveClientImageEvent(EntityUid attachedObject, uint imageAppearance) {
                AttachedObject = attachedObject;
                ImageAppearance = imageAppearance;
            }
        }
    }
}
