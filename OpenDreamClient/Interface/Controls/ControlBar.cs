using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlBar : InterfaceControl {
    private ProgressBar? _bar;
    private Slider? _slider;
    private BoxContainer _container = default!; // Created by base constructor

    public ControlBar(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) {
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();
        OnContainerResized(); //most of the actual logic needs to go here anyway, so lets just shove it all in there
    }

    private void OnValueChanged(Robust.Client.UserInterface.Controls.Range range) {
        if (_slider is not null && _slider.Grabbed) //don't run while you're still sliding, only after
            return;

        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        if (controlDescriptor.OnChange != null) {
            _interfaceManager.RunCommand(controlDescriptor.OnChange);
        }
    }


    protected override Control CreateUIElement() {
        _container = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        _container.OnResized += OnContainerResized;
        return _container;
    }

    private void OnContainerResized() {
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        //TODO dir - these both need RT level changes
        //TODO angles

        //width
        float barWidth = controlDescriptor.Width ?? 10f;
        if (barWidth == 0)
            barWidth = _container.Size.Y;

        //is-slider
        if (controlDescriptor.IsSlider) {
            if (_slider is null) {
                _slider = new Slider {
                    MaxValue = 100,
                    MinValue = 0,
                    Margin = new Thickness(4),
                    HorizontalExpand = true,
                    MinHeight = barWidth
                };

                if (_bar is not null) {
                    _container.RemoveChild(_bar);
                    _bar = null;
                }

                _container.AddChild(_slider);
            } else {
                _slider.SetHeight = barWidth;
            }

            //value
            _slider.Value = controlDescriptor.Value ?? 0.0f;
            //on-change
            _slider.OnValueChanged += OnValueChanged;
            //bar-color
            _slider.TryGetStyleProperty<StyleBox>(Slider.StylePropertyGrabber, out var box);
            if (box is not null) {
                StyleBoxFlat boxFlat = (StyleBoxFlat)box;
                boxFlat.BackgroundColor = controlDescriptor.BarColor ?? Color.Transparent;
                _slider.GrabberStyleBoxOverride = boxFlat;
            }
        } else {
            if (_bar is null) {
                _bar = new ProgressBar {
                    MaxValue = 100,
                    MinValue = 0,
                    Margin = new Thickness(4),
                    HorizontalExpand = true,
                    MinHeight = barWidth
                };

                if (_slider is not null) {
                    _container.RemoveChild(_slider);
                    _slider = null;
                }

                _container.AddChild(_bar);
            } else {
                _bar.SetHeight = barWidth;
            }

            //on-change
            if (controlDescriptor.OnChange != null && _bar.Value != controlDescriptor.Value) {
                _interfaceManager.RunCommand(controlDescriptor.OnChange);
            }

            //bar-color
            if (_bar.TryGetStyleProperty<StyleBoxFlat>(ProgressBar.StylePropertyForeground, out var box)) {
                box.BackgroundColor = controlDescriptor.BarColor ?? Color.Transparent;
                _bar.ForegroundStyleBoxOverride = box;
            }

            //value
            _bar.Value = controlDescriptor.Value ?? 0.0f;
        }
    }
}
