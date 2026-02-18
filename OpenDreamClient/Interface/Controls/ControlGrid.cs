using OpenDreamShared.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlGrid : InterfaceControl {
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
