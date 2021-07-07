/*using OpenDreamShared.Dream.Procs;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenDreamClient.Interface.Prompts {
    class NumberPrompt : InputWindow {
        public NumberPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            TextBox numberInput = new() {
                Text = defaultValue
            };
            numberInput.PreviewTextInput += NumberInput_PreviewTextInput;

            return numberInput;
        }

        private void NumberInput_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            //Only allow numbers
            foreach (char c in e.Text) {
                if (!char.IsDigit(c)) {
                    e.Handled = true;

                    return;
                }
            }
        }

        protected override void OkButtonClicked() {
            if (!Int32.TryParse(((TextBox)_inputControl).Text, out Int32 num)) {
                Console.Error.WriteLine("Error while trying to convert " + ((TextBox)_inputControl).Text + " to a number.");
            }

            FinishPrompt(DMValueType.Num, num);
        }
    }
}
*/
