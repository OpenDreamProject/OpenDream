using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts
{
    [Virtual]
    class NumberPrompt : InputWindow {
        private readonly LineEdit _numberInput;

        public NumberPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(
            promptId, title, message, defaultValue, canCancel) {
            _numberInput = new() {
                Text = defaultValue,
                VerticalAlignment = VAlignment.Top,
                IsValid = static str => float.TryParse(str, out float _),
            };

            SetPromptControl(_numberInput);
        }

        protected override void OkButtonClicked() {
            if (!float.TryParse(_numberInput.Text, out float num)) {
                Logger.Error($"Error while trying to convert {_numberInput.Text} to a number.");
            }

            FinishPrompt(DMValueType.Num, num);
        }
    }
}
