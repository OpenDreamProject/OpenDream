using OpenDreamShared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls {
    sealed class ControlGrid : InterfaceControl {
        private GridContainer _grid;

        public ControlGrid(ControlDescriptor controlDescriptor, ControlWindow window) :
            base(controlDescriptor, window) {
        }

        protected override Control CreateUIElement() {
            _grid = new GridContainer() {

            };

            return _grid;
        }
    }
}
