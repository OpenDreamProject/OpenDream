using System.Globalization;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlBar : InterfaceControl {
    private ProgressBar? _bar;
    private Slider? _slider;
    private Control _container = default!; // Created by base constructor

    private ControlDescriptorBar BarDescriptor => (ControlDescriptorBar)ElementDescriptor;

    public ControlBar(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) {
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        //width
        float barWidth = BarDescriptor.Width ?? 10f;

        //TODO dir - these both need RT level changes
        //TODO angles

        //is-slider
        if (BarDescriptor.IsSlider) {
            if (_slider is null) {
                _slider = new Slider {
                    MaxValue = 100,
                    MinValue = 0,
                    Margin = new Thickness(4),
                    HorizontalExpand = true,
                    VerticalExpand = (barWidth == 0f),
                    MinHeight = barWidth,
                    Value = BarDescriptor.Value ?? 0.0f
                };

                _slider.OnValueChanged += OnValueChanged;

                if (_bar is not null) {
                    _container.RemoveChild(_bar);
                    _bar = null;
                }

                _container.AddChild(_slider);
            } else {
                _slider.Value = BarDescriptor.Value ?? 0.0f;
            }

            //bar-color
            if (_slider.TryGetStyleProperty<StyleBoxFlat>(Slider.StylePropertyGrabber, out var box)) {
                box.BackgroundColor = BarDescriptor.BarColor ?? Color.Transparent;
                _slider.GrabberStyleBoxOverride = box;
            }
        } else {
            if (_bar is null) {
                _bar = new ProgressBar {
                    MaxValue = 100,
                    MinValue = 0,
                    Margin = new Thickness(4),
                    HorizontalExpand = true,
                    VerticalExpand = (barWidth == 0f),
                    MinHeight = barWidth,
                    Value = BarDescriptor.Value ?? 0.0f
                };

                _bar.OnValueChanged += OnValueChanged;

                if (_slider is not null) {
                    _container.RemoveChild(_slider);
                    _slider = null;
                }

                _container.AddChild(_bar);
            } else {
                _bar.Value = BarDescriptor.Value ?? 0.0f;
            }

            //bar-color
            if (_bar.TryGetStyleProperty<StyleBoxFlat>(ProgressBar.StylePropertyForeground, out var box)) {
                box.BackgroundColor = BarDescriptor.BarColor ?? Color.Transparent;
                _bar.ForegroundStyleBoxOverride = box;
            }
        }
    }

    private void OnValueChanged(Robust.Client.UserInterface.Controls.Range range) {
        //don't run while you're still sliding, only after
        // TODO: RT doesn't update Grabbed until after OnValueChanged, fix that
        //if (_slider is not null && _slider.Grabbed)
        //    return;

        if (BarDescriptor.OnChange != null) {
            var valueReplaced =
                BarDescriptor.OnChange.Replace("[[*]]", range.Value.ToString(CultureInfo.InvariantCulture));

            _interfaceManager.RunCommand(valueReplaced);
        }
    }

    protected override Control CreateUIElement() {
        _container = new Control();
        return _container;
    }
}
