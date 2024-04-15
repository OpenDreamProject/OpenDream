using System.Linq;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlTab(ControlDescriptor controlDescriptor, ControlWindow window)
    : InterfaceControl(controlDescriptor, window) {
    private ControlDescriptorTab TabDescriptor => (ControlDescriptorTab)ElementDescriptor;

    private TabContainer _tab;
    private readonly List<ControlWindow> _tabs = new();

    protected override Control CreateUIElement() {
        _tab = new TabContainer();

        return _tab;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        _tabs.Clear();
        _tab.RemoveAllChildren();
        if (TabDescriptor.Tabs != null) {
            var tabIds = TabDescriptor.Tabs.Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var tabId in tabIds) {
                if (!_interfaceManager.Windows.TryGetValue(tabId, out var pane))
                    continue;

                TabContainer.SetTabTitle(pane.UIElement, pane.Title);
                _tab.AddChild(pane.UIElement);
                _tabs.Add(pane);
                if (TabDescriptor.CurrentTab == pane.Title)
                    _tab.CurrentTab = pane.UIElement.GetPositionInParent();
            }
        }
    }

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out DMFProperty? value) {
        switch (property) {
            case "current-tab":
                var currentTab = _tab.GetChild(_tab.CurrentTab);

                // The use of First() is kinda bad but hopefully this isn't large or performance critical
                value = _tabs.First(tab => tab.UIElement == currentTab).Id;
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }
}
