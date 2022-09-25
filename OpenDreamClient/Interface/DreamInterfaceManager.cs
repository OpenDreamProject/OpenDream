using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Interface;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Prompts;
using OpenDreamClient.Resources;
using OpenDreamShared.Compiler.DMF;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;

namespace OpenDreamClient.Interface {
    sealed class DreamInterfaceManager : IDreamInterfaceManager {
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IDreamMacroManager _macroManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IDreamResourceManager _dreamResource = default!;
        [Dependency] private readonly IFileDialogManager _fileDialogManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        public InterfaceDescriptor InterfaceDescriptor { get; private set; }

        public ControlWindow DefaultWindow;
        public ControlOutput DefaultOutput;
        public ControlInfo DefaultInfo;
        public ControlMap DefaultMap;

        public (string, string, string)[] AvailableVerbs { get; private set; } = Array.Empty<(string, string, string)>();

        public Dictionary<string, ControlWindow> Windows { get; } = new();
        public readonly Dictionary<string, BrowsePopup> PopupWindows = new();

        public void LoadInterfaceFromSource(string source) {
            DMFLexer dmfLexer = new DMFLexer("interface.dmf", source);
            DMFParser dmfParser = new DMFParser(dmfLexer);

            InterfaceDescriptor interfaceDescriptor = null;
            try {
                interfaceDescriptor = dmfParser.Interface();
            } catch (CompileErrorException) { }

            if (dmfParser.Warnings.Count > 0) {
                foreach (CompilerWarning warning in dmfParser.Warnings) {
                    Logger.Warning(warning.ToString());
                }
            }

            if (dmfParser.Errors.Count > 0 || interfaceDescriptor == null) {
                foreach (CompilerError error in dmfParser.Errors) {
                    Logger.Error(error.ToString());
                }

                throw new Exception("Errors while parsing interface data");
            }

            LoadInterface(interfaceDescriptor);
        }

        public void Initialize()
        {
            _userInterfaceManager.MainViewport.Visible = false;

            _netManager.RegisterNetMessage<MsgUpdateStatPanels>(RxUpdateStatPanels);
            _netManager.RegisterNetMessage<MsgSelectStatPanel>(RxSelectStatPanel);
            _netManager.RegisterNetMessage<MsgUpdateAvailableVerbs>(RxUpdateAvailableVerbs);
            _netManager.RegisterNetMessage<MsgOutput>(RxOutput);
            _netManager.RegisterNetMessage<MsgAlert>(RxAlert);
            _netManager.RegisterNetMessage<MsgPrompt>(RxPrompt);
            _netManager.RegisterNetMessage<MsgPromptList>(RxPromptList);
            _netManager.RegisterNetMessage<MsgPromptResponse>();
            _netManager.RegisterNetMessage<MsgBrowse>(RxBrowse);
            _netManager.RegisterNetMessage<MsgTopic>();
            _netManager.RegisterNetMessage<MsgWinSet>(RxWinSet);
            _netManager.RegisterNetMessage<MsgWinExists>(RxWinExists);
            _netManager.RegisterNetMessage<MsgLoadInterface>(RxLoadInterface);
            _netManager.RegisterNetMessage<MsgAckLoadInterface>();
        }

        private void RxUpdateStatPanels(MsgUpdateStatPanels message)
        {
            DefaultInfo?.UpdateStatPanels(message);
        }

        private void RxSelectStatPanel(MsgSelectStatPanel message)
        {
            DefaultInfo?.SelectStatPanel(message.StatPanel);
        }

        private void RxUpdateAvailableVerbs(MsgUpdateAvailableVerbs message)
        {
            AvailableVerbs = message.AvailableVerbs;
            foreach (var verb in AvailableVerbs)
            {
                // Verb category
                if (verb.Item3 != string.Empty && !DefaultInfo.HasVerbPanel(verb.Item3))
                {
                    DefaultInfo.CreateVerbPanel(verb.Item3);
                }
            }
            DefaultInfo?.RefreshVerbs();

        }

        public void RxOutput(MsgOutput pOutput) {
            InterfaceControl interfaceElement;
            string data = null;

            if (pOutput.Control != null) {
                string[] split = pOutput.Control.Split(":");

                interfaceElement = (InterfaceControl)FindElementWithName(split[0]);
                if (split.Length > 1) data = split[1];
            } else {
                interfaceElement = DefaultOutput;
            }

            if (interfaceElement != null) interfaceElement.Output(pOutput.Value, data);
        }

        private void RxAlert(MsgAlert message)
        {
            OpenAlert(
                message.PromptId,
                message.Title,
                message.Message,
                message.Button1, message.Button2, message.Button3);
        }

        public void OpenAlert(int promptId, string title, string message, string button1, string button2 = "",
            string button3 = "") {
            var alert = new AlertWindow(
                promptId,
                title,
                message,
                button1, button2, button3);

            alert.Owner = _clyde.MainWindow;
            alert.Show();
        }

        private void RxPrompt(MsgPrompt pPrompt) {
            PromptWindow prompt = null;
            bool canCancel = (pPrompt.Types & DMValueType.Null) == DMValueType.Null;

            if ((pPrompt.Types & DMValueType.Text) == DMValueType.Text) {
                prompt = new TextPrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel);
            } else if ((pPrompt.Types & DMValueType.Num) == DMValueType.Num) {
                prompt = new NumberPrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel);
            } else if ((pPrompt.Types & DMValueType.Message) == DMValueType.Message) {
                prompt = new MessagePrompt(pPrompt.PromptId, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel);
            }

            if (prompt != null) {
                ShowPrompt(prompt);
            }
        }

        private void RxPromptList(MsgPromptList pPromptList) {
            var prompt = new ListPrompt(
                pPromptList.PromptId,
                pPromptList.Title,
                pPromptList.Message,
                pPromptList.DefaultValue,
                pPromptList.CanCancel,
                pPromptList.Values
            );

            ShowPrompt(prompt);
        }

        private void RxBrowse(MsgBrowse pBrowse)
        {
            if (pBrowse.HtmlSource == null && pBrowse.Window != null) { //Closing a popup
                if (PopupWindows.TryGetValue(pBrowse.Window, out BrowsePopup popup)) {
                    popup.Close();
                }
            } else if (pBrowse.HtmlSource != null) { //Outputting to a browser
                string htmlFileName;
                ControlBrowser outputBrowser;
                BrowsePopup popup = null;

                if (pBrowse.Window != null) {
                    htmlFileName = pBrowse.Window;
                    outputBrowser = FindElementWithName(pBrowse.Window) as ControlBrowser;

                    if (outputBrowser == null) {

                        if (!PopupWindows.TryGetValue(pBrowse.Window, out popup))
                        {
                            popup = new BrowsePopup(pBrowse.Window, pBrowse.Size, _clyde.MainWindow);
                            popup.Closed += () => {
                                PopupWindows.Remove(pBrowse.Window);
                            };

                            PopupWindows.Add(pBrowse.Window, popup);
                        }

                        outputBrowser = popup.Browser;
                    }
                } else {
                    //TODO: Find embedded browser panel
                    htmlFileName = null;
                    outputBrowser = null;
                }

                var cacheFile = _dreamResource.CreateCacheFile(htmlFileName + ".html", pBrowse.HtmlSource);
                outputBrowser?.SetFileSource(cacheFile, true);

                popup?.Open();
            }
        }

        private void RxWinSet(MsgWinSet message) {
            WinSet(message.ControlId, message.Params);
        }

        private void RxWinExists(MsgWinExists message) {
            InterfaceElement element = FindElementWithName(message.ControlId);
            MsgPromptResponse response = new() {
                PromptId = message.PromptId,
                Type = DMValueType.Text,
                Value = (element != null) ? element.Type : String.Empty
            };

            _netManager.ClientSendMessage(response);
        }

        private void RxLoadInterface(MsgLoadInterface message)
        {
            LoadInterfaceFromSource(message.InterfaceText);

            _netManager.ClientSendMessage(new MsgAckLoadInterface());
        }

        private void ShowPrompt(PromptWindow prompt) {
            prompt.Owner = _clyde.MainWindow;
            prompt.Show();
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs)
        {
            if (DefaultMap != null)
                DefaultMap.Viewport.Eye = _eyeManager.CurrentEye;
        }

        public InterfaceElement FindElementWithName(string name) {
            string[] split = name.Split(".");

            if (split.Length == 2) {
                string windowName = split[0];
                string elementName = split[1];
                ControlWindow window = null;

                if (Windows.ContainsKey(windowName)) {
                    window = Windows[windowName];
                } else if (PopupWindows.TryGetValue(windowName, out BrowsePopup popup)) {
                    window = popup.WindowElement;
                }

                if (window != null) {
                    foreach (InterfaceControl element in window.ChildControls) {
                        if (element.Name == elementName) return element;
                    }
                }
            } else {
                string elementName = split[0];

                foreach (ControlWindow window in Windows.Values) {
                    foreach (InterfaceControl element in window.ChildControls) {
                        if (element.Name == elementName) return element;
                    }
                }

                if (Windows.TryGetValue(elementName, out ControlWindow windowElement)) return windowElement;
            }

            return null;
        }

        public void SaveScreenshot(bool openDialog)
        {
            // ReSharper disable once AsyncVoidLambda
            DefaultMap?.Viewport.Screenshot(async img =>
            {
                //TODO: Support automatically choosing a location if openDialog == false
                var filters = new FileDialogFilters(new FileDialogFilters.Group("png"));
                var tuple = await _fileDialogManager.SaveFile(filters);
                if (tuple == null)
                    return;

                await using var file = tuple.Value.fileStream;
                await img.SaveAsPngAsync(file);
            });
        }

        public void WinSet(string controlId, string winsetParams) {
            DMFLexer lexer = new DMFLexer($"winset({controlId}, \"{winsetParams}\")", winsetParams);
            DMFParser parser = new DMFParser(lexer);
            MappingDataNode node = parser.Attributes();

            if (parser.Errors.Count > 0) {
                foreach (CompilerError error in parser.Errors) {
                    Logger.ErrorS("opendream.interface.winset", error.ToString());
                }

                return;
            }

            foreach (CompilerWarning warning in parser.Warnings) {
                Logger.WarningS("opendream.interface.winset", warning.ToString());
            }

            if (String.IsNullOrEmpty(controlId)) {
                if (node.TryGet("command", out ValueDataNode command)) {
                    DreamCommandSystem commandSystem = _entitySystemManager.GetEntitySystem<DreamCommandSystem>();

                    commandSystem.RunCommand(command.Value);
                } else {
                    Logger.ErrorS("opendream.interface.winset", $"Invalid global winset \"{winsetParams}\"");
                }
            } else {
                InterfaceElement element = FindElementWithName(controlId);

                if (element != null) {
                    element.PopulateElementDescriptor(node, _serializationManager);
                } else {
                    Logger.ErrorS("opendream.interface.winset", $"Invalid element \"{controlId}\"");
                }
            }
        }

        private void LoadInterface(InterfaceDescriptor descriptor)
        {
            InterfaceDescriptor = descriptor;

            _macroManager.LoadMacroSets(InterfaceDescriptor.MacroSetDescriptors);
            _macroManager.SetActiveMacroSet(InterfaceDescriptor.MacroSetDescriptors[0]);

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                ControlWindow window = new ControlWindow(windowDescriptor);

                Windows.Add(windowDescriptor.Name, window);
                if (window.IsDefault) {
                    DefaultWindow = window;
                }
            }

            foreach (ControlWindow window in Windows.Values) {
                window.CreateChildControls(this);

                foreach (InterfaceControl control in window.ChildControls) {
                    if (control.IsDefault) {
                        switch (control) {
                            case ControlOutput controlOutput: DefaultOutput = controlOutput; break;
                            case ControlInfo controlInfo: DefaultInfo = controlInfo; break;
                            case ControlMap controlMap: DefaultMap = controlMap; break;
                        }
                    }
                }
            }

            DefaultWindow.RegisterOnClydeWindow(_clyde.MainWindow);
            DefaultWindow.UIElement.Name = "MainWindow";

            LayoutContainer.SetAnchorRight(DefaultWindow.UIElement, 1);
            LayoutContainer.SetAnchorBottom(DefaultWindow.UIElement, 1);

            _userInterfaceManager.StateRoot.AddChild(DefaultWindow.UIElement);
        }
    }

    public interface IDreamInterfaceManager
    {
        (string, string, string)[] AvailableVerbs { get; }
        Dictionary<string, ControlWindow> Windows { get; }
        public InterfaceDescriptor InterfaceDescriptor { get; }

        void Initialize();
        void FrameUpdate(FrameEventArgs frameEventArgs);
        InterfaceElement FindElementWithName(string name);
        void SaveScreenshot(bool openDialog);
        void LoadInterfaceFromSource(string source);
        void WinSet(string controlId, string winsetParams);
    }
}
