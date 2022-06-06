using System.ComponentModel;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Network.Messages;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;

namespace OpenDreamClient.Interface.Prompts
{
    public abstract class PromptWindow : OSWindow
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        protected readonly Control InputControl;

        private BoxContainer _dockPanel;

        private int _promptId;
        private BoxContainer _buttonPanel;
        private bool _responseSent = false;

        public PromptWindow(int promptId, String title, String message)
        {
            IoCManager.InjectDependencies(this);

            _promptId = promptId;

            Title = !String.IsNullOrEmpty(title) ? title : "OpenDream";

            Label messageLabel = new Label();
            messageLabel.Margin = new Thickness(5);
            messageLabel.Text = message;

            _buttonPanel = new BoxContainer();
            _buttonPanel.Margin = new Thickness(5);
            _buttonPanel.Orientation = BoxContainer.LayoutOrientation.Horizontal;
            _buttonPanel.HorizontalAlignment = HAlignment.Right;
            _buttonPanel.VerticalAlignment = VAlignment.Bottom;

            InputControl = new Control
            {
                VerticalExpand = true
            };

            _dockPanel = new BoxContainer();
            _dockPanel.Orientation = BoxContainer.LayoutOrientation.Vertical;
            _dockPanel.Margin = new Thickness(5);
            _dockPanel.Children.Add(messageLabel);
            _dockPanel.Children.Add(InputControl);
            _dockPanel.Children.Add(_buttonPanel);

            SizeToContent = WindowSizeToContent.WidthAndHeight;
            MinWidth = 300;
            MinHeight = 150;
            StartupLocation = WindowStartupLocation.CenterOwner;
            Closing += PromptWindow_Closing;
            WindowStyles = OSWindowStyles.NoTitleOptions;

            AddChild(_dockPanel);
        }

        protected void CreateButton(string text, bool isDefault)
        {
            Button button = new Button()
            {
                Margin = new Thickness(15, 0, 0, 0),
                Children = { new Label { Text = text, Margin = new Thickness(5, 2, 5, 2) } }
                /*IsDefault = isDefault*/
            };

            button.OnPressed += _ => ButtonClicked(text);
            _buttonPanel.Children.Add(button);
        }

        protected virtual void ButtonClicked(string button)
        {
            Close();
        }

        protected void FinishPrompt(DMValueType responseType, object value)
        {
            if (_responseSent) return;
            _responseSent = true;

            var msg = new MsgPromptResponse() {
                PromptId = _promptId,
                Type = responseType,
                Value = value
            };

            _netManager.ClientSendMessage(msg);
        }

        private void PromptWindow_Closing(CancelEventArgs e)
        {
            //Don't allow closing if there hasn't been a response to the prompt
            if (!_responseSent)
            {
                e.Cancel = true;
            }
            else
            {
                Owner = null;
            }
        }
    }
}
