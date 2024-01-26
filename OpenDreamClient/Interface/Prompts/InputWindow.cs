﻿using OpenDreamShared.Dream;
using Robust.Client.UserInterface;

namespace OpenDreamClient.Interface.Prompts;

[Virtual]
internal class InputWindow : PromptWindow {
    protected InputWindow(string title, string message, bool canCancel,
        Action<DreamValueType, object?>? onClose) : base(title, message, onClose) {
        CreateButton("Ok", true);
        if (canCancel) CreateButton("Cancel", false);
    }

    protected void SetPromptControl(Control promptControl, bool grabKeyboard = true) {
        InputControl.RemoveAllChildren();
        InputControl.AddChild(promptControl);
        if (grabKeyboard) promptControl.GrabKeyboardFocus();
    }

    protected override void ButtonClicked(string button) {
        if (button == "Ok") OkButtonClicked();
        else FinishPrompt(DreamValueType.Null, null);

        base.ButtonClicked(button);
    }

    protected virtual void OkButtonClicked() {
        throw new NotImplementedException();
    }
}
