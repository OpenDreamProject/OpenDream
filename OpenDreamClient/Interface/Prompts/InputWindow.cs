using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;

namespace OpenDreamClient.Interface.Prompts
{
    [Virtual]
    class InputWindow : PromptWindow {
        public InputWindow(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message) {
            CreateButton("Ok", true);
            if (canCancel) CreateButton("Cancel", false);;
        }

        protected void SetPromptControl(Control promptControl, bool grabKeyboard = true) {
            InputControl.RemoveAllChildren();
            InputControl.AddChild(promptControl);
            if (grabKeyboard) promptControl.GrabKeyboardFocus();
        }

        protected override void ButtonClicked(string button) {
            if (button == "Ok") OkButtonClicked();
            else FinishPrompt(DMValueType.Null, null);

            base.ButtonClicked(button);
        }

        protected virtual void OkButtonClicked() {
            throw new NotImplementedException();
        }
    }}
