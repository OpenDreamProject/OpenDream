using System;
using OpenDreamShared.Dream.Procs;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class TextPrompt : PromptWindow {
        public TextPrompt(int promptId, String title, String message, String defaultValue) : base(promptId, title, message, defaultValue) { }

        protected override Control CreatePromptControl(String defaultValue) {
            TextBox box = new() {
                Text = defaultValue
            };
            return box;
        }

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Text, ((TextBox)PromptControl).Text);
        }
    }
}
