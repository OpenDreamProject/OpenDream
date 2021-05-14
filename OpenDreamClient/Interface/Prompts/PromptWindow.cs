using System;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Net.Packets;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class PromptWindow : Window {
        public int PromptId;
        public Control PromptControl;

        private bool _responseSent = false;

        public PromptWindow(int promptId, String title, String message, String defaultValue) {
            PromptId = promptId;

            Title = !String.IsNullOrEmpty(title) ? title : "OpenDream";

            Label messageLabel = new Label();
            messageLabel.Margin = new Thickness(5);
            messageLabel.Content = message;

            PromptControl = CreatePromptControl(defaultValue);
            PromptControl.Margin = new Thickness(5);

            Button okButton = new Button();
            okButton.Content = "Ok";
            okButton.Margin = new Thickness(5);
            okButton.IsDefault = true;
            okButton.Click += OkButton_Click;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(5);
            stackPanel.Children.Add(messageLabel);
            stackPanel.Children.Add(PromptControl);
            stackPanel.Children.Add(okButton);

            SizeToContent = SizeToContent.WidthAndHeight;
            MinWidth = 300;
            MinHeight = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Closed += PromptWindow_Closed;
            AddChild(stackPanel);

            PromptControl.Focus();
        }

        protected virtual Control CreatePromptControl(String defaultValue) {
            return new Control();
        }

        protected virtual void OkButton_Click(object sender, RoutedEventArgs e) {
            FinishPrompt(DMValueType.Null, null);
        }

        private void PromptWindow_Closed(object sender, System.EventArgs e) {
            FinishPrompt(DMValueType.Null, null);
        }

        protected void FinishPrompt(DMValueType responseType, object value) {
            if (_responseSent) return;
            _responseSent = true;

            Program.OpenDream.Connection.SendPacket(new PacketPromptResponse(PromptId, responseType, value));
            Close();
        }
    }
}
