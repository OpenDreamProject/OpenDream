using OpenDreamShared.Dream.Procs;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class MessagePrompt : PromptWindow {
        public MessagePrompt(int promptId, string title, string message) : base(promptId, title, message) { }

        protected override Control CreatePromptControl() {
            TextBox textBox = new TextBox();

            textBox.MinHeight = 100;
            textBox.MaxWidth = 500;
            textBox.MaxHeight = 400;
            textBox.AcceptsReturn = true;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            return textBox;
        }

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Message, ((TextBox)PromptControl).Text);
        }
    }
}
