using OpenDreamClient.Input;
using OpenDreamClient.Interface;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Robust.Shared.Log;

namespace OpenDreamClient {
    class DreamClientSystem : EntitySystem {
        [Dependency] private readonly IDreamMacroManager _macroManager = default!;
        [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private List<EntityUid> _lookupTreeUpdateQueue = new();

        public override void Initialize() {
            SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);
        }

        public override void Update(float frameTime) {
            //TODO: Remove this when RT has an event for bounds changing (bug sloth)
            if (_lookupTreeUpdateQueue.Count > 0) {
                foreach (EntityUid entity in _lookupTreeUpdateQueue) {
                    if (_entityManager.TryGetComponent<TransformComponent>(entity, out var comp))
                    {
                        _entityLookup.UpdateEntityTree(entity, comp);
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, $"Failed to update AABB for {entity}");
                    }
                }

                _lookupTreeUpdateQueue.Clear();
            }
        }

        public void QueueLookupTreeUpdate(EntityUid entity) {
            _lookupTreeUpdateQueue.Add(entity);
        }

        private void OnPlayerAttached(PlayerAttachSysMessage e) {
            // The active input context gets reset to "common" when a new player is attached
            // So we have to set it again
            _macroManager.SetActiveMacroSet(_interfaceManager.InterfaceDescriptor.MacroSetDescriptors[0]);
        }
    }
}
