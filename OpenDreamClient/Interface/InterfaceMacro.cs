using JetBrains.Annotations;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Input;
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

    private readonly string _inputContextName;

    public InterfaceMacroSet(MacroSetDescriptor descriptor, IEntitySystemManager entitySystemManager, IInputManager inputManager) : base(descriptor) {
        _inputManager = inputManager;
        _entitySystemManager = entitySystemManager;

        _inputContextName = $"{InputContextPrefix}{Name}";
        if (inputManager.Contexts.Exists(_inputContextName)) {
            inputManager.Contexts.Remove(_inputContextName);
        }

        _inputContext = inputManager.Contexts.New(_inputContextName, "common");
        foreach (MacroDescriptor macro in descriptor.Macros) {
            AddChild(macro);
        }
    }

    public override void AddChild(ElementDescriptor descriptor) {
        if (descriptor is not MacroDescriptor macroDescriptor)
            throw new ArgumentException($"Attempted to add a {descriptor} to a macro set", nameof(descriptor));
        Macros[macroDescriptor.Name] = new InterfaceMacro(_inputContextName, macroDescriptor, _entitySystemManager, _inputManager, _inputContext);
    }

    public void SetActive() {
        _inputManager.Contexts.SetActiveContext($"{InputContextPrefix}{Name}");
    }
}

public sealed class InterfaceMacro : InterfaceElement {
    public string Id => MacroDescriptor.Id;
    public string Command => MacroDescriptor.Id;

    private MacroDescriptor MacroDescriptor => (ElementDescriptor as MacroDescriptor);

    private readonly IEntitySystemManager _entitySystemManager;

    private readonly bool _isRepeating;
    private readonly bool _isRelease;

    public InterfaceMacro(string contextName, MacroDescriptor descriptor, IEntitySystemManager entitySystemManager, IInputManager inputManager, IInputCmdContext inputContext) : base(descriptor) {
        _entitySystemManager = entitySystemManager;

        BoundKeyFunction function = new BoundKeyFunction($"{contextName}_{Id}");
        KeyBindingRegistration binding = CreateMacroBinding(function, Name.ToUpperInvariant());

        if (binding == null)
            return;

        _isRepeating = Name.Contains("+REP");
        _isRelease = Name.Contains("+UP");

        if (_isRepeating && _isRelease)
            throw new Exception("A macro cannot be both +REP and +UP");

        inputContext.AddFunction(function);
        inputManager.RegisterBinding(in binding);
        inputManager.SetInputCommand(function, InputCmdHandler.FromDelegate(OnMacroPress, OnMacroRelease, outsidePrediction: false));
    }

    private void OnMacroPress([CanBeNull] ICommonSession session) {
        if (String.IsNullOrEmpty(Command))
            return;
        if (_isRelease)
            return;

        if (_entitySystemManager.TryGetEntitySystem(out DreamCommandSystem commandSystem)) {
            if (_isRepeating) {
                commandSystem.StartRepeatingCommand(Command);
            } else {
                commandSystem.RunCommand(Command);
            }
        }
    }

    private void OnMacroRelease([CanBeNull] ICommonSession session) {
        if (String.IsNullOrEmpty(Command))
            return;

        if (_entitySystemManager.TryGetEntitySystem(out DreamCommandSystem commandSystem)) {
            if (_isRepeating) {
                commandSystem.StopRepeatingCommand(Command);
            } else if (_isRelease) {
                commandSystem.RunCommand(Command);
            }
        }
    }

    private static KeyBindingRegistration CreateMacroBinding(BoundKeyFunction function, string macroName) {
        macroName = macroName.Replace("+UP", String.Empty);
        macroName = macroName.Replace("+REP", String.Empty);
        macroName = macroName.Replace("SHIFT+", String.Empty);
        macroName = macroName.Replace("CTRL+", String.Empty);
        macroName = macroName.Replace("ALT+", String.Empty);

        //TODO: modifiers
        var key = KeyNameToKey(macroName);
        if (key == Key.Unknown) {
            Logger.Warning($"Unknown key: {macroName}");
            return null;
        }

        return new KeyBindingRegistration() {
            BaseKey = key,
            Function = function
        };
    }

    private static Key KeyNameToKey(string keyName) {
        return keyName.ToUpper() switch {
            "A" => Key.A,
            "B" => Key.B,
            "C" => Key.C,
            "D" => Key.D,
            "E" => Key.E,
            "F" => Key.F,
            "G" => Key.G,
            "H" => Key.H,
            "I" => Key.I,
            "J" => Key.J,
            "K" => Key.K,
            "L" => Key.L,
            "M" => Key.M,
            "N" => Key.N,
            "O" => Key.O,
            "P" => Key.P,
            "Q" => Key.Q,
            "R" => Key.R,
            "S" => Key.S,
            "T" => Key.T,
            "U" => Key.U,
            "V" => Key.V,
            "W" => Key.W,
            "X" => Key.X,
            "Y" => Key.Y,
            "Z" => Key.Z,
            "0" => Key.Num0,
            "1" => Key.Num1,
            "2" => Key.Num2,
            "3" => Key.Num3,
            "4" => Key.Num4,
            "5" => Key.Num5,
            "6" => Key.Num6,
            "7" => Key.Num7,
            "8" => Key.Num8,
            "9" => Key.Num9,
            "F1" => Key.F1,
            "F2" => Key.F2,
            "F3" => Key.F3,
            "F4" => Key.F4,
            "F5" => Key.F5,
            "F6" => Key.F6,
            "F7" => Key.F7,
            "F8" => Key.F8,
            "F9" => Key.F9,
            "F10" => Key.F10,
            "F11" => Key.F11,
            "F12" => Key.F12,
            "F13" => Key.F13,
            "F14" => Key.F14,
            "F15" => Key.F15,
            "NUMPAD0" => Key.NumpadNum0,
            "NUMPAD1" => Key.NumpadNum1,
            "NUMPAD2" => Key.NumpadNum2,
            "NUMPAD3" => Key.NumpadNum3,
            "NUMPAD4" => Key.NumpadNum4,
            "NUMPAD5" => Key.NumpadNum5,
            "NUMPAD6" => Key.NumpadNum6,
            "NUMPAD7" => Key.NumpadNum7,
            "NUMPAD8" => Key.NumpadNum8,
            "NUMPAD9" => Key.NumpadNum9,
            "NORTH" => Key.Up,
            "SOUTH" => Key.Down,
            "EAST" => Key.Right,
            "WEST" => Key.Left,
            "NORTHWEST" => Key.Home,
            "SOUTHWEST" => Key.End,
            "NORTHEAST" => Key.PageUp,
            "SOUTHEAST" => Key.PageDown,
            //"CENTER" => Key.Clear,
            "RETURN" => Key.Return,
            "ESCAPE" => Key.Escape,
            "TAB" => Key.Tab,
            "SPACE" => Key.Space,
            "BACK" => Key.BackSpace,
            "INSERT" => Key.Insert,
            "DELETE" => Key.Delete,
            "PAUSE" => Key.Pause,
            //"SNAPSHOT" => Key.PrintScreen,
            "LWIN" => Key.LSystem,
            "RWIN" => Key.RSystem,
            //"APPS" => Key.Apps,
            "MULTIPLY" => Key.NumpadMultiply,
            "ADD" => Key.NumpadAdd,
            "SUBTRACT" => Key.NumpadSubtract,
            "DIVIDE" => Key.NumpadDivide,
            //TODO: Right shift/ctrl/alt
            "SHIFT" => Key.Shift,
            "CTRL" => Key.Control,
            "ALT" => Key.Alt,
            _ => Key.Unknown
        };
    }
}
