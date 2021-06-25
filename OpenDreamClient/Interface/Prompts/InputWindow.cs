using OpenDreamShared.Dream.Procs;
using System;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class InputWindow : PromptWindow {
        protected Control _inputControl;

        public InputWindow(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message) {
            _inputControl = CreateInputControl(defaultValue);
            DockPanel.SetDock(_inputControl, Dock.Top);
            _dockPanel.Children.Add(_inputControl);

            CreateButton("Ok", true);
            if (canCancel) CreateButton("Cancel", false);
        }

        protected virtual Control CreateInputControl(String defaultValue) {
            return new Control();
        }

        protected override void ButtonClicked(string button) {
            if (button == "Ok") OkButtonClicked();
            else FinishPrompt(DMValueType.Null, null);

            base.ButtonClicked(button);
        }

        protected virtual void OkButtonClicked() {
            throw new NotImplementedException();
        }
    }
}
