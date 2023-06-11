using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace OpenDreamClient.Interface.Controls;

public sealed class ControlMap : InterfaceControl {
    public ScalingViewport Viewport { get; private set; }

    private MouseInputSystem _mouseInput;

    public ControlMap(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) {
    }

    protected override Control CreateUIElement() {
        Viewport = new ScalingViewport {ViewportSize = (32 * 15, 32 * 15), MouseFilter = Control.MouseFilterMode.Stop};
        Viewport.OnKeyBindDown += OnViewportKeyBindDown;

        return new PanelContainer {StyleClasses = {"MapBackground"}, Children = {Viewport}};
    }

    private void OnViewportKeyBindDown(GUIBoundKeyEventArgs e) {
        if (e.Function == EngineKeyFunctions.Use || e.Function == EngineKeyFunctions.TextCursorSelect || e.Function ==  EngineKeyFunctions.UIRightClick) {
            IoCManager.Resolve<IEntitySystemManager>().Resolve(ref _mouseInput);

            if (_mouseInput.HandleViewportClick(Viewport, e)) {
                e.Handle();
            }
        }
    }
}
