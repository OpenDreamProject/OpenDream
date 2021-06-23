using System.Windows.Controls;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamClient.Interface.Prompts
{
    class PasswordPrompt : InputWindow
    {
        public PasswordPrompt(int promptId, string title, string message, string defaultValue, bool canCancel) : base(
            promptId, title, message, defaultValue, canCancel)
        {
        }

        protected override Control CreateInputControl(string defaultValue)
        {
            return new PasswordBox
            {
                Password = defaultValue,
                VerticalAlignment = System.Windows.VerticalAlignment.Top
            };
        }

        protected override void OkButtonClicked()
        {
            FinishPrompt(DMValueType.Password, ((PasswordBox) _inputControl).Password);
        }
    }
}
