using OpenDreamShared.Dream;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts;

internal sealed class NumberPrompt : InputWindow {
    private readonly LineEdit _numberInput;

    public NumberPrompt(string title, string message, string defaultValue, bool canCancel,
        Action<DMValueType, object?>? onClose) : base(title, message, canCancel, onClose) {
        _numberInput = new() {
            Text = defaultValue,
            VerticalAlignment = VAlignment.Top,
            IsValid = static str => float.TryParse(str, out float _),
        };

        _numberInput.OnTextEntered += NumberInput_TextEntered;
        SetPromptControl(_numberInput);
    }

    protected override void OkButtonClicked() {
        if (!float.TryParse(_numberInput.Text, out float num)) {
            Logger.GetSawmill("opendream.prompt").Error($"Error while trying to convert {_numberInput.Text} to a number.");
        }

        FinishPrompt(DMValueType.Num, num);
    }

    private void NumberInput_TextEntered(LineEdit.LineEditEventArgs obj) {
        ButtonClicked(DefaultButton);
    }
}
