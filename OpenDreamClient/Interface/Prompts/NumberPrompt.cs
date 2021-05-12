using OpenDreamShared.Dream.Procs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenDreamClient.Interface.Prompts {
    class NumberPrompt : PromptWindow {
        public NumberPrompt(int promptId, string title, string message) : base(promptId, title, message) { }

        protected override Control CreatePromptControl() {
            TextBox numberInput = new TextBox();
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

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Num, Int32.Parse(((TextBox)PromptControl).Text));
        }
    }
}
