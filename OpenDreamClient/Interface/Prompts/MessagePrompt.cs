using System;
using OpenDreamShared.Dream.Procs;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class MessagePrompt : PromptWindow {
        public MessagePrompt(int promptId, String title, String message, String defaultValue) : base(promptId, title, message, defaultValue) { }

        protected override Control CreatePromptControl(String defaultValue) {
            TextBox textBox = new() {
                MinHeight = 100,
                MaxWidth = 500,
                MaxHeight = 400,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = defaultValue,
            };
            return textBox;
        }

        protected override void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Message, ((TextBox)PromptControl).Text);
        }
    }
}
