using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Dream.Procs.Native;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace OpenDreamServer.Net {
    class DreamConnection {
        public string CKey = null;
        public List<int> PressedKeys = new List<int>();
        public DreamObject ClientDreamObject = null;
        public DreamObject MobDreamObject {
            get => _mobDreamObject;
            set {
                if (_mobDreamObject != value) {
                    if (_mobDreamObject != null) _mobDreamObject.CallProc("Logout");

                    if (value != null && value.IsSubtypeOf(DreamPath.Mob)) {
                        DreamConnection oldMobConnection = Program.DreamServer.GetConnectionFromMob(value);
                        if (oldMobConnection != null) oldMobConnection.MobDreamObject = null;

                        _mobDreamObject = value;
                        ClientDreamObject?.SetVariable("eye", new DreamValue(_mobDreamObject));
                        _mobDreamObject.CallProc("Login");
                    } else {
                        _mobDreamObject = null;
                    }

                    UpdateAvailableVerbs();
                }
            }
        }

        private DreamObject _mobDreamObject = null;
        private Dictionary<int, Action<DreamValue>> _promptEvents = new();
        private Dictionary<string, DreamProc> _availableVerbs = new();
        private Dictionary<string, List<string>> _statPanels = new();
        private string _selectedStatPanel;

        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private BinaryReader _tcpStreamBinaryReader;
        private BinaryWriter _tcpStreamBinaryWriter;
        private object _netLock = new object();

        public DreamConnection(TcpClient tcpClient) {
            _tcpClient = tcpClient;
            _tcpStream = _tcpClient.GetStream();
            _tcpStreamBinaryReader = new BinaryReader(_tcpStream);
            _tcpStreamBinaryWriter = new BinaryWriter(_tcpStream);
        }

        public byte[] ReadPacketData() {
            lock (_netLock) {
                if (_tcpClient.Connected && _tcpStream.DataAvailable) {
                    UInt32 packetDataLength = _tcpStreamBinaryReader.ReadUInt32();
                    byte[] packetData = new byte[packetDataLength];

                    int bytesRead = _tcpStream.Read(packetData, 0, (int)packetDataLength);
                    while (bytesRead < packetDataLength) {
                        bytesRead += _tcpStream.Read(packetData, bytesRead, (int)packetDataLength - bytesRead);
                    }

                    return packetData;
                } else {
                    return null;
                }
            }
        }

        public void SendPacket(IPacket packet) {
            PacketStream stream = new PacketStream();

            stream.WriteByte((byte)packet.PacketID);
            packet.WriteToStream(stream);

            lock (_netLock) {
                _tcpStreamBinaryWriter.Write((UInt32)stream.Length);
                _tcpStream.Write(stream.ToArray());
            }
        }

        public void OutputDreamValue(DreamValue value) {
            if (value.Type == DreamValue.DreamValueType.String) {
                SendPacket(new PacketOutput(value.GetValueAsString(), null));
            } else if (value.Type == DreamValue.DreamValueType.Integer) {
                SendPacket(new PacketOutput(value.GetValueAsInteger().ToString(), null));
            } else if (value.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject outputObject = value.GetValueAsDreamObject();

                if (outputObject != null) {
                    if (outputObject.IsSubtypeOf(DreamPath.Sound)) {
                        UInt16 channel = (UInt16)outputObject.GetVariable("channel").GetValueAsInteger();
                        DreamValue file = outputObject.GetVariable("file");
                        UInt16 volume = (UInt16)outputObject.GetVariable("volume").GetValueAsNumber();

                        if (file.IsType(DreamValue.DreamValueType.String) || file == DreamValue.Null) {
                            SendPacket(new PacketSound(channel, (string)file.Value, volume));
                        } else if (file.IsType(DreamValue.DreamValueType.DreamResource)) {
                            SendPacket(new PacketSound(channel, file.GetValueAsDreamResource().ResourcePath, volume));
                        } else {
                            throw new ArgumentException("Cannot output " + value, nameof(value));
                        }
                    }
                }
            } else {
                throw new ArgumentException("Cannot output " + value, nameof(value));
            }
        }

        public void Browse(string body, string options) {
            string window = null;
            Size size = new Size(480, 480);

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

                        size = new Size(int.Parse(sizeSeparated[0]), int.Parse(sizeSeparated[1]));
                    }
                }
            }

            SendPacket(new PacketBrowse(window, body) {
                Size = size
            });
        }

        public void BrowseResource(DreamResource resource, string filename) {
            SendPacket(new PacketBrowseResource(filename, resource.ResourceData));
        }

        public void OutputControl(string message, string control) {
            SendPacket(new PacketOutput(message, control));
        }

        public void UpdateAvailableVerbs() {
            _availableVerbs.Clear();

            if (MobDreamObject != null) {
                List<DreamValue> mobVerbPaths = MobDreamObject.GetVariable("verbs").GetValueAsDreamList().GetValues();
                foreach (DreamValue mobVerbPath in mobVerbPaths) {
                    DreamPath path = mobVerbPath.GetValueAsPath();

                    _availableVerbs.Add(path.LastElement, MobDreamObject.GetProc(path.LastElement));
                }
            }

            SendPacket(new PacketUpdateAvailableVerbs(_availableVerbs.Keys.ToArray()));
        }

        public void UpdateStat() {
            if (ClientDreamObject != null) {
                Task.Run(() => {
                    _statPanels.Clear();

                    ClientDreamObject.CallProc("Stat", new DreamProcArguments(null, null), _mobDreamObject);
                    SendPacket(new PacketUpdateStatPanels(_statPanels));
                });
            }
        }

        public void SelectStatPanel(string name) {
            _selectedStatPanel = name;
        }

        public void AddStatPanelLine(string text) {
            List<string> statPanelLines;
            if (!_statPanels.TryGetValue(_selectedStatPanel, out statPanelLines)) {
                statPanelLines = new List<string>();

                _statPanels.Add(_selectedStatPanel, statPanelLines);
            }

            statPanelLines.Add(text);
        }

        public Task<DreamValue> Prompt(DMValueType types, String title, String message, String defaultValue) {
            Task<DreamValue> promptTask = new Task<DreamValue>(() => {
                ManualResetEvent promptWaitHandle = new ManualResetEvent(false);
                int promptId = _promptEvents.Count;

                DreamValue promptResponse = DreamValue.Null;
                _promptEvents[promptId] = (DreamValue response) => {
                    promptResponse = response;
                    promptWaitHandle.Set();
                };

                SendPacket(new PacketPrompt(promptId, types, title, message, defaultValue));
                promptWaitHandle.WaitOne();
                return promptResponse;
            });

            promptTask.Start();
            return promptTask;
        }

        public void HandlePacketPromptResponse(PacketPromptResponse pPromptResponse) {
            if (_promptEvents.TryGetValue(pPromptResponse.PromptId, out Action<DreamValue> promptEvent)) {

                DreamValue value = pPromptResponse.Type switch {
                    DMValueType.Null => DreamValue.Null,
                    DMValueType.Text => new DreamValue((string)pPromptResponse.Value),
                    DMValueType.Num => new DreamValue((int)pPromptResponse.Value),
                    DMValueType.Message => new DreamValue((string)pPromptResponse.Value),
                    _ => throw new Exception("Invalid prompt response '" + pPromptResponse.Type + "'")
                };

                promptEvent.Invoke(value);
                _promptEvents[pPromptResponse.PromptId] = null;
            }
        }

        public void HandlePacketKeyboardInput(PacketKeyboardInput pKeyboardInput) {
            foreach (int key in pKeyboardInput.KeysDown) {
                if (!PressedKeys.Contains(key)) PressedKeys.Add(key);
            }

            foreach (int key in pKeyboardInput.KeysUp) {
                PressedKeys.Remove(key);
            }
        }

        public void HandlePacketClickAtom(PacketClickAtom pClickAtom) {
            if (DreamMetaObjectAtom.AtomIDToAtom.TryGetValue(pClickAtom.AtomID, out DreamObject atom)) {
                NameValueCollection paramsBuilder = HttpUtility.ParseQueryString(String.Empty);
                paramsBuilder.Add("icon-x", pClickAtom.IconX.ToString());
                paramsBuilder.Add("icon-y", pClickAtom.IconY.ToString());
                paramsBuilder.Add("screen-loc", pClickAtom.ScreenLocation.ToString());
                if (pClickAtom.ModifierShift) paramsBuilder.Add("shift", "1");
                if (pClickAtom.ModifierCtrl) paramsBuilder.Add("ctrl", "1");
                if (pClickAtom.ModifierAlt) paramsBuilder.Add("alt", "1");

                DreamProcArguments clickArguments = new DreamProcArguments(new() {
                    new DreamValue(atom),
                    DreamValue.Null,
                    DreamValue.Null,
                    new DreamValue(paramsBuilder.ToString())
                });

                Task.Run(() => ClientDreamObject?.CallProc("Click", clickArguments, MobDreamObject));
            }
        }

        public void HandlePacketTopic(PacketTopic pTopic) {
            DreamList hrefList = DreamProcNativeRoot.params2list(HttpUtility.UrlDecode(pTopic.Query));
            DreamValue srcRefValue = hrefList.GetValue(new DreamValue("src"));
            DreamObject src = null;

            if (srcRefValue.Value != null) {
                int srcRef = int.Parse(srcRefValue.GetValueAsString());

                src = DreamObject.GetFromReferenceID(srcRef);
            }

            DreamProcArguments topicArguments = new DreamProcArguments(new() {
                new DreamValue(pTopic.Query),
                new DreamValue(hrefList),
                new DreamValue(src)
            });

            Task.Run(() => ClientDreamObject?.CallProc("Topic", topicArguments, MobDreamObject));
        }

        public void HandlePacketCallVerb(PacketCallVerb pCallVerb) {
            if (_availableVerbs.TryGetValue(pCallVerb.VerbName, out DreamProc verb)) {
                Task.Run(async () => {
                    Dictionary<String, DreamValue> arguments = new();

                    for (int i = 0; i < verb.ArgumentNames.Count; i++) {
                        String argumentName = verb.ArgumentNames[i];
                        DMValueType argumentType = verb.ArgumentTypes[i];
                        DreamValue value = await Prompt(argumentType, title: null, // No settable title for verbs
                                                        argumentName, defaultValue: ""); // No default value for verbs

                        arguments.Add(argumentName, value);
                    }

                    try {
                        verb.Run(MobDreamObject, new DreamProcArguments(new(), arguments), MobDreamObject);
                    } catch (Exception e) {
                        Console.WriteLine("Exception while running verb \"" + pCallVerb.VerbName + "\": " + e.Message);
                    }
                });
            }
        }
    }
}
