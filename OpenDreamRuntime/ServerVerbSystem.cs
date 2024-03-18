using System.Diagnostics;
using DMCompiler.DM;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace OpenDreamRuntime;

public sealed class ServerVerbSystem : VerbSystem {
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly List<VerbInfo> _verbs = new();
    private readonly Dictionary<int, DreamProc> _verbIdToProc = new();

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.verbs");

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

        var verbArguments = Array.Empty<VerbArg>();
        if (verb.ArgumentTypes != null) {
            verbArguments = new VerbArg[verb.ArgumentTypes.Count];

            for (int i = 0; i < verb.ArgumentTypes.Count; i++) {
                verbArguments[i] = new VerbArg {
                    Name = verb.ArgumentNames![i],
                    Types = verb.ArgumentTypes[i]
                };
            }
        }

        VerbAccessibility? verbAccessibility = verb.VerbSrc switch {
            VerbSrc.View => VerbAccessibility.View, // TODO: Ranges on the view()/range() types
            VerbSrc.InView => VerbAccessibility.InView,
            VerbSrc.OView => VerbAccessibility.OView,
            VerbSrc.InOView => VerbAccessibility.InOView,
            VerbSrc.Range => VerbAccessibility.Range,
            VerbSrc.InRange => VerbAccessibility.InRange,
            VerbSrc.ORange => VerbAccessibility.ORange,
            VerbSrc.InORange => VerbAccessibility.InORange,
            VerbSrc.World => VerbAccessibility.InWorld,
            VerbSrc.InWorld => VerbAccessibility.InWorld,
            VerbSrc.Usr => VerbAccessibility.Usr,
            VerbSrc.InUsr => VerbAccessibility.InUsr,
            VerbSrc.UsrLoc => VerbAccessibility.UsrLoc,
            VerbSrc.UsrGroup => VerbAccessibility.UsrGroup,
            null => null,
            _ => throw new UnreachableException("All cases should be covered")
        };

        if (verbAccessibility == null) {
            var def = verb.OwningType.ObjectDefinition;

            // Assign a default based on the type this verb is defined on
            if (def.IsSubtypeOf(_objectTree.Obj)) {
                verbAccessibility = VerbAccessibility.InUsr;
            } else if (def.IsSubtypeOf(_objectTree.Turf) || def.IsSubtypeOf(_objectTree.Area)) {
                verbAccessibility = VerbAccessibility.View; // TODO: Range of 0
            } else {
                // The default for everything else (/mob especially)
                verbAccessibility = VerbAccessibility.Usr;
            }
        }

        var verbInfo = new VerbInfo {
            Name = verb.VerbName,

            // TODO: default_verb_category
            // Explicitly null category is hidden from verb panels, "" category becomes the default_verb_category
            // But if default_verb_category is null, we hide it from the verb panel
            Category = verb.VerbCategory ?? string.Empty,

            Invisibility = verb.Invisibility,
            HiddenAttribute = (verb.Attributes & ProcAttributes.Hidden) == ProcAttributes.Hidden,
            Accessibility = verbAccessibility.Value,
            Arguments = verbArguments
        };

        verb.VerbId = _verbs.Count;
        _verbs.Add(verbInfo);
        _verbIdToProc.Add(verb.VerbId.Value, verb);

        RaiseNetworkEvent(new RegisterVerbEvent(verb.VerbId.Value, verbInfo));
    }

    public DreamProc GetVerb(int verbId) => _verbIdToProc[verbId];

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
        if (src == null || !_verbIdToProc.TryGetValue(msg.VerbId, out var verb) || !CanExecute(connection, src, verb))
            return;

        var argCount = verb.ArgumentTypes?.Count ?? 0;
        if (msg.Arguments.Length != argCount) {
            _sawmill.Error(
                $"User \"{args.SenderSession.Name}\" gave {msg.Arguments.Length} argument(s) to the \"{verb.Name}\" verb which only has {argCount} argument(s)");
            return;
        }

        // Convert the values the client gave to DreamValues
        DreamValue[] arguments = new DreamValue[argCount];
        for (int i = 0; i < argCount; i++) {
            var argType = verb.ArgumentTypes![i];

            if (!connection.TryConvertPromptResponse(argType, msg.Arguments[i], out arguments[i])) {
                _sawmill.Error(
                    $"User \"{args.SenderSession.Name}\" gave an invalid value for argument #{i + 1} of verb \"{verb.Name}\"");
                return;
            }
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

        if (src is DreamObjectClient client) {
            if (!client.ClientVerbs.Verbs.Contains(verb))
                return false; // Not inside client.verbs

            // Client verbs ignore "set src" checks
            // Deviates from BYOND, where anything but usr and world shows the verb in the statpanel but is not executable
            return true;
        } else if (src is DreamObjectAtom atom) {
            var appearance = _atomManager.MustGetAppearance(atom);

            if (appearance?.Verbs.Contains(verb.VerbId.Value) is not true) // Inside atom.verbs?
                return false;
        }

        var verbInfo = _verbs[verb.VerbId.Value];

        // Check that "set src = ..." allows execution in this instance
        switch (verbInfo.Accessibility) {
            case VerbAccessibility.Usr:
                return src == connection.Mob;
            case VerbAccessibility.InUsr:
                if (src is not DreamObjectMovable srcMovable)
                    return false;

                return srcMovable.Loc == connection.Mob;
            default:
                // TODO: All the other kinds
                return true;
        }
    }
}
