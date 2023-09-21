using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;
using Key = Robust.Client.Input.Keyboard.Key;

namespace OpenDreamClient.Interface;

public sealed class InterfaceMacroSet : InterfaceElement {
    public readonly Dictionary<string, InterfaceMacro> Macros = new();

    private const string InputContextPrefix = "macroSet_";

    private readonly IInputManager _inputManager;
    private readonly IEntitySystemManager _entitySystemManager;
    private readonly IInputCmdContext _inputContext;
    private readonly IUserInterfaceManager _uiManager;

    private readonly string _inputContextName;

    public InterfaceMacroSet(MacroSetDescriptor descriptor, IEntitySystemManager entitySystemManager, IInputManager inputManager, IUserInterfaceManager uiManager) : base(descriptor) {
        _inputManager = inputManager;
        _entitySystemManager = entitySystemManager;
        _uiManager = uiManager;

        _inputContextName = $"{InputContextPrefix}{ElementDescriptor.Name}";
        if (inputManager.Contexts.TryGetContext(_inputContextName, out var existingContext)) {
            _inputContext = existingContext;
        } else {
            _inputContext = inputManager.Contexts.New(_inputContextName, "common");
        }

        foreach (MacroDescriptor macro in descriptor.Macros) {
            AddChild(macro);
        }
    }

    public override void AddChild(ElementDescriptor descriptor) {
        if (descriptor is not MacroDescriptor macroDescriptor)
            throw new ArgumentException($"Attempted to add a {descriptor} to a macro set", nameof(descriptor));

        Macros.Add(macroDescriptor.Id, new InterfaceMacro(_inputContextName, macroDescriptor, _entitySystemManager, _inputManager, _inputContext, _uiManager));
    }

    public void SetActive() {
        _inputManager.Contexts.SetActiveContext($"{InputContextPrefix}{ElementDescriptor.Name}");
    }
}

internal struct ParsedKeybind {
    public bool Up;
    public bool Rep;
    public bool Shift;
    public bool Ctrl;
    public bool Alt;
    public bool IsAny;
    public Key? Key;

    private static Dictionary<string, Key> keyNameToKey = new Dictionary<string, Key>() {
        {"A", Keyboard.Key.A},
        {"B", Keyboard.Key.B},
        {"C", Keyboard.Key.C},
        {"D", Keyboard.Key.D},
        {"E", Keyboard.Key.E},
        {"F", Keyboard.Key.F},
        {"G", Keyboard.Key.G},
        {"H", Keyboard.Key.H},
        {"I", Keyboard.Key.I},
        {"J", Keyboard.Key.J},
        {"K", Keyboard.Key.K},
        {"L", Keyboard.Key.L},
        {"M", Keyboard.Key.M},
        {"N", Keyboard.Key.N},
        {"O", Keyboard.Key.O},
        {"P", Keyboard.Key.P},
        {"Q", Keyboard.Key.Q},
        {"R", Keyboard.Key.R},
        {"S", Keyboard.Key.S},
        {"T", Keyboard.Key.T},
        {"U", Keyboard.Key.U},
        {"V", Keyboard.Key.V},
        {"W", Keyboard.Key.W},
        {"X", Keyboard.Key.X},
        {"Y", Keyboard.Key.Y},
        {"Z", Keyboard.Key.Z},
        {"0", Keyboard.Key.Num0},
        {"1", Keyboard.Key.Num1},
        {"2", Keyboard.Key.Num2},
        {"3", Keyboard.Key.Num3},
        {"4", Keyboard.Key.Num4},
        {"5", Keyboard.Key.Num5},
        {"6", Keyboard.Key.Num6},
        {"7", Keyboard.Key.Num7},
        {"8", Keyboard.Key.Num8},
        {"9", Keyboard.Key.Num9},
        {"F1", Keyboard.Key.F1},
        {"F2", Keyboard.Key.F2},
        {"F3", Keyboard.Key.F3},
        {"F4", Keyboard.Key.F4},
        {"F5", Keyboard.Key.F5},
        {"F6", Keyboard.Key.F6},
        {"F7", Keyboard.Key.F7},
        {"F8", Keyboard.Key.F8},
        {"F9", Keyboard.Key.F9},
        {"F10", Keyboard.Key.F10},
        {"F11", Keyboard.Key.F11},
        {"F12", Keyboard.Key.F12},
        {"F13", Keyboard.Key.F13},
        {"F14", Keyboard.Key.F14},
        {"F15", Keyboard.Key.F15},
        {"NUMPAD0", Keyboard.Key.NumpadNum0},
        {"NUMPAD1", Keyboard.Key.NumpadNum1},
        {"NUMPAD2", Keyboard.Key.NumpadNum2},
        {"NUMPAD3", Keyboard.Key.NumpadNum3},
        {"NUMPAD4", Keyboard.Key.NumpadNum4},
        {"NUMPAD5", Keyboard.Key.NumpadNum5},
        {"NUMPAD6", Keyboard.Key.NumpadNum6},
        {"NUMPAD7", Keyboard.Key.NumpadNum7},
        {"NUMPAD8", Keyboard.Key.NumpadNum8},
        {"NUMPAD9", Keyboard.Key.NumpadNum9},
        {"NORTH", Keyboard.Key.Up},
        {"SOUTH", Keyboard.Key.Down},
        {"EAST", Keyboard.Key.Right},
        {"WEST", Keyboard.Key.Left},
        {"NORTHWEST", Keyboard.Key.Home},
        {"SOUTHWEST", Keyboard.Key.End},
        {"NORTHEAST", Keyboard.Key.PageUp},
        {"SOUTHEAST", Keyboard.Key.PageDown},
        //{"CENTER", Keyboard.Key.Clear},
        {"RETURN", Keyboard.Key.Return},
        {"ESCAPE", Keyboard.Key.Escape},
        {"TAB", Keyboard.Key.Tab},
        {"SPACE", Keyboard.Key.Space},
        {"BACK", Keyboard.Key.BackSpace},
        {"INSERT", Keyboard.Key.Insert},
        {"DELETE", Keyboard.Key.Delete},
        {"PAUSE", Keyboard.Key.Pause},
        //{"SNAPSHOT", Keyboard.Key.PrintScreen},
        {"LWIN", Keyboard.Key.LSystem},
        {"RWIN", Keyboard.Key.RSystem},
        //{"APPS", Keyboard.Key.Apps},
        {"MULTIPLY", Keyboard.Key.NumpadMultiply},
        {"ADD", Keyboard.Key.NumpadAdd},
        {"SUBTRACT", Keyboard.Key.NumpadSubtract},
        {"DIVIDE", Keyboard.Key.NumpadDivide},
        {";", Keyboard.Key.SemiColon}, // undocumented but works in BYOND
        //TODO: Right shift/ctrl/alt
        {"SHIFT", Keyboard.Key.Shift},
        {"CTRL", Keyboard.Key.Control},
        {"ALT", Keyboard.Key.Alt},
    };

    private static Dictionary<Key, string>? keyToKeyName;

    public static Key KeyNameToKey(string key) {
        if (keyNameToKey.TryGetValue(key, out Key result)) {
            return result;
        } else {
            return Keyboard.Key.Unknown;
        }
    }

    public static string? KeyToKeyName(Key key) {
        if (keyToKeyName == null) {
            keyToKeyName = new Dictionary<Key, string>();
            foreach (KeyValuePair<string, Key> entry in keyNameToKey) {
                keyToKeyName[entry.Value] = entry.Key;
            }
        }

        if (keyToKeyName.TryGetValue(key, out var result)) {
            return result;
        } else {
            return null;
        }
    }

    public static ParsedKeybind Parse(string keybind) {
        ParsedKeybind parsed = new ParsedKeybind();

        bool foundKey = false;
        string[] parts = keybind.ToUpperInvariant().Split('+');
        foreach (string part in parts) {
            switch (part) {
                case "UP":
                    parsed.Up = true;
                    break;
                case "REP":
                    parsed.Rep = true;
                    break;
                case "SHIFT":
                    parsed.Shift = true;
                    break;
                case "CTRL":
                    parsed.Ctrl = true;
                    break;
                case "ALT":
                    parsed.Alt = true;
                    break;
                default:
                    if (part == "ANY") {
                        parsed.IsAny = true;
                    } else {
                        parsed.Key = KeyNameToKey(part);
                        if (parsed.Key == Keyboard.Key.Unknown) {
                            throw new Exception($"Invalid keybind part: {part}");
                        }
                    }

                    if (foundKey) {
                        throw new Exception($"Duplicate key in keybind: {part}");
                    }
                    foundKey = true;
                    break;
            }
        }

        return parsed;
    }
}

public sealed class InterfaceMacro : InterfaceElement {
    public string Command => MacroDescriptor.Command;
    
    private MacroDescriptor MacroDescriptor => (MacroDescriptor)ElementDescriptor;

    private readonly IEntitySystemManager _entitySystemManager;
    private readonly IUserInterfaceManager _uiManager;
    private readonly IInputCmdContext _inputContext;
    private readonly IInputManager _inputManager;

    private readonly bool _isRepeating;
    private readonly bool _isRelease;
    private readonly bool _isAny;

    public InterfaceMacro(string contextName, MacroDescriptor descriptor, IEntitySystemManager entitySystemManager, IInputManager inputManager, IInputCmdContext inputContext, IUserInterfaceManager uiManager) : base(descriptor) {
        _entitySystemManager = entitySystemManager;
        _uiManager = uiManager;
        _inputContext = inputContext;
        _inputManager = inputManager;

        BoundKeyFunction function = new BoundKeyFunction($"{contextName}_{Id}");
        ParsedKeybind parsedKeybind;

        try {
            parsedKeybind = ParsedKeybind.Parse(ElementDescriptor.Name);
        } catch (Exception e) {
            Logger.GetSawmill("opendream.macro").Warning($"Invalid keybind for macro {Id}: {e.Message}");
            return;
        }

        _isRepeating = parsedKeybind.Rep;
        _isRelease = parsedKeybind.Up;
        _isAny = parsedKeybind.IsAny;

        if (_isAny && (parsedKeybind.Shift || parsedKeybind.Ctrl || parsedKeybind.Alt || parsedKeybind.Rep))
            throw new Exception("ANY can only be combined with the +UP modifier");

        if (_isRepeating && _isRelease)
            throw new Exception("A macro cannot be both +REP and +UP");

        if (_isAny) {
            inputManager.FirstChanceOnKeyEvent += FirstChanceKeyHandler;
            return;
        }

        KeyBindingRegistration? binding = CreateMacroBinding(function, parsedKeybind);

        if (binding == null)
            return;

        inputContext.AddFunction(function);
        inputManager.RegisterBinding(in binding);
        inputManager.SetInputCommand(function, InputCmdHandler.FromDelegate(OnMacroPress, OnMacroRelease, outsidePrediction: false));
    }

    private void FirstChanceKeyHandler(KeyEventArgs args, KeyEventType type) {
        if (_inputManager.Contexts.ActiveContext != _inputContext) // don't trigger macro if we're not in the right context / macro set
            return;
        if (!_isAny) // this is where we handle only the ANY macros
            return;
        if ((type != KeyEventType.Up && _isRelease) || (type != KeyEventType.Down && !_isRelease))
            return;
        if (string.IsNullOrEmpty(Command))
            return;
        if (_uiManager.KeyboardFocused != null) {
            // don't trigger  macros if we're typing somewhere
            // Ideally this would be way more robust and would instead go through the RT keybind pipeline, most importantly passing through control.KeyBindDown.
            // However, currently it all seems to be internal, protected or protected internal so no luck.
            return;
        }

        if (_entitySystemManager.TryGetEntitySystem(out DreamCommandSystem? commandSystem)) {
            string? keyName = ParsedKeybind.KeyToKeyName(args.Key);
            if (keyName == null)
                return;
            string command = Command.Replace("[[*]]", keyName);
            commandSystem.RunCommand(command);
            // args.Handle() omitted on purpose, in BYOND both the "specific" keybind and the ANY keybind are triggered
        }
    }

    private void OnMacroPress(ICommonSession? session) {
        if (string.IsNullOrEmpty(Command))
            return;
        if (_isRelease)
            return;

        if (_entitySystemManager.TryGetEntitySystem(out DreamCommandSystem? commandSystem)) {
            if (_isRepeating) {
                commandSystem.StartRepeatingCommand(Command);
            } else {
                commandSystem.RunCommand(Command);
            }
        }
    }

    private void OnMacroRelease(ICommonSession? session) {
        if (string.IsNullOrEmpty(Command))
            return;

        if (_entitySystemManager.TryGetEntitySystem(out DreamCommandSystem? commandSystem)) {
            if (_isRepeating) {
                commandSystem.StopRepeatingCommand(Command);
            } else if (_isRelease) {
                commandSystem.RunCommand(Command);
            }
        }
    }

    private static KeyBindingRegistration? CreateMacroBinding(BoundKeyFunction function, ParsedKeybind keybind) {
        if (keybind.Key == null) {
            Logger.GetSawmill("opendream.macro").Warning($"Invalid keybind: {keybind}");
            return null;
        }

        return new KeyBindingRegistration() {
            BaseKey = keybind.Key.Value,
            Function = function,
            Mod1 = keybind.Shift ? Key.Shift : Key.Unknown,
            Mod2 = keybind.Ctrl ? Key.Control : Key.Unknown,
            Mod3 = keybind.Alt ? Key.Alt : Key.Unknown,
        };
    }
}
