using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Interface;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls
{
    public abstract class InterfaceControl : InterfaceElement
    {
        public Control UIElement { get; private set; }
        public bool IsDefault { get => _controlDescriptor.IsDefault; }
        public Vector2i? Size { get => _controlDescriptor.Size; }
        public Vector2i? Pos { get => _controlDescriptor.Pos; }
        public Vector2i? Anchor1 { get => _controlDescriptor.Anchor1; }
        public Vector2i? Anchor2 { get => _controlDescriptor.Anchor2; }

        protected ControlDescriptor _controlDescriptor { get => ElementDescriptor as ControlDescriptor; }
        protected ControlWindow _window;

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public InterfaceControl(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor)
        {
            IoCManager.InjectDependencies(this);

            _window = window;
            UIElement = CreateUIElement();

            UpdateElementDescriptor();
        }

        protected abstract Control CreateUIElement();


        public override void UpdateElementDescriptor()
        {
            UIElement.Name = _controlDescriptor.Name;

            var pos = _controlDescriptor.Pos.GetValueOrDefault();
            LayoutContainer.SetMarginLeft(UIElement, pos.X);
            LayoutContainer.SetMarginTop(UIElement, pos.Y);

            if (_controlDescriptor.Size is { } size)
                UIElement.SetSize = size;

            _window?.UpdateAnchors();

            if (_controlDescriptor.BackgroundColor is { } bgColor)
            {
                var styleBox = new StyleBoxFlat { BackgroundColor = bgColor };

                switch (UIElement)
                {
                    case PanelContainer panel:
                        panel.PanelOverride = styleBox;
                        break;
                }
            }

            UIElement.Visible = _controlDescriptor.IsVisible;
            // TODO: enablement
            //UIControl.IsEnabled = !_controlDescriptor.IsDisabled;
        }

        public virtual void Output(string value, string data) {

        }
    }
}
