#if DEBUG
using Robust.Shared.Player;
using Robust.Server.Console;
using System.Net;
using Robust.Shared.Network;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace OpenDreamRuntime;

/// <summary>
///     Debug ConGroup controller implementation that gives any client connected through localhost every permission.
/// </summary>
public sealed class LocalHostConGroup : IConGroupControllerImplementation, IPostInjectInit {
    public bool CanCommand(ICommonSession session, string cmdName) {
        return IsLocal(session);
    }

    public bool CanViewVar(ICommonSession session) {
        return IsLocal(session);
    }

    public bool CanAdminPlace(ICommonSession session) {
        return IsLocal(session);
    }

    public bool CanScript(ICommonSession session) {
        return IsLocal(session);
    }

    public bool CanAdminMenu(ICommonSession session) {
        return IsLocal(session);
    }

    public bool CanAdminReloadPrototypes(ICommonSession session) {
        return IsLocal(session);
    }

    private static bool IsLocal(ICommonSession player) {
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
