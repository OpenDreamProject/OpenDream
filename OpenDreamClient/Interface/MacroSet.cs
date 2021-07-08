using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using Key = Robust.Client.Input.Keyboard.Key;

namespace OpenDreamClient.Interface {
    class MacroSet {
        public string Name;
        public Dictionary<string, Macro> Macros = new();

        public MacroSet(MacroSetDescriptor macroSetDescriptor) {
            Name = macroSetDescriptor.Name;

            foreach (MacroDescriptor macroDescriptor in macroSetDescriptor.Macros) {
                Macro macro = new Macro(macroDescriptor);

                Macros.Add(macro.Name, macro);
            }
        }
    }

    class Macro : IInterfaceElement {
        public enum MacroSuffix {
            None,
            Repeat,
            Release
        }

        public Key Key;
        // TODO ROBUST: Modifiers...
        //public ModifierKeys Modifier;
        public MacroSuffix Suffix;
        public string Command;
        private ElementDescriptor _elementDescriptor;

        public Macro(MacroDescriptor macroDescriptor)
        {
            _elementDescriptor = macroDescriptor;
            UpdateElementDescriptor();
        }

        public string Name => _elementDescriptor.Name;

        ElementDescriptor IInterfaceElement.ElementDescriptor
        {
            get => _elementDescriptor;
            set => _elementDescriptor = value;
        }

        public void SetAttribute(string name, object value)
        {
            throw new NotImplementedException();
        }

        public void UpdateElementDescriptor() {
            MacroDescriptor macroDescriptor = (MacroDescriptor)_elementDescriptor;
            string macroName = macroDescriptor.Name.ToUpper();

            //if (macroName.StartsWith("SHIFT+")) Modifier = ModifierKeys.Shift;
            //else if (macroName.StartsWith("CTRL+")) Modifier = ModifierKeys.Control;
            //else if (macroName.StartsWith("ALT+")) Modifier = ModifierKeys.Alt;
            //else Modifier = ModifierKeys.None;

            if (macroName.EndsWith("+REP")) Suffix = MacroSuffix.Repeat;
            else if (macroName.EndsWith("+UP")) Suffix = MacroSuffix.Release;
            else Suffix = MacroSuffix.None;

            //Remove the modifier and suffix, if they exist
            //if (Modifier != ModifierKeys.None) macroName = macroName.Substring(macroName.IndexOf("+"));
            if (Suffix != MacroSuffix.None) macroName = macroName.Substring(0, macroName.LastIndexOf("+"));

            Key = KeyNameToKey(macroName);
            Command = macroDescriptor.Command;
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        private static Key KeyNameToKey(string keyName)
        {
            return keyName switch
            {
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
                _ => throw new Exception("Invalid key name \"" + keyName + "\"")
            };
        }
    }
}

