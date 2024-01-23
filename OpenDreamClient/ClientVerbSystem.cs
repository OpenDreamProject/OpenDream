using System.Threading.Tasks;
using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Client.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Timing;

namespace OpenDreamClient;

public sealed class ClientVerbSystem : VerbSystem {
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly ITimerManager _timerManager = default!;

    private EntityQuery<DMISpriteComponent> _spriteQuery;
    private EntityQuery<DreamMobSightComponent> _sightQuery;

    private readonly Dictionary<int, VerbInfo> _verbs = new();
    private List<int>? _clientVerbs;

    public override void Initialize() {
        _spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        _sightQuery = _entityManager.GetEntityQuery<DreamMobSightComponent>();

        _playerManager.LocalPlayerAttached += OnLocalPlayerAttached;

        SubscribeNetworkEvent<AllVerbsEvent>(OnAllVerbsEvent);
        SubscribeNetworkEvent<RegisterVerbEvent>(OnRegisterVerbEvent);
        SubscribeNetworkEvent<UpdateClientVerbsEvent>(OnUpdateClientVerbsEvent);
    }

    public override void Shutdown() {
        _verbs.Clear();
    }

    /// <summary>
    /// Prompt the user for the arguments to a verb, then ask the server to execute it
    /// </summary>
    /// <param name="src">The target of the verb</param>
    /// <param name="verbId">ID of the verb to execute</param>
    public async void ExecuteVerb(ClientObjectReference src, int verbId) {
        var verbInfo = _verbs[verbId];

        RaiseNetworkEvent(new ExecuteVerbEvent(src, verbId, await PromptVerbArguments(verbInfo)));
    }

    /// <summary>
    /// Ask the server to execute a verb with the given arguments
    /// </summary>
    /// <param name="src">The target of the verb</param>
    /// <param name="verbId">ID of the verb to execute</param>
    /// <param name="arguments">Arguments to the verb</param>
    /// <remarks>The server will not execute the verb if the arguments are invalid</remarks> // TODO: I think the server actually just errors, fix that
    public void ExecuteVerb(ClientObjectReference src, int verbId, object?[] arguments) {
        RaiseNetworkEvent(new ExecuteVerbEvent(src, verbId, arguments));
    }

    public IEnumerable<VerbInfo> GetAllVerbs() {
        return _verbs.Values;
    }

    /// <summary>
    /// Find all the verbs the client is currently capable of executing
    /// </summary>
    /// <param name="ignoreHiddenAttr">Whether to ignore "set hidden = TRUE"</param>
    /// <returns>The ID, target, and information of every executable verb</returns>
    public IEnumerable<(int Id, ClientObjectReference Src, VerbInfo VerbInfo)> GetExecutableVerbs(bool ignoreHiddenAttr = false) {
        DMISpriteComponent? playerSprite = null;
        sbyte? seeInvisibility = null;
        if (_playerManager.LocalEntity != null) {
            playerSprite = _spriteQuery.GetComponent(_playerManager.LocalEntity.Value);
            seeInvisibility = _sightQuery.GetComponent(_playerManager.LocalEntity.Value).SeeInvisibility;
        }

        // First, the verbs attached to our client
        if (_clientVerbs != null) {
            foreach (var verbId in _clientVerbs) {
                if (!_verbs.TryGetValue(verbId, out var verb))
                    continue;
                if (verb.IsHidden(ignoreHiddenAttr, seeInvisibility ?? 0))
                    continue; // TODO: How do invisible client verbs work when you don't have a mob?

                yield return (verbId, ClientObjectReference.Client, verb);
            }
        }

        // Then, the verbs attached to our mob
        if (playerSprite?.Icon.Appearance is { } playerAppearance) {
            var playerNetEntity = _entityManager.GetNetEntity(_playerManager.LocalEntity);

            if (playerNetEntity != null) {
                foreach (var verbId in playerAppearance.Verbs) {
                    if (!_verbs.TryGetValue(verbId, out var verb))
                        continue;
                    if (verb.IsHidden(ignoreHiddenAttr, seeInvisibility!.Value))
                        continue;

                    yield return (verbId, new(playerNetEntity.Value), verb);
                }
            }
        }
    }

    public bool TryGetVerbInfo(int verbId, out VerbInfo verbInfo) {
        return _verbs.TryGetValue(verbId, out verbInfo);
    }

    /// <summary>
    /// Look for a verb with the given command-name that the client can execute
    /// </summary>
    /// <param name="commandName">Command-name to look for</param>
    /// <returns>The ID, target, and verb information if a verb was found</returns>
    public (int Id, ClientObjectReference Src, VerbInfo VerbInfo)? FindVerbWithCommandName(string commandName) {
        foreach (var verb in GetExecutableVerbs(true)) {
            if (verb.VerbInfo.GetCommandName() == commandName)
                return verb;
        }

        return null;
    }

    /// <summary>
    /// Open prompt windows for the user to enter the arguments to a verb
    /// </summary>
    /// <param name="verbInfo">The verb to get arguments for</param>
    /// <returns>The values the user gives</returns>
    private async Task<object?[]> PromptVerbArguments(VerbInfo verbInfo) {
        var argumentCount = verbInfo.Arguments.Length;
        var arguments = (argumentCount > 0) ? new object?[argumentCount] : Array.Empty<object?>();

        for (int i = 0; i < argumentCount; i++) {
            var arg = verbInfo.Arguments[i];
            var tcs = new TaskCompletionSource<object?>();

            _taskManager.RunOnMainThread(() => {
                _interfaceManager.Prompt(arg.Types, verbInfo.Name, arg.Name, string.Empty, (_, value) => {
                    tcs.SetResult(value);
                });
            });

            arguments[i] = await tcs.Task; // Wait for this prompt to finish before moving on to the next
        }

        return arguments;
    }

    private void OnAllVerbsEvent(AllVerbsEvent e) {
        _verbs.EnsureCapacity(e.Verbs.Count);

        for (int i = 0; i < e.Verbs.Count; i++) {
            var verb = e.Verbs[i];

            _verbs.Add(i, verb);
        }

        _interfaceManager.DefaultInfo?.RefreshVerbs(this);
    }

    private void OnRegisterVerbEvent(RegisterVerbEvent e) {
        _verbs.Add(e.VerbId, e.VerbInfo);
    }

    private void OnUpdateClientVerbsEvent(UpdateClientVerbsEvent e) {
        _clientVerbs = e.VerbIds;
        _interfaceManager.DefaultInfo?.RefreshVerbs(this);
    }

    private void OnLocalPlayerAttached(EntityUid obj) {
        // Our mob changed, update our verb panels
        // A little hacky, but also wait half a second for verb information about our mob to arrive
        // TODO: Remove this timer
        _timerManager.AddTimer(new Timer(500, false, () => _interfaceManager.DefaultInfo?.RefreshVerbs(this)));
    }
}
