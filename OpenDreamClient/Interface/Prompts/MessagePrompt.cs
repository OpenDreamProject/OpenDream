using OpenDreamShared.Dream.Procs;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class MessagePrompt : PromptWindow {
        public MessagePrompt(int promptId, string message) : base(promptId, message) { }

        protected override Control CreatePromptControl() {
            TextBox textBox = new TextBox();

            textBox.MinHeight = 100;
            textBox.AcceptsReturn = true;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            return textBox;
        }

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Message, ((TextBox)PromptControl).Text);
        }
    }
}
