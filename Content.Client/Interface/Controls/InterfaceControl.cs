using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Content.Shared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Interface.Controls
{
    abstract class InterfaceControl : InterfaceElement
    {
        public Control UIElement { get; private set; }
        public bool IsDefault { get => _controlDescriptor.IsDefault; }
        public Size? Size { get => _controlDescriptor.Size; }
        public Point? Pos { get => _controlDescriptor.Pos; }
        public Point? Anchor1 { get => _controlDescriptor.Anchor1; }
        public Point? Anchor2 { get => _controlDescriptor.Anchor2; }

        protected ControlDescriptor _controlDescriptor { get => _elementDescriptor as ControlDescriptor; }
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
                UIElement.SetSize = (size.Width, size.Height);

            _window?.UpdateAnchors();

            // todo: implement background colors.
            /*
            if (Descriptor.BackgroundColor is { } bgColor)
            {
                var styleBox = new StyleBoxFlat { BackgroundColor = bgColor };

                switch (UIControl)
                {
                    case Panel panel:
                        panel.Background = brush;
                        break;
                    case Control control:
                        control.Background = brush;
                        break;
                }
            }
            */

            UIElement.Visible = _controlDescriptor.IsVisible;
            // TODO: enablement
            //UIControl.IsEnabled = !_controlDescriptor.IsDisabled;
        }

        public virtual void Output(string value, string data) {

        }
    }
}
