using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts
{
    [Virtual]
    class NumberPrompt : InputWindow {
        public NumberPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            LineEdit numberInput = new() {
                Text = defaultValue,
                VerticalAlignment = VAlignment.Top,
                IsValid = static str => float.TryParse(str, out float _),
            };
            return numberInput;
        }

        protected override void OkButtonClicked() {
            if (!float.TryParse(((LineEdit)_inputControl).Text, out float num)) {
                Logger.Error("Error while trying to convert " + ((LineEdit)_inputControl).Text + " to a number.");
            }

            FinishPrompt(DMValueType.Num, num);
        }
    }
}
