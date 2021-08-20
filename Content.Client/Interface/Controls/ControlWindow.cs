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

        public void UpdateAnchors()
        {
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

            return container;
        }

    }
}
