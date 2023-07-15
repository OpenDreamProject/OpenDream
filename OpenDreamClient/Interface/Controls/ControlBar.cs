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
        //TODO dir
        //TODO angles
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        float bar_width = controlDescriptor.Width != null ? (float)controlDescriptor.Width : 10;
        if(bar_width == 0 && this._container is not null)
            bar_width = _container.Size.Y;
        if(controlDescriptor.IsSlider){
            if(_slider is null){
                _slider = new Slider() {MaxValue = 100, MinValue = 0, Margin=new Thickness(4), HorizontalExpand=true, MinHeight=bar_width};
                if(_bar is not null){
                    _container!.RemoveChild(_bar);
                    _bar = null;
                }
                _container!.AddChild(_slider);
            }
            _slider.Value = controlDescriptor.Value == null ? 0.0f : (float)controlDescriptor.Value;
            _slider.OnValueChanged += OnValueChanged;
            //TODO grabber color override
        }
        else {
            if(_bar is null){
                _bar = new ProgressBar() {MaxValue = 100, MinValue = 0, Margin=new Thickness(4), HorizontalExpand=true, MinHeight=bar_width};
                if(_slider is not null){
                    _container!.RemoveChild(_slider);
                    _slider = null;
                }
                _container!.AddChild(_bar);
            }
            if (controlDescriptor.OnChange != null && _bar.Value != controlDescriptor.Value) {
                EntitySystem.Get<DreamCommandSystem>().RunCommand(controlDescriptor.OnChange);
            }
            _bar.Value = controlDescriptor.Value == null ? 0.0f : (float)controlDescriptor.Value;
            //_bar.ForegroundStyleBoxOverride = new StyleBoxFlat(Color.Green); TODO
        }
    }

    private void OnValueChanged(Robust.Client.UserInterface.Controls.Range range) { //todo check if this runs only when you're done (it should for byond parity)
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        if (controlDescriptor.OnChange != null) {
            EntitySystem.Get<DreamCommandSystem>().RunCommand(controlDescriptor.OnChange);
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
        float bar_width = controlDescriptor.Width != null ? (float)controlDescriptor.Width : 10;
        if(bar_width == 0 && this._container is not null)
            bar_width = _container.Size.Y;
        if(_slider is not null)
            _slider.SetHeight=bar_width;
        else if (_bar is not null)
            _bar.SetHeight=bar_width;
    }
}
