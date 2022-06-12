using OpenDreamShared.Interface;
using OpenDreamClient.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls {
    sealed class InterfaceButton : Button
    {
        public InterfaceButton()
        {
            Label.Margin = new Thickness(6, 0, 6, 2);
        }
    }

    sealed class ControlButton : InterfaceControl
    {
        public const string StyleClassDMFButton = "DMFbutton";

        private Button _button;

        public ControlButton(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _button = new InterfaceButton();
            _button.OnPressed += OnButtonClick;
            _button.ClipText = true;
            _button.Label.AddStyleClass("DMFbutton");

            return _button;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

            _button.Text = controlDescriptor.Text;
        }

        private void OnButtonClick(BaseButton.ButtonEventArgs args) {
            ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

            if (controlDescriptor.Command != null) {
                EntitySystem.Get<DreamCommandSystem>().RunCommand(controlDescriptor.Command);
            }
        }
    }
}
