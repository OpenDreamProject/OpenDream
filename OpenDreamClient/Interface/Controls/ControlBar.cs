using OpenDreamClient.Interface.Descriptors;
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
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        if(controlDescriptor.IsSlider){
            if(_slider is null){
                _slider = new Slider() {MaxValue = 100, MinValue = 0};
                if(_bar is not null){
                    _container!.RemoveChild(_bar);
                    _bar = null;
                }
                _container!.AddChild(_slider);
            }
            _slider.Value = controlDescriptor.Value == null ? 0.0f : (float)controlDescriptor.Value;
        }
        else {
            if(_bar is null){
                _bar = new ProgressBar() {MaxValue = 100, MinValue = 0};
                if(_slider is not null){
                    _container!.RemoveChild(_slider);
                    _slider = null;
                }
                _container!.AddChild(_bar);
            }
            _bar.Value = controlDescriptor.Value == null ? 0.0f : (float)controlDescriptor.Value;
        }
    }

    protected override Control CreateUIElement() {
        ControlDescriptorBar controlDescriptor = (ControlDescriptorBar)ElementDescriptor;
        _container = new BoxContainer() {Orientation = BoxContainer.LayoutOrientation.Vertical};
        return _container;
    }
}
