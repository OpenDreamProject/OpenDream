using OpenDreamShared.Dream.Procs;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class TextPrompt : PromptWindow {
        public TextPrompt(int promptId, string title, string message) : base(promptId, title, message) { }

        protected override Control CreatePromptControl() {
            return new TextBox();
        }

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Text, ((TextBox)PromptControl).Text);
        }
    }
}
