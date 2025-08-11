using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls.UI;

/// <summary>
/// Similar functionality to RobustToolbox's OutputPanel
/// Works with inline controls in the rich texts
/// </summary>
public sealed class OutputControl : Control {
    public StyleBox? StyleBoxOverride {
        get => _panelContainer.PanelOverride;
        set => _panelContainer.PanelOverride = value;
    }

    private readonly PanelContainer _panelContainer;
    private readonly BoxContainer _messageContainer;

    public OutputControl() {
        _panelContainer = new PanelContainer {
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = {
                new ScrollContainer {
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    HScrollEnabled = false,
                    Children = {
                        (_messageContainer = new BoxContainer {
                            Orientation = BoxContainer.LayoutOrientation.Vertical
                        })
                    }
                }
            }
        };

        AddChild(_panelContainer);
    }

    public void AddMessage(FormattedMessage message) {
        var label = new RichTextLabel();

        label.SetMessage(message);
        _messageContainer.AddChild(label);
    }
}
