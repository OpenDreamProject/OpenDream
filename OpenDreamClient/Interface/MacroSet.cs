/*using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Windows.Input;

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

    class Macro : InterfaceElement {
        public enum MacroSuffix {
            None,
            Repeat,
            Release
        }

        public Key Key;
        public ModifierKeys Modifier;
        public MacroSuffix Suffix;
        public string Command;

        public Macro(MacroDescriptor macroDescriptor) : base(macroDescriptor) {
            UpdateElementDescriptor();
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            MacroDescriptor macroDescriptor = (MacroDescriptor)_elementDescriptor;
            string macroName = macroDescriptor.Name.ToUpper();

            if (macroName.StartsWith("SHIFT+")) Modifier = ModifierKeys.Shift;
            else if (macroName.StartsWith("CTRL+")) Modifier = ModifierKeys.Control;
            else if (macroName.StartsWith("ALT+")) Modifier = ModifierKeys.Alt;
            else Modifier = ModifierKeys.None;

            if (macroName.EndsWith("+REP")) Suffix = MacroSuffix.Repeat;
            else if (macroName.EndsWith("+UP")) Suffix = MacroSuffix.Release;
            else Suffix = MacroSuffix.None;

            //Remove the modifier and suffix, if they exist
            if (Modifier != ModifierKeys.None) macroName = macroName.Substring(macroName.IndexOf("+"));
            if (Suffix != MacroSuffix.None) macroName = macroName.Substring(0, macroName.LastIndexOf("+"));

            Key = KeyNameToKey(macroName);
            Command = macroDescriptor.Command;
        }

        private static Key KeyNameToKey(string keyName) {
            if (keyName.Length == 1) {
                char c = keyName[0];

                if (c >= 'A' && c <= 'Z') {
                    return (Key)(c - 21); //I'm not typing all these out individually
                } else if (c >= '0' && c <= '9') {
                    return (Key)(c - 14); //Same here
                }
            } else {
                switch (keyName) {
                    case "NUMPAD0": return Key.NumPad0;
                    case "NUMPAD1": return Key.NumPad1;
                    case "NUMPAD2": return Key.NumPad2;
                    case "NUMPAD3": return Key.NumPad3;
                    case "NUMPAD4": return Key.NumPad4;
                    case "NUMPAD5": return Key.NumPad5;
                    case "NUMPAD6": return Key.NumPad6;
                    case "NUMPAD7": return Key.NumPad7;
                    case "NUMPAD8": return Key.NumPad8;
                    case "NUMPAD9": return Key.NumPad9;
                    case "NORTH": return Key.Up;
                    case "SOUTH": return Key.Down;
                    case "EAST": return Key.Right;
                    case "WEST": return Key.Left;
                    case "NORTHWEST": return Key.Home;
                    case "SOUTHWEST": return Key.End;
                    case "NORTHEAST": return Key.PageUp;
                    case "SOUTHEAST": return Key.PageDown;
                    case "CENTER": return Key.Clear;
                    case "RETURN": return Key.Enter;
                    case "ESCAPE": return Key.Escape;
                    case "TAB": return Key.Tab;
                    case "SPACE": return Key.Space;
                    case "BACK": return Key.Back;
                    case "INSERT": return Key.Insert;
                    case "DELETE": return Key.Delete;
                    case "PAUSE": return Key.Pause;
                    case "SNAPSHOT": return Key.PrintScreen;
                    case "LWIN": return Key.LWin;
                    case "RWIN": return Key.RWin;
                    case "APPS": return Key.Apps;
                    case "MULTIPLY": return Key.Multiply;
                    case "ADD": return Key.Add;
                    case "SUBTRACT": return Key.Subtract;
                    case "DIVIDE": return Key.Divide;

                    //TODO: Right shift/ctrl/alt
                    case "SHIFT": return Key.LeftShift;
                    case "CTRL": return Key.LeftCtrl;
                    case "ALT": return Key.LeftAlt;
                }
            }

            throw new Exception("Invalid key name \"" + keyName + "\"");
        }
    }
}
*/
