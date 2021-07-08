/*using System.Collections.Generic;
using Robust.Client.Input;

namespace OpenDreamClient.Interface {
    class MacroHandler {
        private OpenDream _openDream;
        private Dictionary<string, MacroSet> _macroSets = new();
        private List<Macro> _repeatingMacros = new();

        public MacroHandler(OpenDream openDream) {
            _openDream = openDream;
            _openDream.ClientTick += CallRepeating;
        }

        public void ClearMacroSets() {
            _macroSets.Clear();
        }

        public void AddMacroSet( MacroSet macroSet) {
            _macroSets.Add(macroSet.Name, macroSet);
        }

        public void HandleKeyDown(object sender, KeyEventArgs e) {
            List<Macro> macros = GetMacrosWithKey(e.Key);

            foreach (Macro macro in macros) {
                if (macro.Suffix == Macro.MacroSuffix.Release) continue;
                e.Handle();

                if (macro.Suffix == Macro.MacroSuffix.Repeat) {
                    if (_repeatingMacros.Contains(macro)) continue;

                    _repeatingMacros.Add(macro);
                }

                _openDream.RunCommand(macro.Command);
            }
        }

        public void HandleKeyUp(object sender, KeyEventArgs e) {
            List<Macro> macros = GetMacrosWithKey(e.Key);

            foreach (Macro macro in macros) {
                _repeatingMacros.Remove(macro);

                e.Handle();
                if (macro.Suffix == Macro.MacroSuffix.Release) _openDream.RunCommand(macro.Command);
            }
        }

        private void CallRepeating() {
            foreach (Macro macro in _repeatingMacros) {
                _openDream.RunCommand(macro.Command);
            }
        }

        private List<Macro> GetMacrosWithKey(Keyboard.Key key) {
            List<Macro> macros = new();

            //TODO: Only check the macro set the relevant window uses, instead of all macro sets
            foreach (MacroSet macroSet in _macroSets.Values) {
                foreach (Macro macro in macroSet.Macros.Values) {
                    if (macro.Key == key) macros.Add(macro);
                }
            }

            return macros;
        }
    }
}
*/
