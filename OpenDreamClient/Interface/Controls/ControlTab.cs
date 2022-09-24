using OpenDreamShared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls {
    sealed class ControlTab : InterfaceControl {
        private TabContainer _tab;

        public ControlTab(ControlDescriptor controlDescriptor, ControlWindow window) :
            base(controlDescriptor, window) {
        }

        protected override Control CreateUIElement() {
            _tab = new TabContainer() {

            };

            return _tab;
        }
    }
}
