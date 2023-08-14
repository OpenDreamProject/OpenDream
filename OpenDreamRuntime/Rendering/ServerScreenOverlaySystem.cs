﻿using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Rendering;
using Robust.Server.GameStates;
using Robust.Server.Player;

namespace OpenDreamRuntime.Rendering {
    public sealed class ServerScreenOverlaySystem : SharedScreenOverlaySystem {
        private readonly Dictionary<IPlayerSession, HashSet<EntityUid>> _sessionToScreenObjects = new();

        public override void Initialize() {
            SubscribeLocalEvent<ExpandPvsEvent>(HandleExpandPvsEvent);
        }

        public void AddScreenObject(DreamConnection connection, DreamObjectMovable screenObject) {
            if (!_sessionToScreenObjects.TryGetValue(connection.Session, out var objects)) {
                objects = new HashSet<EntityUid>();
                _sessionToScreenObjects.Add(connection.Session, objects);
            }

            objects.Add(screenObject.Entity);
            RaiseNetworkEvent(new AddScreenObjectEvent(screenObject.Entity), connection.Session.ConnectedClient);
        }

        public void RemoveScreenObject(DreamConnection connection, DreamObjectMovable screenObject) {
            _sessionToScreenObjects[connection.Session].Remove(screenObject.Entity);
            RaiseNetworkEvent(new RemoveScreenObjectEvent(screenObject.Entity), connection.Session.ConnectedClient);
        }

        private void HandleExpandPvsEvent(ref ExpandPvsEvent e) {
            if (_sessionToScreenObjects.TryGetValue(e.Session, out var objects)) {
                e.Entities.AddRange(objects);
            }
        }
    }
}
