using System;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Net.Packets;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class AlertWindow : Window {
        public int PromptId;

        private bool _responseSent = false;

        public AlertWindow(int promptId, String title, String message, String button1, String button2, String button3) {
            PromptId = promptId;

            Title = !String.IsNullOrEmpty(title) ? title : "OpenDream";

            Label messageLabel = new Label();
            messageLabel.Margin = new Thickness(5);
            messageLabel.Content = message;

            StackPanel buttonPanel = new StackPanel();
            buttonPanel.Margin = new Thickness(5);
            buttonPanel.Orientation = Orientation.Horizontal;
            buttonPanel.HorizontalAlignment = HorizontalAlignment.Right;
            buttonPanel.VerticalAlignment = VerticalAlignment.Bottom;

            buttonPanel.Children.Add(CreateButton(button1, true));
            if (!String.IsNullOrEmpty(button2)) buttonPanel.Children.Add(CreateButton(button2, false));
            if (!String.IsNullOrEmpty(button3)) buttonPanel.Children.Add(CreateButton(button3, false));

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(messageLabel);
            stackPanel.Children.Add(buttonPanel);

            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Closing += PromptWindow_Closing;
            AddChild(stackPanel);
        }

        protected virtual void ButtonClicked(string button) {
            FinishPrompt(DMValueType.Text, button);
        }

        private void PromptWindow_Closing(object sender, EventArgs e) {
            Owner = null;
            FinishPrompt(DMValueType.Null, null);
        }

        protected void FinishPrompt(DMValueType responseType, object value) {
            if (_responseSent) return;
            _responseSent = true;

            Program.OpenDream.Connection.SendPacket(new PacketPromptResponse(PromptId, responseType, value));
            Close();
        }

        private Button CreateButton(string text, bool isDefault) {
            Button button = new Button() {
                Content = text,
                Margin = new Thickness(15, 0, 0, 0),
                Padding = new Thickness(5, 2, 5, 2),
                IsDefault = isDefault
            };

            button.Click += (object sender, RoutedEventArgs e) => ButtonClicked(text);
            return button;
        }
    }
}
