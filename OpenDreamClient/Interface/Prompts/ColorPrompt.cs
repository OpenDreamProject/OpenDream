using OpenDreamShared.Dream.Procs;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace OpenDreamClient.Interface.Prompts
{
    class ColorPrompt : InputWindow
    {
        public ColorPrompt(int promptId, string title, string message, string defaultValue, bool canCancel) : base(
            promptId, title, message, defaultValue, canCancel)
        {
        }

        protected override Control CreateInputControl(string defaultValue)
        {
            Color? defaultColor = null;
            try
            {
                defaultColor = (Color?) ColorConverter.ConvertFromString(defaultValue);
            }
            catch (FormatException)
            {
            }

            return new ColorPicker {SelectedColor = defaultColor};
        }

        protected override void OkButtonClicked()
        {
            var chosenColor = ((ColorPicker) _inputControl).SelectedColor;
            FinishPrompt(DMValueType.Color, chosenColor.ToString());
        }
    }
}
