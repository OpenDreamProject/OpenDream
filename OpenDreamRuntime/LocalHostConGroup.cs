#if DEBUG
using Robust.Server.Console;
using Robust.Server.Player;
using System.Net;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
#endif

namespace OpenDreamRuntime {
    #if DEBUG
    /// <summary>
    ///     Debug ConGroup controller implementation that gives any client connected through localhost every permission.
    /// </summary>
    public sealed class LocalHostConGroup : IConGroupControllerImplementation, IPostInjectInit {
        public bool CanCommand(IPlayerSession session, string cmdName) {
            return IsLocal(session);
        }

        public bool CanViewVar(IPlayerSession session) {
            return IsLocal(session);
        }

        public bool CanAdminPlace(IPlayerSession session) {
            return IsLocal(session);
        }

        public bool CanScript(IPlayerSession session) {
            return IsLocal(session);
        }

        public bool CanAdminMenu(IPlayerSession session) {
            return IsLocal(session);
        }

        public bool CanAdminReloadPrototypes(IPlayerSession session) {
            return IsLocal(session);
        }
        
        private static bool IsLocal(IPlayerSession player) {
            return IsLocal(player.ConnectedClient);
        }

        private static bool IsLocal(INetChannel client) {
            var ep = client.RemoteEndPoint;
            var addr = ep.Address;
            if (addr.IsIPv4MappedToIPv6) {
                addr = addr.MapToIPv4();
            }

            return Equals(addr, IPAddress.Loopback) || Equals(addr, IPAddress.IPv6Loopback);
        }

        public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error) {
            error = null;
            if (user is null) return true; // Server console
            return IsLocal(user.ConnectedClient);
        }

        void IPostInjectInit.PostInject() {
            IoCManager.Resolve<IConGroupController>().Implementation = this;
        }
    }
    #endif
}
