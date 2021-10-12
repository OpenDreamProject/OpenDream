using System;
using System.Collections.Specialized;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Input;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace OpenDreamRuntime.Input {
    class MouseInputSystem : SharedMouseInputSystem {
        [Dependency] private IAtomManager _atomManager = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IDreamManager _dreamManager;

        public override void Initialize() {
            base.Initialize();

            SubscribeNetworkEvent<EntityClickedEvent>(OnEntityClicked);
        }

        private void OnEntityClicked(EntityClickedEvent e, EntitySessionEventArgs sessionEvent) {
            IEntity entity = _entityManager.GetEntity(e.EntityUid);
            DreamObject atom = _atomManager.GetAtomFromEntity(entity);
            if (atom == null)
                return;

            IPlayerSession session = (IPlayerSession)sessionEvent.SenderSession;
            var client = _dreamManager.GetConnectionBySession(session).ClientDreamObject;
            var usr = client.GetVariable("mob").GetValueAsDreamObject();

            client.SpawnProc("Click", ConstructClickArguments(atom), usr: usr);
        }

        private DreamProcArguments ConstructClickArguments(DreamObject atom) {
            NameValueCollection paramsBuilder = HttpUtility.ParseQueryString(String.Empty);
            //TODO: click params ("icon-x", "icon-y", "screen-loc", "shift", "ctrl", "alt")

            return new DreamProcArguments(new() {
                new DreamValue(atom),
                DreamValue.Null,
                DreamValue.Null,
                new DreamValue(paramsBuilder.ToString())
            });
        }
    }
}
