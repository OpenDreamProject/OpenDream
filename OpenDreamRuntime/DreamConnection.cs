using System.Threading.Tasks;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace OpenDreamRuntime;

public sealed class DreamConnection {
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;
    [Dependency] private readonly WalkManager _walkManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private readonly ServerScreenOverlaySystem? _screenOverlaySystem;
    private readonly ServerClientImagesSystem? _clientImagesSystem;

    [ViewVariables] private readonly Dictionary<string, (DreamObject Src, DreamProc Verb)> _availableVerbs = new();
    [ViewVariables] private readonly Dictionary<string, List<(string, string, string?)>> _statPanels = new();
    [ViewVariables] private bool _currentlyUpdatingStat;

    [ViewVariables] public ICommonSession? Session { get; private set; }
    [ViewVariables] public DreamObjectClient? Client { get; private set; }
    [ViewVariables]
    public DreamObjectMob? Mob {
        get => _mob;
        set {
            if (_mob != value) {
                var oldMob = _mob;
                _mob = value;

                if (oldMob != null) {
                    oldMob.Key = null;
                    oldMob.SpawnProc("Logout");
                    oldMob.Connection = null;
                }

                StatObj = new(value);
                if (Eye != null && Eye == oldMob) {
                    Eye = value;
                }

                if (_mob != null) {
                    // If the mob is already owned by another player, kick them out
                    if (_mob.Connection != null)
                        _mob.Connection.Mob = null;

                    _mob.Connection = this;
                    _mob.Key = Session!.Name;
                    _mob.SpawnProc("Login", usr: _mob);
                }

                UpdateAvailableVerbs();
            }
        }
    }

    [ViewVariables]
    public DreamObjectMovable? Eye {
        get => _eye;
        set {
            _eye = value;
            _playerManager.SetAttachedEntity(Session!, _eye?.Entity);
        }
    }

    [ViewVariables]
    public DreamValue StatObj { get; set; } // This can be just any DreamValue. Only atoms will function though.

    [ViewVariables] private string? _outputStatPanel;
    [ViewVariables] private string _selectedStatPanel;
    [ViewVariables] private readonly Dictionary<int, Action<DreamValue>> _promptEvents = new();
    [ViewVariables] private int _nextPromptEvent = 1;

    private DreamObjectMob? _mob;
    private DreamObjectMovable? _eye;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.connection");

    public string SelectedStatPanel {
        get => _selectedStatPanel;
        set {
            _selectedStatPanel = value;

            var msg = new MsgSelectStatPanel() { StatPanel = value };
            Session?.ConnectedClient.SendMessage(msg);
        }
    }

    public DreamConnection() {
        IoCManager.InjectDependencies(this);

        _entitySystemManager.TryGetEntitySystem(out _screenOverlaySystem);
        _entitySystemManager.TryGetEntitySystem(out _clientImagesSystem);
    }

    public void HandleConnection(ICommonSession session) {
        var client = new DreamObjectClient(_objectTree.Client.ObjectDefinition, this, _screenOverlaySystem, _clientImagesSystem);

        Session = session;

        Client = client;
        Client.InitSpawn(new());

        SendClientInfoUpdate();
    }

    public void HandleDisconnection() {
        if (Session == null || Client == null) // Already disconnected?
            return;

        if (_mob != null) {
            // Don't null out the ckey here
            _mob.SpawnProc("Logout");

            if (_mob != null) { // Logout() may have removed our mob
                _mob.Connection = null;
                _mob = null;
            }
        }

        Client.Delete();
        Client = null;

        Session = null;
    }

    public void UpdateAvailableVerbs() {
        _availableVerbs.Clear();
        var verbs = new List<(string, string, string)>();

        void AddVerbs(DreamObject src, IEnumerable<DreamValue> adding) {
            foreach (DreamValue mobVerb in adding) {
                if (!mobVerb.TryGetValueAsProc(out var proc))
                    continue;

                string verbName = proc.VerbName ?? proc.Name;
                string verbId = verbName.ToLowerInvariant().Replace(" ", "-"); // Case-insensitive, dashes instead of spaces
                if (_availableVerbs.ContainsKey(verbId)) {
                    // BYOND will actually show the user two verbs with different capitalization/dashes, but they will both execute the same verb.
                    // We make a warning and ignore the latter ones instead.
                    _sawmill.Warning($"User \"{Session.Name}\" has multiple verb commands named \"{verbId}\", ignoring all but the first");
                    continue;
                }

                _availableVerbs.Add(verbId, (src, proc));

                // Don't send invisible verbs.
                if (_mob != null && proc.Invisibility > _mob.SeeInvisible) {
                    continue;
                }

                // Don't send hidden verbs. Names starting with "." count as hidden.
                if ((proc.Attributes & ProcAttributes.Hidden) == ProcAttributes.Hidden ||
                    verbName.StartsWith('.')) {
                    continue;
                }

                string? category = proc.VerbCategory;
                // Explicitly null category is hidden from verb panels, "" category becomes the default_verb_category
                if (category == string.Empty) {
                    // But if default_verb_category is null, we hide it from the verb panel
                    Client.GetVariable("default_verb_category").TryGetValueAsString(out category);
                }

                // Null category is serialized as an empty string and treated as hidden
                verbs.Add((verbName, verbId, category ?? string.Empty));
            }
        }

        if (Client != null) {
            AddVerbs(Client, Client.Verbs.GetValues());
        }

        if (Mob != null) {
            AddVerbs(Mob, Mob.Verbs.GetValues());
        }

        var msg = new MsgUpdateAvailableVerbs() {
            AvailableVerbs = verbs.ToArray()
        };

        Session?.ConnectedClient.SendMessage(msg);
    }

    public void UpdateStat() {
        if (Session == null || Client == null || _currentlyUpdatingStat)
            return;

        _currentlyUpdatingStat = true;
        _statPanels.Clear();

        DreamThread.Run("Stat", async (state) => {
            try {
                var statProc = Client.GetProc("Stat");

                await state.Call(statProc, Client, Mob);
                if (Session.Status == SessionStatus.InGame) {
                    var msg = new MsgUpdateStatPanels(_statPanels);
                    Session.ConnectedClient.SendMessage(msg);
                }

                return DreamValue.Null;
            } finally {
                _currentlyUpdatingStat = false;
            }
        });
    }

    public void SendClientInfoUpdate() {
        MsgUpdateClientInfo msg = new() {
            View = Client!.View
        };

        Session?.ConnectedClient.SendMessage(msg);
    }

    public void SetOutputStatPanel(string name) {
        if (!_statPanels.ContainsKey(name))
            _statPanels.Add(name, new());

        _outputStatPanel = name;
    }

    public void AddStatPanelLine(string name, string value, string? atomRef) {
        if (_outputStatPanel == null || !_statPanels.ContainsKey(_outputStatPanel))
            SetOutputStatPanel("Stats");

        _statPanels[_outputStatPanel].Add((name, value, atomRef));
    }

    public void HandleMsgSelectStatPanel(MsgSelectStatPanel message) {
        _selectedStatPanel = message.StatPanel;
    }

    public void HandleMsgPromptResponse(MsgPromptResponse message) {
        if (!_promptEvents.TryGetValue(message.PromptId, out var promptEvent)) {
            _sawmill.Warning($"{message.MsgChannel}: Received MsgPromptResponse for prompt {message.PromptId} which does not exist.");
            return;
        }

        DreamValue value = message.Type switch {
            DMValueType.Null => DreamValue.Null,
            DMValueType.Text or DMValueType.Message => new DreamValue((string)message.Value),
            DMValueType.Num => new DreamValue((float)message.Value),
            DMValueType.Color => new DreamValue(((Color)message.Value).ToHexNoAlpha()),
            _ => throw new Exception("Invalid prompt response '" + message.Type + "'")
        };

        promptEvent.Invoke(value);
        _promptEvents.Remove(message.PromptId);
    }

    public void HandleMsgTopic(MsgTopic pTopic) {
        DreamList hrefList = DreamProcNativeRoot.params2list(_objectTree, HttpUtility.UrlDecode(pTopic.Query));
        DreamValue srcRefValue = hrefList.GetValue(new DreamValue("src"));
        DreamValue src = DreamValue.Null;

        if (srcRefValue.TryGetValueAsString(out var srcRef)) {
            src = _dreamManager.LocateRef(srcRef);
        }

        Client?.SpawnProc("Topic", usr: Mob, new(pTopic.Query), new(hrefList), src);
    }

    public void OutputDreamValue(DreamValue value) {
        if (value.TryGetValueAsDreamObject<DreamObjectSound>(out var outputObject)) {
            ushort channel = (ushort)outputObject.GetVariable("channel").GetValueAsInteger();
            ushort volume = (ushort)outputObject.GetVariable("volume").GetValueAsInteger();
            DreamValue file = outputObject.GetVariable("file");

            var msg = new MsgSound() {
                Channel = channel,
                Volume = volume
            };

            if (!file.TryGetValueAsDreamResource(out var soundResource)) {
                if (file.TryGetValueAsString(out var soundPath)) {
                    soundResource = _resourceManager.LoadResource(soundPath);
                } else if (!file.IsNull) {
                    throw new ArgumentException($"Cannot output {value}", nameof(value));
                }
            }

            msg.ResourceId = soundResource?.Id;
            if (soundResource?.ResourcePath is { } resourcePath) {
                if (resourcePath.EndsWith(".ogg"))
                    msg.Format = MsgSound.FormatType.Ogg;
                else if (resourcePath.EndsWith(".wav"))
                    msg.Format = MsgSound.FormatType.Wav;
                else
                    throw new Exception($"Sound {value} is not a supported file type");
            }

            Session?.ConnectedClient.SendMessage(msg);
            return;
        }

        OutputControl(value.Stringify(), null);
    }

    public void OutputControl(string message, string? control) {
        var msg = new MsgOutput() {
            Value = message,
            Control = control
        };

        Session?.ConnectedClient.SendMessage(msg);
    }

    public void HandleCommand(string fullCommand) {
        // TODO: Arguments are a little more complicated than "split by spaces"
        // e.g. strings can be passed
        string[] args = fullCommand.Split(' ', StringSplitOptions.TrimEntries);
        string command = args[0].ToLowerInvariant(); // Case-insensitive

        switch (command) {
            case ".north":
            case ".east":
            case ".south":
            case ".west":
            case ".northeast":
            case ".southeast":
            case ".southwest":
            case ".northwest":
            case ".center":
                string movementProc = command switch {
                    ".north" => "North",
                    ".east" => "East",
                    ".south" => "South",
                    ".west" => "West",
                    ".northeast" => "Northeast",
                    ".southeast" => "Southeast",
                    ".southwest" => "Southwest",
                    ".northwest" => "Northwest",
                    ".center" => "Center",
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (Mob != null)
                    _walkManager.StopWalks(Mob);
                Client?.SpawnProc(movementProc, Mob); break;

            default: {
                if (_availableVerbs.TryGetValue(command, out var value)) {
                    (DreamObject verbSrc, DreamProc verb) = value;

                    DreamThread.Run(fullCommand, async (state) => {
                        DreamValue[] arguments;
                        if (verb.ArgumentNames != null) {
                            arguments = new DreamValue[verb.ArgumentNames.Count];

                            // TODO: this should probably be done on the client, shouldn't it?
                            if (args.Length == 1) { // No args given; prompt the client for them
                                for (int i = 0; i < verb.ArgumentNames.Count; i++) {
                                    String argumentName = verb.ArgumentNames[i];
                                    DMValueType argumentType = verb.ArgumentTypes[i];
                                    DreamValue argumentValue = await Prompt(argumentType, title: String.Empty, // No settable title for verbs
                                        argumentName, defaultValue: String.Empty); // No default value for verbs

                                    arguments[i] = argumentValue;
                                }
                            } else { // Attempt to parse the given arguments
                                for (int i = 0; i < verb.ArgumentNames.Count; i++) {
                                    DMValueType argumentType = verb.ArgumentTypes[i];

                                    if (argumentType == DMValueType.Text) {
                                        arguments[i] = new(args[i + 1]);
                                    } else {
                                        _sawmill.Error($"Parsing verb args of type {argumentType} is unimplemented; ignoring command ({fullCommand})");
                                        return DreamValue.Null;
                                    }
                                }
                            }
                        } else {
                            arguments = Array.Empty<DreamValue>();
                        }

                        await state.Call(verb, verbSrc, Mob, arguments);
                        return DreamValue.Null;
                    });
                }

                break;
            }
        }
    }

    public Task<DreamValue> Prompt(DMValueType types, String title, String message, String defaultValue) {
        var task = MakePromptTask(out var promptId);
        var msg = new MsgPrompt() {
            PromptId = promptId,
            Title = title,
            Message = message,
            Types = types,
            DefaultValue = defaultValue
        };

        Session.ConnectedClient.SendMessage(msg);
        return task;
    }

    public async Task<DreamValue> PromptList(DMValueType types, DreamList list, string title, string message, DreamValue defaultValue) {
        List<DreamValue> listValues = list.GetValues();

        List<string> promptValues = new(listValues.Count);
        for (int i = 0; i < listValues.Count; i++) {
            DreamValue value = listValues[i];

            if (types.HasFlag(DMValueType.Obj) && !value.TryGetValueAsDreamObject<DreamObjectMovable>(out _))
                continue;
            if (types.HasFlag(DMValueType.Mob) && !value.TryGetValueAsDreamObject<DreamObjectMob>(out _))
                continue;
            if (types.HasFlag(DMValueType.Turf) && !value.TryGetValueAsDreamObject<DreamObjectTurf>(out _))
                continue;
            if (types.HasFlag(DMValueType.Area) && !value.TryGetValueAsDreamObject<DreamObjectArea>(out _))
                continue;

            promptValues.Add(value.Stringify());
        }

        if (promptValues.Count == 0)
            return DreamValue.Null;

        var task = MakePromptTask(out var promptId);
        var msg = new MsgPromptList() {
            PromptId = promptId,
            Title = title,
            Message = message,
            CanCancel = (types & DMValueType.Null) == DMValueType.Null,
            DefaultValue = defaultValue.Stringify(),
            Values = promptValues.ToArray()
        };

        Session.ConnectedClient.SendMessage(msg);

        // The client returns the index of the selected item, this needs turned back into the DreamValue.
        var selectedIndex = await task;
        if (selectedIndex.TryGetValueAsInteger(out int index) && index < listValues.Count) {
            return listValues[index];
        }

        // Client returned an invalid value.
        // Return the first value in the list, or null if cancellable
        return msg.CanCancel ? DreamValue.Null : listValues[0];
    }

    public Task<DreamValue> WinExists(string controlId) {
        var task = MakePromptTask(out var promptId);
        var msg = new MsgWinExists() {
            PromptId = promptId,
            ControlId = controlId
        };

        Session.ConnectedClient.SendMessage(msg);

        return task;
    }

    public Task<DreamValue> WinGet(string controlId, string queryValue) {
        var task = MakePromptTask(out var promptId);
        var msg = new MsgWinGet() {
            PromptId = promptId,
            ControlId = controlId,
            QueryValue = queryValue
        };

        Session.ConnectedClient.SendMessage(msg);

        return task;
    }

    public Task<DreamValue> Alert(String title, String message, String button1, String button2, String button3) {
        var task = MakePromptTask(out var promptId);
        var msg = new MsgAlert() {
            PromptId = promptId,
            Title = title,
            Message = message,
            Button1 = button1,
            Button2 = button2,
            Button3 = button3
        };

        Session.ConnectedClient.SendMessage(msg);
        return task;
    }

    private Task<DreamValue> MakePromptTask(out int promptId) {
        TaskCompletionSource<DreamValue> tcs = new();
        promptId = _nextPromptEvent++;

        _promptEvents.Add(promptId, response => {
            tcs.TrySetResult(response);
        });

        return tcs.Task;
    }

    public void BrowseResource(DreamResource resource, string filename) {
        if (resource.ResourceData == null)
            return;

        var msg = new MsgBrowseResource() {
            Filename = filename,
            Data = resource.ResourceData
        };

        Session?.ConnectedClient.SendMessage(msg);
    }

    public void Browse(string? body, string? options) {
        string? window = null;
        Vector2i size = (480, 480);

        if (options != null) {
            foreach (string option in options.Split(',', ';', '&')) {
                string optionTrimmed = option.Trim();

                if (optionTrimmed != string.Empty) {
                    string[] optionSeparated = optionTrimmed.Split("=", 2);
                    string key = optionSeparated[0];
                    string value = optionSeparated[1];

                    if (key == "window") {
                        window = value;
                    } else if (key == "size") {
                        string[] sizeSeparated = value.Split("x", 2);

                        size = (int.Parse(sizeSeparated[0]), int.Parse(sizeSeparated[1]));
                    }
                }
            }
        }

        var msg = new MsgBrowse() {
            Size = size,
            Window = window,
            HtmlSource = body
        };

        Session?.ConnectedClient.SendMessage(msg);
    }

    public void WinSet(string? controlId, string @params) {
        var msg = new MsgWinSet() {
            ControlId = controlId,
            Params = @params
        };

        Session?.ConnectedClient.SendMessage(msg);
    }

    public void WinClone(string controlId, string cloneId) {
        var msg = new MsgWinClone() { ControlId = controlId, CloneId = cloneId };

        Session?.ConnectedClient.SendMessage(msg);
    }

    /// <summary>
    /// Prompts the user to save a file to disk
    /// </summary>
    /// <param name="file">File to save</param>
    /// <param name="suggestedName">Suggested name to save the file as</param>
    public void SendFile(DreamResource file, string suggestedName) {
        var msg = new MsgFtp {
            ResourceId = file.Id,
            SuggestedName = suggestedName
        };

        Session?.ConnectedClient.SendMessage(msg);
    }
}
