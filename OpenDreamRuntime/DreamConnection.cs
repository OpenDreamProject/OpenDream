using System.Threading.Tasks;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
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
    private readonly ServerVerbSystem? _verbSystem;

    [ViewVariables] private readonly Dictionary<string, List<(string, string, string?)>> _statPanels = new();
    [ViewVariables] private bool _currentlyUpdatingStat;

    [ViewVariables] public ICommonSession? Session { get; private set; }
    [ViewVariables] public DreamObjectClient? Client { get; private set; }
    [ViewVariables] public string Key { get; private set; }

    [ViewVariables] public DreamObjectMob? Mob {
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
                    _mob.Key = Key;
                    _mob.SpawnProc("Login", usr: _mob);
                }
            }
        }
    }

    [ViewVariables] public DreamObjectMovable? Eye {
        get => _eye;
        set {
            _eye = value;
            _playerManager.SetAttachedEntity(Session!, _eye?.Entity);
        }
    }

    [ViewVariables]
    public DreamValue StatObj { get; set; } // This can be just any DreamValue. Only atoms will function though.

    [ViewVariables] private string? _outputStatPanel;
    [ViewVariables] private string? _selectedStatPanel;
    [ViewVariables] private readonly Dictionary<int, Action<DreamValue>> _promptEvents = new();
    [ViewVariables] private int _nextPromptEvent = 1;
    private readonly Dictionary<string, DreamResource> _permittedBrowseRscFiles = new();
    private DreamObjectMob? _mob;
    private DreamObjectMovable? _eye;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.connection");

    public string? SelectedStatPanel {
        get => _selectedStatPanel;
        set {
            _selectedStatPanel = value;

            var msg = new MsgSelectStatPanel() { StatPanel = value };
            Session?.Channel.SendMessage(msg);
        }
    }

    public DreamConnection(string key) {
        IoCManager.InjectDependencies(this);
        Key = key;

        _entitySystemManager.TryGetEntitySystem(out _screenOverlaySystem);
        _entitySystemManager.TryGetEntitySystem(out _clientImagesSystem);
        _entitySystemManager.TryGetEntitySystem(out _verbSystem);
    }

    public void HandleConnection(ICommonSession session) {
        Session = session;

        Client = new DreamObjectClient(_objectTree.Client.ObjectDefinition, this, _screenOverlaySystem, _clientImagesSystem);
        Client.InitSpawn(new());

        _verbSystem?.UpdateClientVerbs(Client);
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

    public void UpdateStat() {
        if (Session == null || Client == null || _currentlyUpdatingStat)
            return;

        _currentlyUpdatingStat = true;
        _statPanels.Clear();

        DreamThread.Run("Stat", async state => {
            try {
                var statProc = Client.GetProc("Stat");

                await state.Call(statProc, Client, Mob);
                if (Session.Status == SessionStatus.InGame) {
                    var msg = new MsgUpdateStatPanels(_statPanels);
                    Session?.Channel.SendMessage(msg);
                }

                return DreamValue.Null;
            } finally {
                _currentlyUpdatingStat = false;
            }
        });
    }

    public void SendClientInfoUpdate() {
        MsgUpdateClientInfo msg = new() {
            View = Client!.View,
            ShowPopupMenus = Client!.ShowPopupMenus
        };

        Session?.Channel.SendMessage(msg);
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

        if (!TryConvertPromptResponse(message.Type, message.Value, out var value))
            throw new Exception($"Invalid prompt response '{value}'");

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

            Session?.Channel.SendMessage(msg);
            return;
        }

        OutputControl(value.Stringify(), null);
    }

    public void OutputControl(string message, string? control) {
        var msg = new MsgOutput() {
            Value = message,
            Control = control
        };

        Session?.Channel.SendMessage(msg);
    }

    // TODO: Remove this. Vestigial and doesn't run all commands.
    public void HandleCommand(string fullCommand) {
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
        }
    }

    public Task<DreamValue> Prompt(DreamValueType types, string title, string message, string defaultValue) {
        var task = MakePromptTask(out var promptId);
        var msg = new MsgPrompt {
            PromptId = promptId,
            Title = title,
            Message = message,
            Types = types,
            DefaultValue = defaultValue
        };

        Session?.Channel.SendMessage(msg);
        return task;
    }

    public async Task<DreamValue> PromptList(DreamValueType types, DreamList list, string title, string message, DreamValue defaultValue) {
        List<DreamValue> listValues = list.GetValues();

        List<string> promptValues = new(listValues.Count);
        foreach (var value in listValues) {
            if (types.HasFlag(DreamValueType.Obj) && !value.TryGetValueAsDreamObject<DreamObjectMovable>(out _))
                continue;
            if (types.HasFlag(DreamValueType.Mob) && !value.TryGetValueAsDreamObject<DreamObjectMob>(out _))
                continue;
            if (types.HasFlag(DreamValueType.Turf) && !value.TryGetValueAsDreamObject<DreamObjectTurf>(out _))
                continue;
            if (types.HasFlag(DreamValueType.Area) && !value.TryGetValueAsDreamObject<DreamObjectArea>(out _))
                continue;

            promptValues.Add(value.Stringify());
        }

        if (promptValues.Count == 0)
            return DreamValue.Null;

        var task = MakePromptTask(out var promptId);
        var msg = new MsgPromptList {
            PromptId = promptId,
            Title = title,
            Message = message,
            CanCancel = (types & DreamValueType.Null) == DreamValueType.Null,
            DefaultValue = defaultValue.Stringify(),
            Values = promptValues.ToArray()
        };

        Session?.Channel.SendMessage(msg);

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

        Session?.Channel.SendMessage(msg);

        return task;
    }

    public Task<DreamValue> WinGet(string controlId, string queryValue) {
        var task = MakePromptTask(out var promptId);
        var msg = new MsgWinGet() {
            PromptId = promptId,
            ControlId = controlId,
            QueryValue = queryValue
        };

        Session?.Channel.SendMessage(msg);

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

        Session?.Channel.SendMessage(msg);
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
            DataHash = resource.ResourceData.Length //TODO: make a quick hash that can work clientside too
        };
        _permittedBrowseRscFiles[filename] = resource;

        Session?.Channel.SendMessage(msg);
    }

    public void HandleBrowseResourceRequest(string filename) {
        if(_permittedBrowseRscFiles.TryGetValue(filename, out var dreamResource)) {
            var msg = new MsgBrowseResourceResponse() {
                Filename = filename,
                Data = dreamResource.ResourceData! //honestly if this is null, something mega fucked up has happened and we should error hard
            };
            _permittedBrowseRscFiles.Remove(filename);
            Session?.Channel.SendMessage(msg);
        } else {
            _sawmill.Error($"Client({Session}) requested a browse_rsc file they had not been permitted to request ({filename}).");
        }

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

        Session?.Channel.SendMessage(msg);
    }

    public void WinSet(string? controlId, string @params) {
        var msg = new MsgWinSet() {
            ControlId = controlId,
            Params = @params
        };

        Session?.Channel.SendMessage(msg);
    }

    public void WinClone(string controlId, string cloneId) {
        var msg = new MsgWinClone() { ControlId = controlId, CloneId = cloneId };

        Session?.Channel.SendMessage(msg);
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

        Session?.Channel.SendMessage(msg);
    }

    public bool TryConvertPromptResponse(DreamValueType type, object? value, out DreamValue converted) {
        bool CanBe(DreamValueType canBeType) => (type == DreamValueType.Anything) || ((type & canBeType) != 0x0);

        if (CanBe(DreamValueType.Null) && value == null) {
            converted = DreamValue.Null;
            return true;
        } else if (CanBe(DreamValueType.Text | DreamValueType.Message | DreamValueType.CommandText) && value is string strVal) {
            converted = new(strVal);
            return true;
        } else if (CanBe(DreamValueType.Num) && value is float numVal) {
            converted = new DreamValue(numVal);
            return true;
        } else if (CanBe(DreamValueType.Color) && value is Color colorVal) {
            converted = new DreamValue(colorVal.ToHexNoAlpha());
            return true;
        } else if (CanBe(type & DreamValueType.AllAtomTypes) && value is ClientObjectReference clientRef) {
            var atom = _dreamManager.GetFromClientReference(this, clientRef);

            if (atom != null) {
                if ((atom.IsSubtypeOf(_objectTree.Obj) && !CanBe(DreamValueType.Obj)) ||
                    (atom.IsSubtypeOf(_objectTree.Mob) && !CanBe(DreamValueType.Mob))) {
                    converted = default;
                    return false;
                }

                converted = new(atom);
                return true;
            }
        }

        converted = default;
        return false;
    }
}
