using System.Collections.Specialized;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Input;
using Robust.Server.Player;

namespace OpenDreamRuntime.Input {
    sealed class MouseInputSystem : SharedMouseInputSystem {
        [Dependency] private IAtomManager _atomManager = default!;
        [Dependency] private IDreamManager _dreamManager = default!;

        public override void Initialize() {
            base.Initialize();

            SubscribeNetworkEvent<EntityClickedEvent>(OnEntityClicked);
        }

        private void OnEntityClicked(EntityClickedEvent e, EntitySessionEventArgs sessionEvent) {
            DreamObject atom = _atomManager.GetAtomFromEntity(e.EntityUid);
            if (atom == null)
                return;

            IPlayerSession session = (IPlayerSession)sessionEvent.SenderSession;
            var client = _dreamManager.GetConnectionBySession(session).ClientDreamObject;
            var usr = client.GetVariable("mob").GetValueAsDreamObject();

            client.SpawnProc("Click", ConstructClickArguments(atom, e), usr: usr);
        }

        private DreamProcArguments ConstructClickArguments(DreamObject atom, EntityClickedEvent e) {
            NameValueCollection paramsBuilder = HttpUtility.ParseQueryString(String.Empty);
            if (e.Shift) paramsBuilder.Add("shift", "1");
            if (e.Ctrl) paramsBuilder.Add("ctrl", "1");
            if (e.Alt) paramsBuilder.Add("alt", "1");
            //TODO: "icon-x", "icon-y", "screen-loc"

            return new DreamProcArguments(new() {
                new DreamValue(atom),
                DreamValue.Null,
                DreamValue.Null,
                new DreamValue(paramsBuilder.ToString())
            });
        }
    }
}
