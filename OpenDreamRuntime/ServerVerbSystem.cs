using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace OpenDreamRuntime;

public sealed class ServerVerbSystem : VerbSystem {
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly List<VerbInfo> _verbs = new();
    private readonly Dictionary<int, DreamProc> _verbIdToProc = new();

    public override void Initialize() {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<ExecuteVerbEvent>(OnVerbExecuted);
    }

    /// <summary>
    /// Add a verb to the total list of verbs and ensure every client has knowledge of it
    /// </summary>
    /// <param name="verb">The verb to register</param>
    public void RegisterVerb(DreamProc verb) {
        if (verb.VerbId != null) // Verb has already been registered
            return;

        var verbInfo = new VerbInfo {
            Name = verb.VerbName,

            // TODO: default_verb_category
            // Explicitly null category is hidden from verb panels, "" category becomes the default_verb_category
            // But if default_verb_category is null, we hide it from the verb panel
            Category = verb.VerbCategory ?? string.Empty,

            Invisibility = verb.Invisibility,
            HiddenAttribute = (verb.Attributes & ProcAttributes.Hidden) == ProcAttributes.Hidden
        };

        if (verb.ArgumentTypes != null) {
            verbInfo.Arguments = new VerbArg[verb.ArgumentTypes.Count];

            for (int i = 0; i < verb.ArgumentTypes.Count; i++) {
                verbInfo.Arguments[i] = new VerbArg {
                    Name = verb.ArgumentNames![i],
                    Types = verb.ArgumentTypes[i]
                };
            }
        }

        verb.VerbId = _verbs.Count;
        _verbs.Add(verbInfo);
        _verbIdToProc.Add(verb.VerbId.Value, verb);

        RaiseNetworkEvent(new RegisterVerbEvent(verb.VerbId.Value, verbInfo));
    }

    public DreamProc GetVerb(int verbId) => _verbIdToProc[verbId];

    public bool TryGetVerb(int verbId, [NotNullWhen(true)] out DreamProc? verb) {
        return _verbIdToProc.TryGetValue(verbId, out verb);
    }

    /// <summary>
    /// Send a client an updated version of its /client's verbs
    /// </summary>
    /// <param name="client">The client to update</param>
    public void UpdateClientVerbs(DreamObjectClient client) {
        var verbs = client.ClientVerbs.Verbs;
        var verbIds = new List<int>(verbs.Count);

        foreach (var verb in verbs) {
            if (verb.VerbId == null)
                RegisterVerb(verb);

            verbIds.Add(verb.VerbId!.Value);
        }

        RaiseNetworkEvent(new UpdateClientVerbsEvent(verbIds), client.Connection.Session!);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
        if (e.NewStatus != SessionStatus.InGame)
            return;

        // Send the new player a list of every verb
        RaiseNetworkEvent(new AllVerbsEvent(_verbs), e.Session);
    }

    private void OnVerbExecuted(ExecuteVerbEvent msg, EntitySessionEventArgs args) {
        var connection = _dreamManager.GetConnectionBySession(args.SenderSession);
        var src = _dreamManager.GetFromClientReference(connection, msg.Src);
        if (src == null || !TryGetVerb(msg.VerbId, out var verb) || !CanExecute(connection, src, verb))
            return;
        if (msg.Arguments.Length != verb.ArgumentTypes?.Count)
            return;

        // Convert the values the client gave to DreamValues
        DreamValue[] arguments = new DreamValue[verb.ArgumentTypes.Count];
        for (int i = 0; i < verb.ArgumentTypes.Count; i++) {
            var argType = verb.ArgumentTypes[i];

            arguments[i] = argType switch {
                DMValueType.Null => DreamValue.Null,
                DMValueType.Text or DMValueType.Message => new DreamValue((string)msg.Arguments[i]),
                DMValueType.Num => new DreamValue((float)msg.Arguments[i]),
                DMValueType.Color => new DreamValue(((Color)msg.Arguments[i]).ToHexNoAlpha()),
                _ => throw new Exception("Invalid prompt response '" + msg.Arguments[i] + "'")
            };
        }

        DreamThread.Run($"Execute {msg.VerbId} by {connection.Session!.Name}", async state => {
            await state.Call(verb, src, connection.Mob, arguments);
            return DreamValue.Null;
        });
    }

    /// <summary>
    /// Verifies a user is allowed to execute a verb on a given target
    /// </summary>
    /// <param name="connection">The user</param>
    /// <param name="src">The target of the verb</param>
    /// <param name="verb">The verb trying to be executed</param>
    /// <returns>True if the user is allowed to execute the verb in this way</returns>
    private bool CanExecute(DreamConnection connection, DreamObject src, DreamProc verb) {
        if (verb.VerbId == null) // Not even a verb
            return false;

        if (src is DreamObjectClient client && !client.ClientVerbs.Verbs.Contains(verb)) { // Inside client.verbs?
            return false;
        } else if (src is DreamObjectAtom atom) {
            var appearance = _atomManager.MustGetAppearance(atom);

            if (appearance?.Verbs.Contains(verb.VerbId.Value) is not true) // Inside atom.verbs?
                return false;
        }

        // TODO: Does "set src = ..." allow execution here?
        return true;
    }
}
