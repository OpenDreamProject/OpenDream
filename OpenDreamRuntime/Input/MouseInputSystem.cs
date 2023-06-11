using System.Collections.Specialized;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamShared.Input;
using Robust.Server.Player;

namespace OpenDreamRuntime.Input {
    sealed class MouseInputSystem : SharedMouseInputSystem {
        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;

        public override void Initialize() {
            base.Initialize();

            SubscribeNetworkEvent<EntityClickedEvent>(OnEntityClicked);
            SubscribeNetworkEvent<TurfClickedEvent>(OnTurfClicked);
        }

        private void OnEntityClicked(EntityClickedEvent e, EntitySessionEventArgs sessionEvent) {
            if (!_atomManager.TryGetMovableFromEntity(e.EntityUid, out var atom))
                return;

            HandleAtomClick(e, atom, sessionEvent);
        }

        private void OnTurfClicked(TurfClickedEvent e, EntitySessionEventArgs sessionEvent) {
            if (!_dreamMapManager.TryGetTurfAt(e.Position, e.Z, out var turf))
                return;

            HandleAtomClick(e, turf, sessionEvent);
        }

        private void HandleAtomClick(IAtomClickedEvent e, DreamObject atom, EntitySessionEventArgs sessionEvent) {
            IPlayerSession session = (IPlayerSession)sessionEvent.SenderSession;
            var connection = _dreamManager.GetConnectionBySession(session);
            var usr = connection.Mob;

            connection.Client?.SpawnProc("Click", usr: usr, ConstructClickArguments(atom, e));
        }

        private DreamValue[] ConstructClickArguments(DreamObject atom, IAtomClickedEvent e) {
            NameValueCollection paramsBuilder = HttpUtility.ParseQueryString(String.Empty);
            if (e.Shift) paramsBuilder.Add("shift", "1");
            if (e.Ctrl) paramsBuilder.Add("ctrl", "1");
            if (e.Alt) paramsBuilder.Add("alt", "1");
            paramsBuilder.Add("screen-loc", e.ScreenLoc.ToString());
            paramsBuilder.Add("icon-x", e.IconX.ToString());
            paramsBuilder.Add("icon-y", e.IconY.ToString());

            return new[] {
                new DreamValue(atom),
                DreamValue.Null,
                DreamValue.Null,
                new DreamValue(paramsBuilder.ToString())
            };
        }
    }
}
