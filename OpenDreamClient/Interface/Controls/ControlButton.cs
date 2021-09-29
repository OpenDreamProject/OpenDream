using OpenDreamShared.Interface;
using OpenDreamClient.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace OpenDreamClient.Interface.Controls {
    class ControlButton : InterfaceControl
    {
        private Button _button;

        public ControlButton(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _button = new Button();
            _button.OnPressed += OnButtonClick;

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
