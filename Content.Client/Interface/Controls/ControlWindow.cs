using System;
using System.Collections.Generic;
using Content.Shared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;


namespace Content.Client.Interface.Controls
{
    sealed class ControlWindow : InterfaceControl
    {
        // NOTE: a "window" in BYOND does not necessarily map 1:1 to OS windows.
        // Just like in win32 (which is definitely what this is inspired by let's be real),
        // windows can be embedded into other windows as a way to do nesting.

        public List<InterfaceControl> ChildControls = new();

        private readonly WindowDescriptor _windowDescriptor;
        private MenuBar _menu = default!;
        private LayoutContainer _canvas = default!;

        public ControlWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor.MainControlDescriptor, null)
        {
            _windowDescriptor = windowDescriptor;
        }

        public override void UpdateElementDescriptor()
        {
            // Don't call base.UpdateElementDescriptor();
        }

        public void UpdateAnchors()
        {
            foreach (InterfaceControl control in ChildControls) {
                var element = control.UIElement;

                if (control.Anchor1.HasValue) {
                    var elementPos = control.Pos.GetValueOrDefault();
                    var windowSize = Size.GetValueOrDefault();

                    var offset1X = elementPos.X - (windowSize.Width * control.Anchor1.Value.X / 100f);
                    var offset1Y = elementPos.Y - (windowSize.Height * control.Anchor1.Value.Y / 100f);
                    var left = (_canvas.Width * control.Anchor1.Value.X / 100) + offset1X;
                    var top = (_canvas.Height * control.Anchor1.Value.Y / 100) + offset1Y;
                    LayoutContainer.SetMarginLeft(element, Math.Max(left, 0));
                    LayoutContainer.SetMarginTop(element, Math.Max(top, 0));

                    if (control.Anchor2.HasValue) {
                        var elementSize = control.Size.GetValueOrDefault();

                        var offset2X = (elementPos.X + elementSize.Width) - (windowSize.Width * control.Anchor2.Value.X / 100);
                        var offset2Y = (elementPos.Y + elementSize.Height) - (windowSize.Height * control.Anchor2.Value.Y / 100);
                        var width = (_canvas.Width * control.Anchor2.Value.X / 100) + offset2X - left;
                        var height = (_canvas.Height * control.Anchor2.Value.Y / 100) + offset2Y - top;
                        element.SetWidth = Math.Max(width, 0);
                        element.SetHeight = Math.Max(height, 0);
                    }
                }
            }
        }

        public void CreateChildControls(DreamInterfaceManager manager) {
            foreach (ControlDescriptor controlDescriptor in _windowDescriptor.ControlDescriptors) {
                if (controlDescriptor == _windowDescriptor.MainControlDescriptor) continue;

                InterfaceControl control = controlDescriptor switch {
                    ControlDescriptorChild => new ControlChild(controlDescriptor, this),
                    ControlDescriptorInput => new ControlInput(controlDescriptor, this),
                    ControlDescriptorButton => new ControlButton(controlDescriptor, this),
                    ControlDescriptorOutput => new ControlOutput(controlDescriptor, this),
                    ControlDescriptorInfo => new ControlInfo(controlDescriptor, this),
                    ControlDescriptorMap => new ControlMap(controlDescriptor, this),
                    ControlDescriptorBrowser => new ControlBrowser(controlDescriptor, this),
                    _ => throw new Exception("Invalid descriptor")
                };

                ChildControls.Add(control);
                _canvas.Children.Add(control.UIElement);
            }
        }


        // Because of how windows are not always real windows,
        // UIControl contains the *contents* of the window, not the actual OS window itself.
        protected override Control CreateUIElement() {
            var container = new BoxContainer
            {
                RectClipContent = true,
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    (_menu = new MenuBar()),
                    (_canvas = new LayoutContainer
                    {
                        VerticalExpand = true
                    })
                }
            };

            _canvas.OnResized += CanvasOnResized;

            return container;
        }

        private void CanvasOnResized()
        {
            UpdateAnchors();
        }
    }
}
