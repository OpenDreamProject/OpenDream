using System;
using OpenDreamShared.Dream.Procs;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class TextPrompt : InputWindow {
        public TextPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            return new TextBox {
                Text = defaultValue,
                VerticalAlignment = System.Windows.VerticalAlignment.Top
            };
        }

        protected override void OkButtonClicked() {
            FinishPrompt(DMValueType.Text, ((TextBox)_inputControl).Text);
        }
    }
}
