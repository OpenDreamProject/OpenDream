﻿using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameStates;
using OpenDreamShared.Dream;

namespace OpenDreamShared.Rendering {
    [NetworkedComponent]
    public abstract partial class SharedDMISpriteComponent : Component {
        [Serializable, NetSerializable]
        public sealed class DMISpriteComponentState : ComponentState {
            public readonly int? AppearanceId;
            public readonly ScreenLocation ScreenLocation;

            public DMISpriteComponentState(int? appearanceId, ScreenLocation screenLocation) {
                AppearanceId = appearanceId;
                ScreenLocation = screenLocation;
            }
        }
    }
}
