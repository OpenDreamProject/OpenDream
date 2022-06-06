using System.Threading.Tasks;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace OpenDreamRuntime
{
    public sealed class DreamConnection
    {
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;

        [ViewVariables] private Dictionary<string, DreamProc> _availableVerbs = new();
        [ViewVariables] private Dictionary<string, List<string>> _statPanels = new();
        [ViewVariables] private bool _currentlyUpdatingStat;
        [ViewVariables] public IPlayerSession Session { get; }

        [ViewVariables] public DreamObject ClientDreamObject;

        [ViewVariables] private DreamObject _mobDreamObject;

        [ViewVariables] private string _outputStatPanel;
        [ViewVariables] private string _selectedStatPanel;
        [ViewVariables] private Dictionary<int, Action<DreamValue>> _promptEvents = new();
        [ViewVariables] private int _nextPromptEvent = 1;

        public string SelectedStatPanel {
            get => _selectedStatPanel;
            set {
                _selectedStatPanel = value;

                var msg = new MsgSelectStatPanel() { StatPanel = value };
                Session.ConnectedClient.SendMessage(msg);
            }
        }

        public DreamObject MobDreamObject
        {
            get => _mobDreamObject;
            set
            {
                if (_mobDreamObject != value)
                {
                    if (_mobDreamObject != null) _mobDreamObject.SpawnProc("Logout");

                    if (value != null && value.IsSubtypeOf(DreamPath.Mob))
                    {
                        DreamConnection oldMobConnection = _dreamManager.GetConnectionFromMob(value);
                        if (oldMobConnection != null) oldMobConnection.MobDreamObject = null;

                        _mobDreamObject = value;
                        ClientDreamObject?.SetVariable("eye", new DreamValue(_mobDreamObject));
                        _mobDreamObject.SpawnProc("Login");
                        Session.AttachToEntity(_atomManager.GetAtomEntity(_mobDreamObject));
                    }
                    else
                    {
                        Session.DetachFromEntity();
                        _mobDreamObject = null;
                    }

                    UpdateAvailableVerbs();
                }
            }
        }

        public DreamConnection(IPlayerSession session)
        {
            IoCManager.InjectDependencies(this);

            Session = session;
        }

        public void UpdateAvailableVerbs()
        {
            _availableVerbs.Clear();
            List<(string, string, string)>? verbs = null;

            if (MobDreamObject != null)
            {
                List<DreamValue> mobVerbPaths = MobDreamObject.GetVariable("verbs").GetValueAsDreamList().GetValues();
                verbs = new List<(string, string, string)>(mobVerbPaths.Count);
                foreach (DreamValue mobVerbPath in mobVerbPaths)
                {
                    DreamPath path = mobVerbPath.GetValueAsPath();
                    if (path.LastElement is null) continue;
                    var proc = MobDreamObject.GetProc(path.LastElement);
                    _availableVerbs.Add(path.LastElement, proc);

                    // Don't send hidden verbs. Names starting with "." count as hidden.
                    if ((proc.Attributes & ProcAttributes.Hidden) == ProcAttributes.Hidden ||
                        (proc.VerbName != null && proc.VerbName[0] == '.'))
                    {
                        continue;
                    }

                    string? category = proc.VerbCategory;
                    // Explicitly null category is hidden from verb panels, "" category becomes the default_verb_category
                    if (category == string.Empty)
                    {
                        // But if default_verb_category is null, we hide it from the verb panel
                        category = ClientDreamObject.GetVariable("default_verb_category").TryGetValueAsString(out var value) ? value : null;
                    }
                    // No set name is serialized as an empty string and the last element will be used
                    // Null category is serialized as an empty string and treated as hidden
                    verbs.Add((path.LastElement, proc.VerbName ?? string.Empty, category ?? string.Empty));
                }
            }

            var msg = new MsgUpdateAvailableVerbs() {
                AvailableVerbs = verbs?.ToArray() ?? Array.Empty<(string, string, string)>()
            };

            Session.ConnectedClient.SendMessage(msg);
        }

        public void UpdateStat()
        {
            if (_currentlyUpdatingStat)
                return;

            _currentlyUpdatingStat = true;
            _statPanels.Clear();

            DreamThread.Run(async (state) =>
            {
                try
                {
                    var statProc = ClientDreamObject.GetProc("Stat");

                    await state.Call(statProc, ClientDreamObject, _mobDreamObject, new DreamProcArguments(null));
                    if (Session.Status == SessionStatus.InGame)
                    {
                        var msg = new MsgUpdateStatPanels() { StatPanels = _statPanels };
                        Session.ConnectedClient.SendMessage(msg);
                    }

                    return DreamValue.Null;
                }
                finally
                {
                    _currentlyUpdatingStat = false;
                }
            });
        }

        public void SetOutputStatPanel(string name)
        {
            if (!_statPanels.ContainsKey(name)) _statPanels.Add(name, new List<string>());

            _outputStatPanel = name;
        }

        public void AddStatPanelLine(string text)
        {
            if (_outputStatPanel == null || !_statPanels.ContainsKey(_outputStatPanel))
                SetOutputStatPanel("Stats");

            _statPanels[_outputStatPanel].Add(text);
        }

        public void HandleMsgSelectStatPanel(MsgSelectStatPanel message)
        {
            _selectedStatPanel = message.StatPanel;
        }

        public void HandleMsgPromptResponse(MsgPromptResponse message)
        {
            if (!_promptEvents.TryGetValue(message.PromptId, out Action<DreamValue> promptEvent))
            {
                Logger.Warning($"{message.MsgChannel}: Received MsgPromptResponse for prompt {message.PromptId} which does not exist.");
                return;
            }

            DreamValue value = message.Type switch {
                DMValueType.Null => DreamValue.Null,
                DMValueType.Text or DMValueType.Message => new DreamValue((string)message.Value),
                DMValueType.Num => new DreamValue((float)message.Value),
                _ => throw new Exception("Invalid prompt response '" + message.Type + "'")
            };

            promptEvent.Invoke(value);
            _promptEvents.Remove(message.PromptId);
        }

        public void HandleMsgTopic(MsgTopic pTopic) {
            DreamList hrefList = DreamProcNativeRoot.params2list(HttpUtility.UrlDecode(pTopic.Query));
            DreamValue srcRefValue = hrefList.GetValue(new DreamValue("src"));
            DreamObject src = null;

            if (srcRefValue.Value != null && int.TryParse(srcRefValue.GetValueAsString(), out var srcRef)) {
                src = DreamObject.GetFromReferenceID(_dreamManager, srcRef);
            }

            DreamProcArguments topicArguments = new DreamProcArguments(new() {
                new DreamValue(pTopic.Query),
                new DreamValue(hrefList),
                new DreamValue(src)
            });

            ClientDreamObject?.SpawnProc("Topic", topicArguments, MobDreamObject);
        }


        public void OutputDreamValue(DreamValue value) {
            if (value.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject outputObject = value.GetValueAsDreamObject();

                if (outputObject?.IsSubtypeOf(DreamPath.Sound) == true) {
                    UInt16 channel = (UInt16)outputObject.GetVariable("channel").GetValueAsInteger();
                    UInt16 volume = (UInt16)outputObject.GetVariable("volume").GetValueAsInteger();
                    DreamValue file = outputObject.GetVariable("file");

                    var msg = new MsgSound() {
                        Channel = channel,
                        Volume = volume
                    };

                    if (file.Type == DreamValue.DreamValueType.String || file == DreamValue.Null) {
                        msg.File = (string)file.Value;
                    } else if (file.TryGetValueAsDreamResource(out DreamResource resource)) {
                        msg.File = resource.ResourcePath;
                    } else {
                        throw new ArgumentException("Cannot output " + value, nameof(value));
                    }

                    Session.ConnectedClient.SendMessage(msg);

                    return;
                }
            }

            OutputControl(value.Stringify(), null);
        }

        public void OutputControl(string message, string control) {
            var msg = new MsgOutput() {
                Value = message,
                Control = control
            };

            Session.ConnectedClient.SendMessage(msg);
        }

        public void HandleCommand(string command)
        {
            switch (command) {
                //TODO: Maybe move these verbs to DM code?
                case ".north": ClientDreamObject.SpawnProc("North"); break;
                case ".east": ClientDreamObject.SpawnProc("East"); break;
                case ".south": ClientDreamObject.SpawnProc("South"); break;
                case ".west": ClientDreamObject.SpawnProc("West"); break;
                case ".northeast": ClientDreamObject.SpawnProc("Northeast"); break;
                case ".southeast": ClientDreamObject.SpawnProc("Southeast"); break;
                case ".southwest": ClientDreamObject.SpawnProc("Southwest"); break;
                case ".northwest": ClientDreamObject.SpawnProc("Northwest"); break;
                case ".center": ClientDreamObject.SpawnProc("Center"); break;

                default: {
                    if (_availableVerbs.TryGetValue(command, out DreamProc verb)) {
                        DreamThread.Run(async (state) => {
                            Dictionary<String, DreamValue> arguments = new();

                            // TODO: this should probably be done on the client, shouldn't it?
                            for (int i = 0; i < (verb.ArgumentNames?.Count ?? 0); i++) {
                                String argumentName = verb.ArgumentNames[i];
                                DMValueType argumentType = verb.ArgumentTypes[i];
                                DreamValue value = await Prompt(argumentType, title: String.Empty, // No settable title for verbs
                                    argumentName, defaultValue: String.Empty); // No default value for verbs

                                arguments.Add(argumentName, value);
                            }

                            await state.Call(verb, MobDreamObject, MobDreamObject, new DreamProcArguments(new(), arguments));
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


        public Task<DreamValue> Alert(String title, String message, String button1, String button2, String button3)
        {
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

        private Task<DreamValue> MakePromptTask(out int promptId)
        {
            TaskCompletionSource<DreamValue> tcs = new();
            promptId = _nextPromptEvent++;

            _promptEvents.Add(promptId, response => {
                tcs.TrySetResult(response);
            });

            return tcs.Task;
        }

        public void BrowseResource(DreamResource resource, string filename)
        {
            var msg = new MsgBrowseResource() {
                Filename = filename,
                Data = resource.ResourceData
            };

            Session.ConnectedClient.SendMessage(msg);
        }

        public void Browse(string body, string options) {
            string window = null;
            Vector2i size = (480, 480);

            string[] separated = options.Split(',', ';', '&');
            foreach (string option in separated) {
                string optionTrimmed = option.Trim();

                if (optionTrimmed != String.Empty) {
                    string[] optionSeparated = optionTrimmed.Split("=");
                    string key = optionSeparated[0];
                    string value = optionSeparated[1];

                    if (key == "window") window = value;
                    if (key == "size") {
                        string[] sizeSeparated = value.Split("x");

                        size = (int.Parse(sizeSeparated[0]), int.Parse(sizeSeparated[1]));
                    }
                }
            }

            var msg = new MsgBrowse() {
                Size = size,
                Window = window,
                HtmlSource = body
            };

            Session.ConnectedClient.SendMessage(msg);
        }

        public void WinSet(string controlId, string @params) {
            var msg = new MsgWinSet() {
                ControlId = controlId,
                Params = @params
            };

            Session.ConnectedClient.SendMessage(msg);
        }
    }
}
