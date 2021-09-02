using Content.Client.Input;
using Content.Shared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Interface.Controls {
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

            ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)_elementDescriptor;
            _button.Text = controlDescriptor.Text;
        }

        private void OnButtonClick(BaseButton.ButtonEventArgs args) {
            ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)_elementDescriptor;

            if (controlDescriptor.Command != null) {
                EntitySystem.Get<DreamCommandSystem>().RunCommand(controlDescriptor.Command);
            }
        }
    }
}
