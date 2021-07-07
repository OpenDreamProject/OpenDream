/*using System;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamClient.Interface.Prompts {
    class AlertWindow : PromptWindow {
        public AlertWindow(int promptId, String title, String message, String button1, String button2, String button3) : base(promptId, title, message) {
            CreateButton(button1, true);
            if (!String.IsNullOrEmpty(button2)) CreateButton(button2, false);
            if (!String.IsNullOrEmpty(button3)) CreateButton(button3, false);
        }

        protected override void ButtonClicked(string button) {
            FinishPrompt(DMValueType.Text, button);

            base.ButtonClicked(button);
        }
    }
}
*/
