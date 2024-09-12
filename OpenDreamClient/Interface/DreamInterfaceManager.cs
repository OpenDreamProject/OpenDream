﻿using System.IO;
using System.Text;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using OpenDreamClient.Interface.Prompts;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using System.Linq;

namespace OpenDreamClient.Interface;

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
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITimerManager _timerManager = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.interface");

    public InterfaceDescriptor InterfaceDescriptor { get; private set; }

    public ControlWindow? DefaultWindow { get; private set; }
    public ControlOutput? DefaultOutput { get; private set; }
    public ControlInfo? DefaultInfo { get; private set; }
    public ControlMap? DefaultMap { get; private set; }

    public Dictionary<string, ControlWindow> Windows { get; } = new();
    public Dictionary<string, InterfaceMenu> Menus { get; } = new();
    public Dictionary<string, InterfaceMacroSet> MacroSets { get; } = new();

    public ViewRange View {
        get => _view;
        private set {
            // Cap to a view range of 45x45
            // Any larger causes crashes with RobustToolbox's GetPixel()
            if (value.Width > 45 || value.Height > 45)
                value = new(Math.Min(value.Width, 45), Math.Min(value.Height, 45));

            _view = value;
            DefaultMap?.UpdateViewRange(_view);
        }
    }

    public bool ShowPopupMenus { get; private set; } = true;

    private readonly Dictionary<string, BrowsePopup> _popupWindows = new();
    private ViewRange _view = new(5);

    public void LoadInterfaceFromSource(string source) {
        Reset();

        DMFLexer dmfLexer = new DMFLexer(source);
        DMFParser dmfParser = new DMFParser(dmfLexer, _serializationManager);
        InterfaceDescriptor interfaceDescriptor = dmfParser.Interface();

        if (dmfParser.Errors.Count > 0) {
            foreach (string error in dmfParser.Errors) {
                _sawmill.Error(error);
            }

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
        // Set up the middle-mouse button keybind
        _inputManager.Contexts.GetContext("common").AddFunction(OpenDreamKeyFunctions.MouseMiddle);
        _inputManager.RegisterBinding(new KeyBindingRegistration() {
            Function = OpenDreamKeyFunctions.MouseMiddle,
            BaseKey = Keyboard.Key.MouseMiddle
        });

        _netManager.RegisterNetMessage<MsgUpdateStatPanels>(RxUpdateStatPanels);
        _netManager.RegisterNetMessage<MsgSelectStatPanel>(RxSelectStatPanel);
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
        _netManager.RegisterNetMessage<MsgWinGet>(RxWinGet);
        _netManager.RegisterNetMessage<MsgFtp>(RxFtp);
        _netManager.RegisterNetMessage<MsgLoadInterface>(RxLoadInterface);
        _netManager.RegisterNetMessage<MsgAckLoadInterface>();
        _netManager.RegisterNetMessage<MsgUpdateClientInfo>(RxUpdateClientInfo);
    }

    private void RxUpdateStatPanels(MsgUpdateStatPanels message) {
        DefaultInfo?.UpdateStatPanels(message);
    }

    private void RxSelectStatPanel(MsgSelectStatPanel message) {
        DefaultInfo?.SelectStatPanel(message.StatPanel);
    }

    private void RxOutput(MsgOutput pOutput) {
        Output(pOutput.Control, pOutput.Value);
    }

    private void RxAlert(MsgAlert message) {
        OpenAlert(
            message.Title,
            message.Message,
            message.Button1, message.Button2, message.Button3,
            (responseType, response) => OnPromptFinished(message.PromptId, responseType, response));
    }

    public void OpenAlert(string title, string message, string button1, string? button2, string? button3, Action<DreamValueType, object?>? onClose) {
        var alert = new AlertWindow(
            title,
            message,
            button1, button2, button3,
            onClose);

        alert.Owner = _clyde.MainWindow;
        alert.Show();
    }

    private void RxPrompt(MsgPrompt pPrompt) {
        void OnPromptClose(DreamValueType responseType, object? response) {
            OnPromptFinished(pPrompt.PromptId, responseType, response);
        }

        Prompt(pPrompt.Types, pPrompt.Title, pPrompt.Message, pPrompt.DefaultValue, OnPromptClose);
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
                htmlFileName = $"browse{_random.Next()}"; // TODO: Possible collisions and explicit file names
                outputBrowser = FindElementWithId(pBrowse.Window) as ControlBrowser;

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
            outputBrowser.SetFileSource(cacheFile);

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
        InterfaceElement? element = FindElementWithId(message.ControlId);
        MsgPromptResponse response = new() {
            PromptId = message.PromptId,
            Type = DreamValueType.Text,
            Value = element?.Type.Value ?? string.Empty
        };

        _netManager.ClientSendMessage(response);
    }

    private void RxWinGet(MsgWinGet message) {
        // Run this later to ensure any pending UI measurements have occured
        _timerManager.AddTimer(new Timer(100, false, () => {
            MsgPromptResponse response = new() {
                PromptId = message.PromptId,
                Type = DreamValueType.Text,
                Value = WinGet(message.ControlId, message.QueryValue, forceSnowflake:true)
            };

            _netManager.ClientSendMessage(response);
        }));
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
        if (_entitySystemManager.TryGetEntitySystem(out ClientVerbSystem? verbSystem))
            DefaultInfo?.RefreshVerbs(verbSystem);
    }

    private void RxUpdateClientInfo(MsgUpdateClientInfo msg) {
        View = msg.View;
        ShowPopupMenus = msg.ShowPopupMenus;
    }

    private void ShowPrompt(PromptWindow prompt) {
        prompt.Owner = _clyde.MainWindow;
        prompt.Show();
    }

    public void FrameUpdate(FrameEventArgs frameEventArgs) {
        if (DefaultMap != null)
            DefaultMap.Viewport.Eye = _eyeManager.CurrentEye;
    }

    public InterfaceElement? FindElementWithId(string id) {
        string[] split = id.Split(".");

        if (split.Length == 2) {
            string windowId = split[0];
            string elementId = split[1];
            ControlWindow? window = null;

            if (Windows.ContainsKey(windowId)) {
                window = Windows[windowId];
            } else if (_popupWindows.TryGetValue(windowId, out var popup)) {
                window = popup.WindowElement;
            } else if (Menus.TryGetValue(windowId, out var menu)) {
                if (menu.MenuElements.TryGetValue(elementId, out var menuElement))
                    return menuElement;
            } else if(MacroSets.TryGetValue(windowId, out var macroSet)) {
                if (macroSet.Macros.TryGetValue(elementId, out var macroElement))
                    return macroElement;
            }

            if (window != null) {
                foreach (InterfaceControl element in window.ChildControls) {
                    if (element.Id.Value == elementId) return element;
                }
            }
        } else {
            string elementId = split[0];

            // ":[element]" returns the default element of that type
            switch (elementId) {
                case ":map":
                    return DefaultMap;
                case ":info":
                    return DefaultInfo;
                case ":window":
                    return DefaultWindow;
                case ":output":
                    return DefaultOutput;
            }

            foreach (ControlWindow window in Windows.Values) {
                if (window.Id.Value == elementId)
                    return window;

                foreach (InterfaceControl element in window.ChildControls) {
                    if (element.Id.Value == elementId) return element;
                }
            }

            foreach (InterfaceMenu menu in Menus.Values) {
                if (menu.Id.Value == elementId)
                    return menu;

                if (menu.MenuElements.TryGetValue(elementId, out var menuElement))
                    return menuElement;
            }

            foreach (var macroSet in MacroSets.Values) {
                if (macroSet.Id.Value == elementId)
                    return macroSet;

                if (macroSet.Macros.TryGetValue(elementId, out var macroElement))
                    return macroElement;
            }

            if (_popupWindows.TryGetValue(elementId, out var popup))
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

    public void Prompt(DreamValueType types, string title, string message, string defaultValue, Action<DreamValueType, object?>? onClose) {
        PromptWindow? prompt = null;
        bool canCancel = (types & DreamValueType.Null) == DreamValueType.Null;

        if ((types & DreamValueType.Text) == DreamValueType.Text) {
            prompt = new TextPrompt(title, message, defaultValue, canCancel, onClose);
        } else if ((types & DreamValueType.Num) == DreamValueType.Num) {
            prompt = new NumberPrompt(title, message, defaultValue, canCancel, onClose);
        } else if ((types & DreamValueType.Message) == DreamValueType.Message) {
            prompt = new MessagePrompt(title, message, defaultValue, canCancel, onClose);
        } else if ((types & DreamValueType.Color) == DreamValueType.Color) {
            prompt = new ColorPrompt(title, message, defaultValue, canCancel, onClose);
        }

        if (prompt != null) {
            ShowPrompt(prompt);
        }
    }

    public void RunCommand(string fullCommand) {
        switch (fullCommand) {
            case not null when fullCommand.StartsWith(".quit"):
                IoCManager.Resolve<IClientNetManager>().ClientDisconnect(".quit used");
                break;

            case not null when fullCommand.StartsWith(".screenshot"):
                string[] split = fullCommand.Split(" ");
                SaveScreenshot(split.Length == 1 || split[1] != "auto");
                break;

            case not null when fullCommand.StartsWith(".configure"):
                _sawmill.Warning(".configure command is not implemented");
                break;

            case not null when fullCommand.StartsWith(".winset"):
                // Everything after .winset, excluding the space and quotes
                string winsetParams = fullCommand.Substring(7); //clip .winset
                winsetParams = winsetParams.Trim(); //clip space
                winsetParams = winsetParams.Trim('\"'); //clip quotes

                WinSet(null, winsetParams);
                break;

            case not null when fullCommand.StartsWith(".output"): {
                string[] args = fullCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length != 3) {
                    _sawmill.Error($".output command was executed with {args.Length - 1} args instead of 2");
                    break;
                }

                Output(args[1], args[2]);
                break;
            }

            default: {
                string[] argsRaw = fullCommand.Split(' ', 2, StringSplitOptions.TrimEntries);
                string command = argsRaw[0].ToLowerInvariant(); // Case-insensitive

                if (!_entitySystemManager.TryGetEntitySystem(out ClientVerbSystem? verbSystem))
                    return;
                var ret = verbSystem.FindVerbWithCommandName(command);
                if (ret is not var (verbId, verbSrc, verbInfo))
                    return;

                if (argsRaw.Length == 1) { // No args given; Let the verb system handle the possible prompting
                    verbSystem.ExecuteVerb(verbSrc, verbId);
                } else { // Attempt to parse the given arguments
                    List<string> args = new List<string>();
                    StringBuilder currentArg = new();
                    bool stringCapture = false;
                    for (int i = 0; i < argsRaw[1].Length; i++) {
                        if (argsRaw[1][i] == '"') {
                            currentArg.Append('"');
                            if (stringCapture) {
                                args.Add(HandleEmbeddedWinget(null, currentArg.ToString()));
                                currentArg.Clear();
                            }

                            stringCapture = !stringCapture;
                            continue;
                        }

                        if (argsRaw[1][i] == ' ' && !stringCapture) {
                            args.Add(HandleEmbeddedWinget(null, currentArg.ToString()));
                            currentArg.Clear();
                            continue;
                        }

                        currentArg.Append(argsRaw[1][i]);
                    }

                    if(currentArg.ToString() is { } arg && !string.IsNullOrEmpty(arg))
                        args.Add(HandleEmbeddedWinget(null, arg));

                    if (args.Count != verbInfo.Arguments.Length) {
                        _sawmill.Error(
                            $"Attempted to call a verb with {verbInfo.Arguments.Length} argument(s) with only {args.Count}");
                        return;
                    }

                    var arguments = new object?[verbInfo.Arguments.Length];
                    for (int i = 0; i < verbInfo.Arguments.Length; i++) {
                        DreamValueType argumentType = verbInfo.Arguments[i].Types;

                        if (argumentType is DreamValueType.Text or DreamValueType.Message or DreamValueType.CommandText) {
                            arguments[i] = args[i];
                        } else if (argumentType == DreamValueType.Num) {
                            if (!float.TryParse(args[i], out var numArg)) {
                                _sawmill.Error(
                                    $"Invalid number argument \"{args[i]}\"; ignoring command ({fullCommand})");
                                return;
                            }

                            arguments[i] = numArg;
                        } else {
                            _sawmill.Error($"Parsing verb args of type {argumentType} is unimplemented; ignoring command ({fullCommand})");
                            return;
                        }
                    }

                    verbSystem.ExecuteVerb(verbSrc, verbId, arguments);
                }

                break;
            }
        }
    }

    public void StartRepeatingCommand(string command) {
        _netManager.ClientSendMessage(new MsgCommandRepeatStart() { Command = command });
    }

    public void StopRepeatingCommand(string command) {
        _netManager.ClientSendMessage(new MsgCommandRepeatStop() { Command = command });
    }

    public string HandleEmbeddedWinget(string? controlId, string value) {
        string result = value;
        int startPos = result.IndexOf("[[", StringComparison.Ordinal);
        while(startPos > -1){
            int endPos = result.IndexOf("]]", startPos, StringComparison.Ordinal);
            if(endPos == -1)
                break;
            string inner = result.Substring(startPos+2, endPos-startPos-2);
            string[] elementSplit = inner.Split('.');
            string innerControlId = controlId ?? "";
            if(elementSplit.Length > 1){
                innerControlId = (string.IsNullOrEmpty(innerControlId) ? "" : innerControlId+".")+string.Join(".", elementSplit[..^1]);
                inner = elementSplit[^1];
            }

            string innerResult = WinGet(innerControlId, inner);
            result = result.Substring(0, startPos) + innerResult + result.Substring(endPos+2);
            startPos = result.IndexOf("[[", StringComparison.Ordinal);
        }

        return result;
    }

    public void WinSet(string? controlId, string winsetParams) {
        DMFParser parser;
        try{
            var lexer = new DMFLexer(winsetParams);
            parser = new DMFParser(lexer, _serializationManager);
        } catch (Exception e) {
            _sawmill.Error($"Error parsing winset: {e}");
            return;
        }

        bool CheckParserErrors() {
            if (parser.Errors.Count <= 0)
                return false;

            foreach (string error in parser.Errors) {
                _sawmill.Error(error);
            }

            return true;
        }

        if (string.IsNullOrEmpty(controlId)) {
            List<DMFWinSet> winSets = parser.GlobalWinSet();

            if (CheckParserErrors())
                return;

            // id=abc overrides the elements of other winsets without an element
            string? elementOverride = winSets.FirstOrDefault(winSet => winSet.Element == null && winSet.Attribute == "id")?.Value;

            foreach (DMFWinSet winSet in winSets) {
                string? elementId = winSet.Element ?? elementOverride;

                if (elementId == null) {
                    if (winSet.Attribute == "command") {
                        RunCommand(HandleEmbeddedWinget(controlId, winSet.Value));
                    } else {
                        _sawmill.Error($"Invalid global winset \"{winsetParams}\"");
                    }
                } else {
                    if(winSet.TrueStatements is not null) {
                        InterfaceElement? conditionalElement = FindElementWithId(elementId);
                        if(conditionalElement is null)
                            _sawmill.Error($"Invalid element on ternary condition \"{elementId}\"");
                        else
                            if(conditionalElement.TryGetProperty(winSet.Attribute, out var conditionalCheckValue) && conditionalCheckValue.Equals(winSet.Value)) {
                                foreach(DMFWinSet statement in winSet.TrueStatements) {
                                    string? statementElementId = statement.Element ?? elementId;
                                    InterfaceElement? statementElement = FindElementWithId(statementElementId);
                                    if(statementElement is not null) {
                                        statementElement.SetProperty(statement.Attribute, HandleEmbeddedWinget(statementElementId, statement.Value), manualWinset: true);
                                    } else {
                                        _sawmill.Error($"Invalid element on ternary \"{statementElementId}\"");
                                    }
                                }
                            } else if (winSet.FalseStatements is not null){
                                foreach(DMFWinSet statement in winSet.FalseStatements) {
                                    string? statementElementId = statement.Element ?? elementId;
                                    InterfaceElement? statementElement = FindElementWithId(statementElementId);
                                    if(statementElement is not null) {
                                        statementElement.SetProperty(statement.Attribute, HandleEmbeddedWinget(statementElementId, statement.Value), manualWinset: true);
                                    } else {
                                        _sawmill.Error($"Invalid element on ternary \"{statementElementId}\"");
                                    }
                                }
                            }
                    } else {
                        InterfaceElement? element = FindElementWithId(elementId);

                        if (element != null) {
                            element.SetProperty(winSet.Attribute, HandleEmbeddedWinget(elementId, winSet.Value), manualWinset: true);
                        } else {
                            _sawmill.Error($"Invalid element \"{elementId}\"");
                        }
                    }
                }
            }
        } else {
            InterfaceElement? element = FindElementWithId(controlId);
            var attributes = parser.AttributesValues();

            if (CheckParserErrors())
                return;

            if (element == null && attributes.TryGetValue("parent", out var parentId)) {
                var parent = FindElementWithId(parentId);
                if (parent == null) {
                    _sawmill.Error($"Attempted to create an element with nonexistent parent \"{parentId}\" ({winsetParams})");
                    return;
                }

                attributes["id"] = controlId;
                var childDescriptor = parent.ElementDescriptor.CreateChildDescriptor(_serializationManager, attributes);
                if (childDescriptor == null)
                    return;

                parent.AddChild(childDescriptor);
            } else if (element != null) {
                foreach (var attribute in attributes) {
                    element.SetProperty(attribute.Key, attribute.Value, manualWinset: true);
                }
            } else {
                _sawmill.Error($"Invalid element \"{controlId}\"");
            }
        }
    }

    public string WinGet(string controlId, string queryValue, bool forceJson = false, bool forceSnowflake = false) {
        bool ParseAndTryGet(InterfaceElement element, string query, out string result) {
            //parse "as blah" from query if it's there
            string[] querySplit = query.Split(" as ");
            IDMFProperty propResult;
            if(querySplit.Length != 2) //must be "thing as blah" or "thing". Anything else is invalid.
                if(element.TryGetProperty(query, out propResult!)){
                    result = forceJson ? propResult.AsJson() : forceSnowflake ? propResult.AsSnowflake() : propResult.AsRaw();
                    return true;
                } else {
                    result = "";
                    return false;
                }
            else{
                if(!element.TryGetProperty(querySplit[0], out propResult!)) {
                    result = "";
                    return false;
                }

                if (forceJson) {
                    result = propResult.AsJson();
                    return true;
                } else if (forceSnowflake) {
                    result = propResult.AsSnowflake();
                    return true;
                }

                switch(querySplit[1]){
                    case "arg":
                        result = propResult.AsArg();
                        break;
                    case "escaped":
                        result = propResult.AsEscaped();
                        break;
                    case "string":
                        result = propResult.AsString();
                        break;
                    case "params":
                        result = propResult.AsParams();
                        break;
                    case "json":
                        result = propResult.AsJson();
                        break;
                    case "json-dm":
                        result = propResult.AsJsonDM();
                        break;
                    case "raw":
                        result = propResult.AsRaw();
                        break;
                    default:
                        _sawmill.Error($"Invalid winget query function \"{querySplit[1]}\" in \"{query}\"");
                        result = "";
                        return false;
                }

                return true;
            }
        }

        string GetProperty(string elementId) {
            var element = FindElementWithId(elementId);
            if (element == null) {
                _sawmill.Error($"Could not winget element {elementId} because it does not exist");
                return string.Empty;
            }

            var multiQuery = queryValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if(multiQuery.Length > 1) {
                var result = "";
                foreach(var query in multiQuery) {
                    if (!ParseAndTryGet(element, query, out var queryResult))
                        _sawmill.Error($"Could not winget property {query} on {element.Id}");
                    result += query+"="+queryResult + ";";
                }

                return result.TrimEnd(';');
            } else if (ParseAndTryGet(element, queryValue, out var value))
                return value;

            _sawmill.Error($"Could not winget property {queryValue} on {element.Id}");
            return string.Empty;
        }

        var elementIds = controlId.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (elementIds.Length == 0) {
            switch (queryValue) {
                // The server will actually never query this because it can predict the answer to be "true"
                // But also have it here in case a local winget ever wants it
                case "hwmode":
                    return "true";
                case "windows":
                    return string.Join(';',
                        Windows.Where(pair => !((WindowDescriptor)pair.Value.ElementDescriptor).IsPane.Value).Select(pair => pair.Key));
                case "panes":
                    return string.Join(';',
                        Windows.Where(pair => ((WindowDescriptor)pair.Value.ElementDescriptor).IsPane.Value).Select(pair => pair.Key));
                case "menus":
                    return string.Join(';', Menus.Keys);
                case "macros":
                    return string.Join(';', MacroSets.Keys);
                case "url":
                    return _netManager.ServerChannel?.RemoteEndPoint.ToString() ?? string.Empty; // TODO: Port should be 0 "if connected to a local .dmb file"
                default:
                    _sawmill.Error($"Special winget \"{queryValue}\" is not implemented");
                    return string.Empty;
            }
        } else if (elementIds.Length == 1) {
            return GetProperty(elementIds[0]);
        }

        var result = new StringBuilder(elementIds.Length * 6 - 1);

        for (int i = 0; i < elementIds.Length; i++) {
            var elementId = elementIds[i];

            result.Append(elementId);
            result.Append('.');
            result.Append(queryValue);
            result.Append('=');
            result.Append(GetProperty(elementId));

            if (i != elementIds.Length - 1)
                result.Append(';');
        }

        return result.ToString();
    }

    public void Output(string? control, string value) {
        InterfaceControl? interfaceElement;
        string? data = null;

        if (control != null) {
            string[] split = control.Split(":");

            interfaceElement = (InterfaceControl?)FindElementWithId(split[0]);
            if (split.Length > 1) data = split[1];
        } else {
            interfaceElement = DefaultOutput;
        }

        interfaceElement?.Output(value, data);
    }

    public void WinClone(string controlId, string cloneId) {
        ElementDescriptor? elementDescriptor = InterfaceDescriptor.GetElementDescriptor(controlId);

        elementDescriptor = elementDescriptor?.CreateCopy(_serializationManager, cloneId);

        // If window_name is "window", "pane", "menu", or "macro", and the skin file does not have a control of
        // that name already, we will create a new control of that type from scratch.
        if (elementDescriptor == null) {
            switch (controlId) {
                case "window":
                    elementDescriptor = new WindowDescriptor(cloneId);
                    break;
                case "menu":
                    elementDescriptor = new MenuDescriptor(cloneId);
                    break;
                case "macro":
                    elementDescriptor = new MacroSetDescriptor(cloneId);
                    break;
                default:
                    _sawmill.Error($"Invalid element to winclone \"{controlId}\"");
                    return;
            }
        }

        if (elementDescriptor is WindowDescriptor windowDescriptor) {
            // Cloned windows start off non-visible
            elementDescriptor = windowDescriptor.WithVisible(_serializationManager, false);
        }

        LoadDescriptor(elementDescriptor);
        if (elementDescriptor is WindowDescriptor && Windows.TryGetValue(cloneId, out var window)) {
            window.CreateChildControls();
        }
    }

    private void Reset() {
        _userInterfaceManager.MainViewport.Visible = false;
        //close windows if they're open, and clear all child uielements
        foreach (var window in Windows.Values){
            window.CloseChildWindow();
            window.UIElement.RemoveAllChildren();
        }

        Windows.Clear();
        Menus.Clear();
        MacroSets.Clear();

        //close popups if they're open
        foreach (var popup in _popupWindows.Values) {
            popup.Close();
        }

        _popupWindows.Clear();
        _inputManager.ResetAllBindings();
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

                MacroSets[macroSet.Id.Value] = macroSet;
                break;
            case MenuDescriptor menuDescriptor:
                InterfaceMenu menu = new(menuDescriptor);

                Menus.Add(menu.Id.Value, menu);
                break;
            case WindowDescriptor windowDescriptor:
                ControlWindow window = new ControlWindow(windowDescriptor);

                Windows.Add(windowDescriptor.Id.Value, window);
                if (window.IsDefault) {
                    DefaultWindow = window;
                }

                break;
        }
    }

    private void OnPromptFinished(int promptId, DreamValueType responseType, object? response) {
        var msg = new MsgPromptResponse {
            PromptId = promptId,
            Type = responseType,
            Value = response
        };

        _netManager.ClientSendMessage(msg);
    }
}

public interface IDreamInterfaceManager {
    Dictionary<string, ControlWindow> Windows { get; }
    Dictionary<string, InterfaceMenu> Menus { get; }
    Dictionary<string, InterfaceMacroSet> MacroSets { get; }
    public ControlWindow? DefaultWindow { get; }
    public ControlOutput? DefaultOutput { get; }
    public ControlInfo? DefaultInfo { get; }
    public ControlMap? DefaultMap { get; }
    public ViewRange View { get; }
    public bool ShowPopupMenus { get; }

    void Initialize();
    void FrameUpdate(FrameEventArgs frameEventArgs);
    InterfaceElement? FindElementWithId(string id);
    void SaveScreenshot(bool openDialog);
    void LoadInterfaceFromSource(string source);

    public void OpenAlert(string title, string message, string button1, string? button2, string? button3, Action<DreamValueType, object?>? onClose);
    public void Prompt(DreamValueType types, string title, string message, string defaultValue, Action<DreamValueType, object?>? onClose);
    public void RunCommand(string fullCommand);
    public void StartRepeatingCommand(string command);
    public void StopRepeatingCommand(string command);
    public void WinSet(string? controlId, string winsetParams);
    public string WinGet(string controlId, string queryValue, bool forceJson = false, bool forceSnowflake = false);
}
