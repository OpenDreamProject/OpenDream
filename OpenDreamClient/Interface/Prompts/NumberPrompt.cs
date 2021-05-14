using OpenDreamShared.Dream.Procs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenDreamClient.Interface.Prompts {
    class NumberPrompt : PromptWindow {
        public NumberPrompt(int promptId, String title, String message, String defaultValue) : base(promptId, title, message, defaultValue) { }

        protected override Control CreatePromptControl(String defaultValue) {
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

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            if (!Int32.TryParse(((TextBox)PromptControl).Text, out Int32 num)) {
                Console.Error.WriteLine("Error while trying to convert " + ((TextBox)PromptControl).Text + " to a number.");
            }
            FinishPrompt(DMValueType.Num, num);
        }
    }
}
