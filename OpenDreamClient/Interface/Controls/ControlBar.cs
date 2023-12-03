using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlBar : InterfaceControl {
    private ProgressBar? _bar = null;
    private Slider? _slider = null;
    private BoxContainer? _container;

    public ControlBar(ControlDescriptor controlDescriptor, ControlWindow window) :
        base(controlDescriptor, window) {
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();
        OnContainerResized(); //most of the actual logic needs to go here anyway, so lets just shove it all in there
    }

    private void OnValueChanged(Robust.Client.UserInterface.Controls.Range range) {
        if(_slider is not null && _slider.Grabbed) //don't run while you're still sliding, only after
            return;
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        if (controlDescriptor.OnChange != null) {
            _interfaceManager.RunCommand(controlDescriptor.OnChange);
        }
    }


    protected override Control CreateUIElement() {
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        _container = new BoxContainer() {Orientation = BoxContainer.LayoutOrientation.Vertical};
        _container.OnResized += OnContainerResized;
        return _container;
    }

    private void OnContainerResized() {
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        //TODO dir - these both need RT level changes
        //TODO angles

        //width
        float bar_width = controlDescriptor.Width != null ? (float)controlDescriptor.Width : 10;
        if(bar_width == 0 && this._container is not null)
            bar_width = _container.Size.Y;

        //is-slider
        if(controlDescriptor.IsSlider){
            if(_slider is null){
                _slider = new Slider() {MaxValue = 100, MinValue = 0, Margin=new Thickness(4), HorizontalExpand=true, MinHeight=bar_width};
                if(_bar is not null){
                    _container!.RemoveChild(_bar);
                    _bar = null;
                }
                _container!.AddChild(_slider);
            } else {
                _slider.SetHeight=bar_width;
            }

            //value
            _slider.Value = controlDescriptor.Value == null ? 0.0f : (float)controlDescriptor.Value;
            //on-change
            _slider.OnValueChanged += OnValueChanged;
            //bar-color
            _slider.TryGetStyleProperty<StyleBox>(Slider.StylePropertyGrabber, out var box);
            if(box is not null){
                StyleBoxFlat boxflat = (StyleBoxFlat) box;
                boxflat.BackgroundColor = controlDescriptor.BarColor is null ? Color.Transparent : controlDescriptor.BarColor.Value;
                _slider.GrabberStyleBoxOverride = boxflat;
            }
        } else {
            if(_bar is null){
                _bar = new ProgressBar() {MaxValue = 100, MinValue = 0, Margin=new Thickness(4), HorizontalExpand=true, MinHeight=bar_width};
                if(_slider is not null){
                    _container!.RemoveChild(_slider);
                    _slider = null;
                }
                _container!.AddChild(_bar);
            } else {
                _bar.SetHeight=bar_width;
            }
            //on-change
            if (controlDescriptor.OnChange != null && _bar.Value != controlDescriptor.Value) {
                _interfaceManager.RunCommand(controlDescriptor.OnChange);
            }
            //bar-color
            _bar.TryGetStyleProperty<StyleBoxFlat>(ProgressBar.StylePropertyForeground , out var box);
            if(box is not null){
                StyleBoxFlat boxflat = (StyleBoxFlat) box;
                boxflat.BackgroundColor = controlDescriptor.BarColor is null ? Color.Transparent : controlDescriptor.BarColor.Value;
                _bar.ForegroundStyleBoxOverride = boxflat;
            }
            //value
            _bar.Value = controlDescriptor.Value == null ? 0.0f : (float)controlDescriptor.Value;
        }
    }
}
