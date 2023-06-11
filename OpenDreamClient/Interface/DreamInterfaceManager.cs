﻿using System.IO;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using OpenDreamClient.Interface.Prompts;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;

namespace OpenDreamClient.Interface {
    internal sealed class DreamInterfaceManager : IDreamInterfaceManager {
        private static readonly ResPath DefaultInterfaceFile = new("/OpenDream/DefaultInterface.dmf");

        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IDreamResourceManager _dreamResource = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IFileDialogManager _fileDialogManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

        public InterfaceDescriptor InterfaceDescriptor { get; private set; }

        public ControlWindow? DefaultWindow { get; private set; }
        public ControlOutput? DefaultOutput { get; private set; }
        public ControlInfo? DefaultInfo { get; private set; }
        public ControlMap? DefaultMap { get; private set; }

        public (string, string, string)[] AvailableVerbs { get; private set; } = Array.Empty<(string, string, string)>();

        public Dictionary<string, ControlWindow> Windows { get; } = new();
        public Dictionary<string, InterfaceMenu> Menus { get; } = new();
        public Dictionary<string, InterfaceMacroSet> MacroSets { get; } = new();

        private readonly Dictionary<string, BrowsePopup> _popupWindows = new();

        public void LoadInterfaceFromSource(string source) {
            DMFLexer dmfLexer = new DMFLexer("interface.dmf", source);
            DMFParser dmfParser = new DMFParser(dmfLexer, _serializationManager);

            InterfaceDescriptor? interfaceDescriptor = null;
            try {
                interfaceDescriptor = dmfParser.Interface();
            } catch (CompileErrorException) { }

            int errorCount = 0;
            foreach (CompilerEmission warning in dmfParser.Emissions) {
                if (warning.Level == ErrorLevel.Error) {
                    Logger.Error(warning.ToString());
                    errorCount++;
                } else {
                    Logger.Warning(warning.ToString());
                }
            }

            if (interfaceDescriptor == null || errorCount > 0) {
                // Open an error message that disconnects from the server once closed
                OpenAlert(
                    "Error",
                    "Encountered error(s) while parsing interface source.\nCheck the console for details.",
                    "Ok", null, null,
                    (_, _) => _client.DisconnectFromServer("Errors while parsing interface"));

                return;
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
            _netManager.RegisterNetMessage<MsgWinClone>(RxWinClone);
            _netManager.RegisterNetMessage<MsgWinExists>(RxWinExists);
            _netManager.RegisterNetMessage<MsgFtp>(RxFtp);
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
            Array.Sort(AvailableVerbs, (a, b) => string.CompareOrdinal(a.Item1, b.Item1));

            if (DefaultInfo == null)
                return; // No verb panel to show these on

            foreach (var verb in AvailableVerbs) {
                // Verb category
                if (verb.Item3 != string.Empty && !DefaultInfo.HasVerbPanel(verb.Item3)) {
                    DefaultInfo.CreateVerbPanel(verb.Item3);
                }
            }

            DefaultInfo.RefreshVerbs();
        }

        private void RxOutput(MsgOutput pOutput) {
            InterfaceControl? interfaceElement;
            string? data = null;

            if (pOutput.Control != null) {
                string[] split = pOutput.Control.Split(":");

                interfaceElement = (InterfaceControl?)FindElementWithName(split[0]);
                if (split.Length > 1) data = split[1];
            } else {
                interfaceElement = DefaultOutput;
            }

            interfaceElement?.Output(pOutput.Value, data);
        }

        private void RxAlert(MsgAlert message) {
            OpenAlert(
                message.Title,
                message.Message,
                message.Button1, message.Button2, message.Button3,
                (responseType, response) => OnPromptFinished(message.PromptId, responseType, response));
        }

        public void OpenAlert(string title, string message, string button1, string? button2, string? button3, Action<DMValueType, object?>? onClose) {
            var alert = new AlertWindow(
                title,
                message,
                button1, button2, button3,
                onClose);

            alert.Owner = _clyde.MainWindow;
            alert.Show();
        }

        private void RxPrompt(MsgPrompt pPrompt) {
            PromptWindow? prompt = null;
            bool canCancel = (pPrompt.Types & DMValueType.Null) == DMValueType.Null;

            void OnPromptClose(DMValueType responseType, object? response) {
                OnPromptFinished(pPrompt.PromptId, responseType, response);
            }

            if ((pPrompt.Types & DMValueType.Text) == DMValueType.Text) {
                prompt = new TextPrompt(pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel, OnPromptClose);
            } else if ((pPrompt.Types & DMValueType.Num) == DMValueType.Num) {
                prompt = new NumberPrompt(pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel, OnPromptClose);
            } else if ((pPrompt.Types & DMValueType.Message) == DMValueType.Message) {
                prompt = new MessagePrompt(pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, canCancel, OnPromptClose);
            }

            if (prompt != null) {
                ShowPrompt(prompt);
            }
        }

        private void RxPromptList(MsgPromptList pPromptList) {
            var prompt = new ListPrompt(
                pPromptList.Title,
                pPromptList.Message,
                pPromptList.DefaultValue,
                pPromptList.CanCancel,
                pPromptList.Values,
                (responseType, response) => OnPromptFinished(pPromptList.PromptId, responseType, response)
            );

            ShowPrompt(prompt);
        }

        private void RxBrowse(MsgBrowse pBrowse) {
            if (pBrowse.HtmlSource == null && pBrowse.Window != null) {
                //Closing a popup
                if (_popupWindows.TryGetValue(pBrowse.Window, out var popup)) {
                    popup.Close();
                }
            } else if (pBrowse.HtmlSource != null) {
                //Outputting to a browser
                string htmlFileName;
                ControlBrowser? outputBrowser;
                BrowsePopup? popup = null;

                if (pBrowse.Window != null) {
                    htmlFileName = pBrowse.Window;
                    outputBrowser = FindElementWithName(pBrowse.Window) as ControlBrowser;

                    if (outputBrowser == null) {
                        if (!_popupWindows.TryGetValue(pBrowse.Window, out popup)) {
                            // Creating a new popup
                            popup = new BrowsePopup(pBrowse.Window, pBrowse.Size, _clyde.MainWindow);
                            popup.Closed += () => { _popupWindows.Remove(pBrowse.Window); };

                            _popupWindows.Add(pBrowse.Window, popup);
                        }

                        outputBrowser = popup.Browser;
                    }
                } else {
                    //TODO: Find embedded browser panel
                    return;
                }

                var cacheFile = _dreamResource.CreateCacheFile(htmlFileName + ".html", pBrowse.HtmlSource);
                outputBrowser.SetFileSource(cacheFile, true);

                popup?.Open();
            }
        }

        private void RxWinSet(MsgWinSet message) {
            WinSet(message.ControlId, message.Params);
        }

        private void RxWinClone(MsgWinClone message) {
            WinClone(message.ControlId, message.CloneId);
        }

        private void RxWinExists(MsgWinExists message) {
            InterfaceElement? element = FindElementWithName(message.ControlId);
            MsgPromptResponse response = new() {
                PromptId = message.PromptId,
                Type = DMValueType.Text,
                Value = element?.Type ?? string.Empty
            };

            _netManager.ClientSendMessage(response);
        }

        private void RxFtp(MsgFtp message) {
            _dreamResource.LoadResourceAsync<DreamResource>(message.ResourceId, async resource => {
                // TODO: Default the filename to message.SuggestedName
                // RT doesn't seem to support this currently
                var tuple = await _fileDialogManager.SaveFile();
                if (tuple == null) // User cancelled
                    return;

                await using var file = tuple.Value.fileStream;
                resource.WriteTo(file);
            });
        }

        private void RxLoadInterface(MsgLoadInterface message) {
            string? interfaceText = message.InterfaceText;
            if (interfaceText == null) {
                if (!_resourceManager.TryContentFileRead(DefaultInterfaceFile.CanonPath, out var defaultInterface)) {
                    // Open an error message that disconnects from the server once closed
                    OpenAlert(
                        "Error",
                        "The server did not provide an interface and there is no default interface in the resources folder.",
                        "Ok", null, null,
                        (_, _) => _client.DisconnectFromServer("No interface to use"));

                    return;
                }

                using var defaultInterfaceReader = new StreamReader(defaultInterface);
                interfaceText = defaultInterfaceReader.ReadToEnd();
            }

            LoadInterfaceFromSource(interfaceText);
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

        public InterfaceElement? FindElementWithName(string name) {
            string[] split = name.Split(".");

            if (split.Length == 2) {
                string windowName = split[0];
                string elementName = split[1];
                ControlWindow? window = null;

                if (Windows.ContainsKey(windowName)) {
                    window = Windows[windowName];
                } else if (_popupWindows.TryGetValue(windowName, out var popup)) {
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

                if (MacroSets.TryGetValue(elementName, out var macroSet))
                    return macroSet;

                if (_popupWindows.TryGetValue(elementName, out var popup))
                    return popup.WindowElement;
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

        public void WinSet(string? controlId, string winsetParams) {
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

            if (string.IsNullOrEmpty(controlId)) {
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
                        InterfaceElement? element = FindElementWithName(winSet.Element);
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
                InterfaceElement? element = FindElementWithName(controlId);
                MappingDataNode node = parser.Attributes();

                if (CheckParserErrors())
                    return;

                if (element == null && node.TryGet("parent", out ValueDataNode? parentNode)) {
                    var parent = FindElementWithName(parentNode.Value);
                    if (parent == null) {
                        Logger.ErrorS("opendream.interface.winset", $"Attempted to create an element with nonexistent parent \"{parentNode.Value}\" ({winsetParams})");
                        return;
                    }

                    var childDescriptor = parent.ElementDescriptor.CreateChildDescriptor(_serializationManager, node);
                    if (childDescriptor == null)
                        return;

                    parent.AddChild(childDescriptor);
                } else if (element != null) {
                    element.PopulateElementDescriptor(node, _serializationManager);
                } else {
                    Logger.ErrorS("opendream.interface.winset", $"Invalid element \"{controlId}\"");
                }
            }
        }

        public void WinClone(string controlId, string cloneId) {
            ElementDescriptor? elementDescriptor = InterfaceDescriptor.GetElementDescriptor(controlId);

            elementDescriptor = elementDescriptor?.CreateCopy(_serializationManager, cloneId);

            // If window_name is "window", "pane", "menu", or "macro", and the skin file does not have a control of
            // that name already, we will create a new control of that type from scratch.
            if (elementDescriptor == null) {
                switch (controlId) {
                    case "window" :
                        elementDescriptor = new WindowDescriptor(cloneId);
                        break;
                    case "menu":
                        elementDescriptor = new MenuDescriptor(cloneId);
                        break;
                    case "macro":
                        elementDescriptor = new MacroSetDescriptor(cloneId);
                        break;
                    default:
                        Logger.ErrorS("opendream.interface.winclone", $"Invalid element \"{controlId}\"");
                        return;
                }
            }

            if (elementDescriptor is WindowDescriptor windowDescriptor) {
                // Cloned windows start off non-visible
                elementDescriptor = windowDescriptor.WithVisible(_serializationManager, false);
            }

            LoadDescriptor(elementDescriptor);
        }

        private void LoadInterface(InterfaceDescriptor descriptor) {
            InterfaceDescriptor = descriptor;

            foreach (MacroSetDescriptor macroSet in descriptor.MacroSetDescriptors) {
                LoadDescriptor(macroSet);
            }

            foreach (MenuDescriptor menuDescriptor in InterfaceDescriptor.MenuDescriptors) {
                LoadDescriptor(menuDescriptor);
            }

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                LoadDescriptor(windowDescriptor);
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

            if (DefaultWindow == null)
                throw new Exception("Given DMF did not have a default window");

            DefaultWindow.RegisterOnClydeWindow(_clyde.MainWindow);
            DefaultWindow.UIElement.Name = "MainWindow";

            LayoutContainer.SetAnchorRight(DefaultWindow.UIElement, 1);
            LayoutContainer.SetAnchorBottom(DefaultWindow.UIElement, 1);

            _userInterfaceManager.StateRoot.AddChild(DefaultWindow.UIElement);
        }

        private void LoadDescriptor(ElementDescriptor descriptor) {
            switch (descriptor) {
                case MacroSetDescriptor macroSetDescriptor:
                    InterfaceMacroSet macroSet = new(macroSetDescriptor, _entitySystemManager, _inputManager, _uiManager);

                    MacroSets.Add(macroSet.Name, macroSet);
                    break;
                case MenuDescriptor menuDescriptor:
                    InterfaceMenu menu = new(menuDescriptor);

                    Menus.Add(menu.Name, menu);
                    break;
                case WindowDescriptor windowDescriptor:
                    ControlWindow window = new ControlWindow(windowDescriptor);

                    Windows.Add(windowDescriptor.Name, window);
                    if (window.IsDefault) {
                        DefaultWindow = window;
                    }
                    break;
            }
        }

        private void OnPromptFinished(int promptId, DMValueType responseType, object? response) {
            var msg = new MsgPromptResponse() {
                PromptId = promptId,
                Type = responseType,
                Value = response
            };

            _netManager.ClientSendMessage(msg);
        }
    }

    public interface IDreamInterfaceManager {
        (string, string, string)[] AvailableVerbs { get; }
        Dictionary<string, ControlWindow> Windows { get; }
        Dictionary<string, InterfaceMenu> Menus { get; }
        Dictionary<string, InterfaceMacroSet> MacroSets { get; }
        public ControlWindow? DefaultWindow { get; }
        public ControlOutput? DefaultOutput { get; }
        public ControlInfo? DefaultInfo { get; }
        public ControlMap? DefaultMap { get; }

        void Initialize();
        void FrameUpdate(FrameEventArgs frameEventArgs);
        InterfaceElement? FindElementWithName(string name);
        void SaveScreenshot(bool openDialog);
        void LoadInterfaceFromSource(string source);
        void WinSet(string? controlId, string winsetParams);
    }
}
