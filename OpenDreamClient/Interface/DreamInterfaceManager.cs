﻿using JetBrains.Annotations;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using OpenDreamClient.Interface.Prompts;
using OpenDreamClient.Resources;
using Robust.Client.Graphics;
using Robust.Client.Input;
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
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IDreamResourceManager _dreamResource = default!;
        [Dependency] private readonly IFileDialogManager _fileDialogManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        public InterfaceDescriptor InterfaceDescriptor { get; private set; }

        public ControlWindow DefaultWindow { get; private set; }
        public ControlOutput DefaultOutput { get; private set; }
        public ControlInfo DefaultInfo { get; private set; }
        public ControlMap DefaultMap { get; private set; }

        public (string, string, string)[] AvailableVerbs { get; private set; }

        public Dictionary<string, ControlWindow> Windows { get; } = new();
        public Dictionary<string, InterfaceMenu> Menus { get; } = new();
        public Dictionary<string, InterfaceMacroSet> MacroSets { get; } = new();

        private readonly Dictionary<string, BrowsePopup> _popupWindows = new();

        public void LoadInterfaceFromSource(string source) {
            DMFLexer dmfLexer = new DMFLexer("interface.dmf", source);
            DMFParser dmfParser = new DMFParser(dmfLexer, _serializationManager);

            InterfaceDescriptor interfaceDescriptor = null;
            try {
                interfaceDescriptor = dmfParser.Interface();
            } catch (CompileErrorException) { }

            if (dmfParser.Emissions.Count > 0) {
                bool wasError = interfaceDescriptor == null;

                foreach (CompilerEmission warning in dmfParser.Emissions) {
                    if (warning.Level == ErrorLevel.Error) {
                        Logger.Error(warning.ToString());
                        wasError = true;
                    } else {
                        Logger.Warning(warning.ToString());
                    }
                }

                if(wasError)
                    throw new Exception($"{dmfParser.Emissions.Count} Errors while parsing interface data");
            }

            LoadInterface(interfaceDescriptor);
        }

        public void Initialize() {
            _userInterfaceManager.MainViewport.Visible = false;

            AvailableVerbs = Array.Empty<(string, string, string)>();
            Windows.Clear();
            Menus.Clear();
            MacroSets.Clear();
            _popupWindows.Clear();

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

        private void RxUpdateStatPanels(MsgUpdateStatPanels message) {
            DefaultInfo?.UpdateStatPanels(message);
        }

        private void RxSelectStatPanel(MsgSelectStatPanel message) {
            DefaultInfo?.SelectStatPanel(message.StatPanel);
        }

        private void RxUpdateAvailableVerbs(MsgUpdateAvailableVerbs message) {
            AvailableVerbs = message.AvailableVerbs;

            // Verbs are displayed alphabetically with uppercase coming first
            Array.Sort(AvailableVerbs, (a, b) => String.CompareOrdinal(a.Item1, b.Item1));

            foreach (var verb in AvailableVerbs) {
                // Verb category
                if (verb.Item3 != string.Empty && !DefaultInfo.HasVerbPanel(verb.Item3)) {
                    DefaultInfo.CreateVerbPanel(verb.Item3);
                }
            }

            DefaultInfo?.RefreshVerbs();
        }

        private void RxOutput(MsgOutput pOutput) {
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

        private void RxAlert(MsgAlert message) {
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

        private void RxBrowse(MsgBrowse pBrowse) {
            if (pBrowse.HtmlSource == null && pBrowse.Window != null) {
                //Closing a popup
                if (_popupWindows.TryGetValue(pBrowse.Window, out BrowsePopup popup)) {
                    popup.Close();
                }
            } else if (pBrowse.HtmlSource != null) {
                //Outputting to a browser
                string htmlFileName;
                ControlBrowser outputBrowser;
                BrowsePopup popup = null;

                if (pBrowse.Window != null) {
                    htmlFileName = pBrowse.Window;
                    outputBrowser = FindElementWithName(pBrowse.Window) as ControlBrowser;

                    if (outputBrowser == null) {

                        if (!_popupWindows.TryGetValue(pBrowse.Window, out popup)) {
                            popup = new BrowsePopup(pBrowse.Window, pBrowse.Size, _clyde.MainWindow);
                            popup.Closed += () => { _popupWindows.Remove(pBrowse.Window); };

                            _popupWindows.Add(pBrowse.Window, popup);
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

        private void RxLoadInterface(MsgLoadInterface message) {
            LoadInterfaceFromSource(message.InterfaceText);

            _netManager.ClientSendMessage(new MsgAckLoadInterface());
        }

        private void ShowPrompt(PromptWindow prompt) {
            prompt.Owner = _clyde.MainWindow;
            prompt.Show();
        }

        public void FrameUpdate(FrameEventArgs frameEventArgs) {
            if (DefaultMap != null)
                DefaultMap.Viewport.Eye = _eyeManager.CurrentEye;
        }

        [CanBeNull]
        public InterfaceElement FindElementWithName(string name) {
            string[] split = name.Split(".");

            if (split.Length == 2) {
                string windowName = split[0];
                string elementName = split[1];
                ControlWindow window = null;

                if (Windows.ContainsKey(windowName)) {
                    window = Windows[windowName];
                } else if (_popupWindows.TryGetValue(windowName, out BrowsePopup popup)) {
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
                    if (window.Name == elementName)
                        return window;

                    foreach (InterfaceControl element in window.ChildControls) {
                        if (element.Name == elementName) return element;
                    }
                }

                foreach (InterfaceMenu menu in Menus.Values) {
                    if (menu.Name == elementName)
                        return menu;

                    if (menu.MenuElements.TryGetValue(elementName, out var menuElement))
                        return menuElement;
                }

                if (MacroSets.TryGetValue(elementName, out var macroSet)) return macroSet;
            }

            return null;
        }

        public void SaveScreenshot(bool openDialog) {
            // ReSharper disable once AsyncVoidLambda
            DefaultMap?.Viewport.Screenshot(async img => {
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
            DMFParser parser = new DMFParser(lexer, _serializationManager);

            bool CheckParserErrors() {
                if (parser.Emissions.Count > 0) {
                    bool hadError = false;
                    foreach (CompilerEmission emission in parser.Emissions) {
                        if (emission.Level == ErrorLevel.Error) {
                            Logger.ErrorS("opendream.interface.winset", emission.ToString());
                            hadError = true;
                        } else {
                            Logger.WarningS("opendream.interface.winset", emission.ToString());
                        }
                    }

                    return hadError;
                }

                return false;
            }

            if (String.IsNullOrEmpty(controlId)) {
                List<DMFWinSet> winSets = parser.GlobalWinSet();

                if (CheckParserErrors())
                    return;

                foreach (DMFWinSet winSet in winSets) {
                    if (winSet.Element == null) {
                        if (winSet.Attribute == "command") {
                            DreamCommandSystem commandSystem = _entitySystemManager.GetEntitySystem<DreamCommandSystem>();

                            commandSystem.RunCommand(winSet.Value);
                        } else {
                            Logger.ErrorS("opendream.interface.winset", $"Invalid global winset \"{winsetParams}\"");
                        }
                    } else {
                        InterfaceElement element = FindElementWithName(winSet.Element);
                        MappingDataNode node = new() {
                            {winSet.Attribute, winSet.Value}
                        };

                        if (element != null) {
                            element.PopulateElementDescriptor(node, _serializationManager);
                        } else {
                            Logger.ErrorS("opendream.interface.winset", $"Invalid element \"{controlId}\"");
                        }
                    }
                }
            } else {
                InterfaceElement element = FindElementWithName(controlId);
                MappingDataNode node = parser.Attributes();

                if (CheckParserErrors())
                    return;

                if (element == null && node.TryGet("parent", out ValueDataNode parentNode)) {
                    var parent = FindElementWithName(parentNode.Value);
                    if (parent == null) {
                        Logger.ErrorS("opendream.interface.winset", $"Attempted to create an element with nonexistent parent \"{parentNode.Value}\" ({winsetParams})");
                        return;
                    }

                    var childDescriptor = parent.ElementDescriptor.CreateChildDescriptor(_serializationManager, node);
                    parent.AddChild(childDescriptor);
                } else if (element != null) {
                    element.PopulateElementDescriptor(node, _serializationManager);
                } else {
                    Logger.ErrorS("opendream.interface.winset", $"Invalid element \"{controlId}\"");
                }
            }
        }

        private void LoadInterface(InterfaceDescriptor descriptor) {
            InterfaceDescriptor = descriptor;

            foreach (MacroSetDescriptor macroSet in descriptor.MacroSetDescriptors) {
                MacroSets.Add(macroSet.Name, new(macroSet, _entitySystemManager, _inputManager));
            }

            foreach (MenuDescriptor menuDescriptor in InterfaceDescriptor.MenuDescriptors) {
                InterfaceMenu menu = new(menuDescriptor);

                Menus.Add(menu.Name, menu);
            }

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                ControlWindow window = new ControlWindow(windowDescriptor);

                Windows.Add(windowDescriptor.Name, window);
                if (window.IsDefault) {
                    DefaultWindow = window;
                }
            }

            foreach (ControlWindow window in Windows.Values) {
                window.CreateChildControls();

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

    public interface IDreamInterfaceManager {
        (string, string, string)[] AvailableVerbs { get; }
        Dictionary<string, ControlWindow> Windows { get; }
        Dictionary<string, InterfaceMenu> Menus { get; }
        Dictionary<string, InterfaceMacroSet> MacroSets { get; }
        public ControlWindow DefaultWindow { get; }
        public ControlOutput DefaultOutput { get; }
        public ControlInfo DefaultInfo { get; }
        public ControlMap DefaultMap { get; }

        void Initialize();
        void FrameUpdate(FrameEventArgs frameEventArgs);
        InterfaceElement FindElementWithName(string name);
        void SaveScreenshot(bool openDialog);
        void LoadInterfaceFromSource(string source);
        void WinSet(string controlId, string winsetParams);
    }
}
