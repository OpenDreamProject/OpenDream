using System;
using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Log;

namespace OpenDreamClient.Interface.Prompts
{
    [Virtual]
    class NumberPrompt : InputWindow {
        public NumberPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            // TODO: text input validation.
            LineEdit numberInput = new() {
                Text = defaultValue,
                VerticalAlignment = VAlignment.Top
            };
            //numberInput.PreviewTextInput += NumberInput_PreviewTextInput;

            return numberInput;
        }

        /*
        private void NumberInput_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            //Only allow numbers
            foreach (char c in e.Text) {
                if (!char.IsDigit(c)) {
                    e.Handled = true;

                    return;
                }
            }
        }
        */

        protected override void OkButtonClicked() {
            if (!float.TryParse(((LineEdit)_inputControl).Text, out float num)) {
                Logger.Error("Error while trying to convert " + ((LineEdit)_inputControl).Text + " to a number.");
            }

            FinishPrompt(DMValueType.Num, num);
        }
    }
}
