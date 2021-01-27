using OpenDreamShared.Dream.Procs;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class NumberPrompt : PromptWindow {
        public NumberPrompt(int promptId, string message) : base(promptId, message) { }

        protected override Control CreatePromptControl() {
            TextBox numberInput = new TextBox();
            numberInput.PreviewTextInput += NumberInput_PreviewTextInput;

            return numberInput;
        }

        private void NumberInput_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) {
            //Only allow numbers
            foreach (char c in e.Text) {
                if (!(c >= '0' && c <= '9')) {
                    e.Handled = true;

                    return;
                }
            }
        }

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Num, Int32.Parse(((TextBox)PromptControl).Text));
        }
    }
}
