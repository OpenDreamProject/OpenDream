using OpenDreamShared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace OpenDreamRuntime.Input;

internal sealed class DreamCommandSystem : EntitySystem{
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly HashSet<(string Command, ICommonSession session)> _repeatingCommands = new();

    public override void Initialize() {
        _netManager.RegisterNetMessage<MsgCommandRepeatStart>(OnRepeatCommandEvent);
        _netManager.RegisterNetMessage<MsgCommandRepeatStop>(OnStopRepeatCommandEvent);
    }

    public void RunRepeatingCommands() {
        foreach (var (command, session) in _repeatingCommands) {
            RunCommand(command, session);
        }
    }

    private void OnRepeatCommandEvent(MsgCommandRepeatStart message) {
        var tuple = (message.Command, _playerManager.GetSessionByChannel(message.MsgChannel));

        _repeatingCommands.Add(tuple);
    }

    private void OnStopRepeatCommandEvent(MsgCommandRepeatStop message) {
        var tuple = (message.Command, _playerManager.GetSessionByChannel(message.MsgChannel));

        _repeatingCommands.Remove(tuple);
    }

    private void RunCommand(string command, ICommonSession session) {
        var connection = _dreamManager.GetConnectionBySession(session);
        connection.HandleCommand(command);
    }
}
