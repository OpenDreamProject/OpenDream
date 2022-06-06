using OpenDreamShared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls
{
    sealed class ControlMap : InterfaceControl
    {
        public ScalingViewport Viewport { get; private set; }

        public ControlMap(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window)
        {
        }

        protected override Control CreateUIElement()
        {
            Viewport = new ScalingViewport
            {
                ViewportSize = (32 * 15, 32 * 15) ,
                MouseFilter = Control.MouseFilterMode.Stop
            };
            return new PanelContainer
            {
                StyleClasses = { "MapBackground" },
                Children = { Viewport }
            };
        }
    }
}
