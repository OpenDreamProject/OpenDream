using System;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Net.Packets;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;

namespace OpenDreamClient.Interface.Prompts {
    class PromptWindow : Window {
        protected DockPanel _dockPanel;

        private int _promptId;
        private StackPanel _buttonPanel;
        private bool _responseSent = false;

        //Used for hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public PromptWindow(int promptId, String title, String message) {
            _promptId = promptId;

            Title = !String.IsNullOrEmpty(title) ? title : "OpenDream";

            Label messageLabel = new Label();
            messageLabel.Margin = new Thickness(5);
            messageLabel.Content = message;

            _buttonPanel = new StackPanel();
            _buttonPanel.Margin = new Thickness(5);
            _buttonPanel.Orientation = Orientation.Horizontal;
            _buttonPanel.HorizontalAlignment = HorizontalAlignment.Right;
            _buttonPanel.VerticalAlignment = VerticalAlignment.Bottom;

            _dockPanel = new DockPanel();
            _dockPanel.Margin = new Thickness(5);
            DockPanel.SetDock(messageLabel, Dock.Top);
            DockPanel.SetDock(_buttonPanel, Dock.Bottom);
            _dockPanel.Children.Add(messageLabel);
            _dockPanel.Children.Add(_buttonPanel);

            SizeToContent = SizeToContent.WidthAndHeight;
            MinWidth = 300;
            MinHeight = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Closing += PromptWindow_Closing;

            AddChild(_dockPanel);
        }

        public new void Show() {
            base.Show();

            //Hide the close button
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        protected void CreateButton(string text, bool isDefault) {
            Button button = new Button() {
                Content = text,
                Margin = new Thickness(15, 0, 0, 0),
                Padding = new Thickness(5, 2, 5, 2),
                IsDefault = isDefault
            };

            button.Click += (object sender, RoutedEventArgs e) => ButtonClicked(text);
            _buttonPanel.Children.Add(button);
        }

        protected virtual void ButtonClicked(string button) {
            Close();
        }

        protected void FinishPrompt(DMValueType responseType, object value) {
            if (_responseSent) return;
            _responseSent = true;

            Program.OpenDream.Connection.SendPacket(new PacketPromptResponse(_promptId, responseType, value));
        }

        private void PromptWindow_Closing(object sender, CancelEventArgs e) {
            //Don't allow closing if there hasn't been a response to the prompt
            if (!_responseSent) {
                e.Cancel = true;
            } else {
                Owner = null;
            }
        }
    }
}
