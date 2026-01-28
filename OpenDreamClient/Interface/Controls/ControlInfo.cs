using System.Linq;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamShared.Interface.Descriptors;
using OpenDreamClient.Interface.Html;
using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls;

[Virtual]
internal class InfoPanel : Control {
    public string PanelName { get; }

    protected InfoPanel(string name) {
        PanelName = name;
        TabContainer.SetTabTitle(this, name);
    }

    public virtual void UpdateElementDescriptor(ControlDescriptorInfo descriptor) {
    }
}

internal sealed class StatPanel : InfoPanel {
    private sealed class StatEntry {
        public readonly RichTextLabel NameLabel = new();
        public readonly RichTextLabel ValueLabel = new();

        private readonly ControlInfo _owner;
        private readonly IEntitySystemManager _entitySystemManager;
        private readonly FormattedMessage _nameText = new();
        private readonly FormattedMessage _valueText = new();
        private string _name = string.Empty;
        private string _value = string.Empty;
        private string? _atomRef;
        private Color _textColor = Color.Black;

        public StatEntry(ControlInfo owner, IEntitySystemManager entitySystemManager) {
            _owner = owner;
            _entitySystemManager = entitySystemManager;

            // TODO: Change color when the mouse is hovering (if clickable)
            //       I couldn't find a way to do this without recreating the FormattedMessage
            ValueLabel.MouseFilter = MouseFilterMode.Stop;
            ValueLabel.OnKeyBindDown += OnKeyBindDown;
            if (_owner.InfoDescriptor.TextColor.Value != Color.Black) {
                _textColor = _owner.InfoDescriptor.TextColor.Value;
            }
        }

        public void Clear() {
            _atomRef = null;
            _nameText.Clear();
            _valueText.Clear();

            NameLabel.SetMessage(_nameText);
            ValueLabel.SetMessage(_valueText);
        }

        public void SetTextColor(Color textColor) {
            if (_textColor == textColor)
                return;

            _textColor = textColor;
            UpdateLabels();
        }

        public void SetLabels(string name, string value, string? atomRef) {
            // TODO: Tabs should align with each other.
            //       Probably should be done by RT, but it just ignores them currently.
            _name = name.Replace("\t", "    ");
            _value = value.Replace("\t", "    ");
            _atomRef = atomRef;

            UpdateLabels();
        }

        private void UpdateLabels() {
            _nameText.Clear();
            _valueText.Clear();

            // Use the default color and font
            _nameText.PushColor(_textColor);
            _valueText.PushColor(_textColor);
            _nameText.PushTag(new MarkupNode("font", null, null));
            _valueText.PushTag(new MarkupNode("font", null, null));

            if (_owner.InfoDescriptor.AllowHtml.Value) {
                // TODO: Look into using RobustToolbox's markup parser once it's customizable enough
                HtmlParser.Parse(_name, _nameText);
                HtmlParser.Parse(_value, _valueText);
            } else {
                _nameText.AddText(_name);
                _valueText.AddText(_value);
            }

            NameLabel.SetMessage(_nameText);
            ValueLabel.SetMessage(_valueText);
        }

        private void OnKeyBindDown(GUIBoundKeyEventArgs e) {
            if (e.Function != EngineKeyFunctions.Use && e.Function != OpenDreamKeyFunctions.MouseMiddle &&
                e.Function != EngineKeyFunctions.TextCursorSelect)
                return;
            if (_atomRef == null)
                return;
            if (!_entitySystemManager.TryGetEntitySystem(out MouseInputSystem? mouseInputSystem))
                return;

            e.Handle();
            mouseInputSystem.HandleStatClick(_atomRef, e.Function == EngineKeyFunctions.UIRightClick, e.Function == OpenDreamKeyFunctions.MouseMiddle);
        }
    }

    private readonly ControlInfo _owner;
    private readonly IEntitySystemManager _entitySystemManager;
    private readonly GridContainer _grid;
    private readonly List<StatEntry> _entries = new();

    public StatPanel(ControlInfo owner, IEntitySystemManager entitySystemManager, string name) : base(name) {
        _owner = owner;
        _entitySystemManager = entitySystemManager;
        _grid = new() {
            Columns = 2
        };

        var scrollViewer = new ScrollContainer() {
            HScrollEnabled = false,
            Children = { _grid }
        };

        AddChild(scrollViewer);
    }

    public override void UpdateElementDescriptor(ControlDescriptorInfo descriptor) {
        base.UpdateElementDescriptor(descriptor);
        var textColor = (descriptor.TextColor.Value != Color.Transparent) ? descriptor.TextColor.Value : Color.Black;
        foreach (var entry in _entries) {
            entry.SetTextColor(textColor);
        }
    }

    public void UpdateLines(List<(string Name, string Value, string? AtomRef)> lines) {
        for (int i = 0; i < Math.Max(_entries.Count, lines.Count); i++) {
            var entry = GetEntry(i);

            if (i < lines.Count) {
                var line = lines[i];

                entry.SetLabels(line.Name, line.Value, line.AtomRef);
            } else {
                entry.Clear();
            }
        }
    }

    private StatEntry GetEntry(int index) {
        // Expand the entries if there aren't enough
        if (_entries.Count <= index) {
            for (int i = _entries.Count; i <= index; i++) {
                var entry = new StatEntry(_owner, _entitySystemManager);

                _grid.AddChild(entry.NameLabel);
                _grid.AddChild(entry.ValueLabel);
                _entries.Add(entry);
            }
        }

        return _entries[index];
    }
}

internal sealed class VerbPanel : InfoPanel {
    public static readonly string DefaultVerbPanel = "Verbs"; // TODO: default_verb_category

    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    private readonly ClientVerbSystem? _verbSystem;

    private readonly VerbPanelGrid _grid;

    private Color _highlightColor;
    private Color _textColor;

    public VerbPanel(string name) : base(name) {
        IoCManager.InjectDependencies(this);
        _entitySystemManager.TryGetEntitySystem(out _verbSystem);

        var scrollContainer = new ScrollContainer {
            HScrollEnabled = false
        };

        _grid = new VerbPanelGrid {
            VerticalAlignment = VAlignment.Top
        };

        scrollContainer.AddChild(_grid);
        AddChild(scrollContainer);
    }

    public override void UpdateElementDescriptor(ControlDescriptorInfo descriptor) {
        base.UpdateElementDescriptor(descriptor);

        _highlightColor = descriptor.HighlightColor.Value;
        _textColor = (descriptor.TextColor.Value != Color.Transparent) ? descriptor.TextColor.Value : Color.Black;

        foreach (var child in _grid.Children) {
            if (child is not Button button)
                continue;

            button.Label.FontColorOverride = _textColor;
        }
    }

    public void RefreshVerbs(IEnumerable<(int, ClientObjectReference, VerbSystem.VerbInfo)> verbs) {
        _grid.Children.Clear();

        foreach (var (verbId, src, verbInfo) in verbs.Order(VerbNameComparer.OrdinalInstance)) {
            if (verbInfo.GetCategoryOrDefault(DefaultVerbPanel) != PanelName)
                continue;

            Button verbButton = new Button {
                Margin = new Thickness(2),
                Text = verbInfo.Name,
                TextAlign = Label.AlignMode.Center
            };

            verbButton.Label.Margin = new Thickness(6, 0, 6, 2);
            verbButton.Label.FontColorOverride = _textColor;
            verbButton.StyleBoxOverride = new StyleBoxEmpty();

            verbButton.OnButtonDown += _ => {
                _verbSystem?.ExecuteVerb(src, verbId);
            };

            verbButton.OnMouseEntered += _ => {
                verbButton.Label.FontColorOverride = _highlightColor;
            };

            verbButton.OnMouseExited += _ => {
                verbButton.Label.FontColorOverride = _textColor;
            };

            _grid.Children.Add(verbButton);
        }
    }
}

public sealed class ControlInfo : InterfaceControl {
    public static readonly string StyleClassDMFInfo = "DMFInfo";

    public ControlDescriptorInfo InfoDescriptor => (ControlDescriptorInfo)ControlDescriptor;

    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private PanelContainer _container;
    private TabContainer _tabControl;
    private readonly Dictionary<string, StatPanel> _statPanels = new();
    private readonly SortedDictionary<string, VerbPanel> _verbPanels = new();

    private bool _defaultPanelSent;

    public ControlInfo(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) {
        IoCManager.InjectDependencies(this);
    }

    protected override Control CreateUIElement() {
        _container = new PanelContainer {
            Children = {
                (_tabControl = new TabContainer())
            },
            StyleClasses = { StyleClassDMFInfo }
        };

        _tabControl.OnTabChanged += OnSelectionChanged;

        _tabControl.OnVisibilityChanged += args => {
            if (args.Visible) {
                OnShowEvent();
            } else {
                OnHideEvent();
            }
        };

        if (ControlDescriptor.IsVisible.Value)
            OnShowEvent();
        else
            OnHideEvent();

        return _container;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        _container.PanelOverride = (InfoDescriptor.TabBackgroundColor.Value != Color.Transparent)
            ? new StyleBoxFlat(InfoDescriptor.TabBackgroundColor.Value)
            : null;
        _tabControl.PanelStyleBoxOverride = new StyleBoxInfoPanel((InfoDescriptor.BackgroundColor.Value != Color.Transparent)
            ? InfoDescriptor.BackgroundColor.Value
            : Color.White);
        _tabControl.TabFontColorOverride = (InfoDescriptor.TabTextColor.Value != Color.Transparent)
            ? InfoDescriptor.TabTextColor.Value
            : null;
        _tabControl.TabFontColorInactiveOverride = (InfoDescriptor.TabTextColor.Value != Color.Transparent)
            ? InfoDescriptor.TabTextColor.Value
            : null;

        foreach (var panel in _statPanels.Values)
            panel.UpdateElementDescriptor(InfoDescriptor);
        foreach (var panel in _verbPanels.Values)
            panel.UpdateElementDescriptor(InfoDescriptor);
    }

    public void RefreshVerbs(ClientVerbSystem verbSystem) {
        IEnumerable<(int, ClientObjectReference, VerbSystem.VerbInfo)> verbs = verbSystem.GetExecutableVerbs();

        foreach (var (_, _, verb) in verbs) {
            var category = verb.GetCategoryOrDefault(VerbPanel.DefaultVerbPanel);

            if (!HasVerbPanel(category)) {
                CreateVerbPanel(category);
            }
        }

        foreach (var panel in _verbPanels) {
            _verbPanels[panel.Key].RefreshVerbs(verbs);
        }
    }

    public void SelectStatPanel(string statPanelName) {
        if (_statPanels.TryGetValue(statPanelName, out var panel))
            _tabControl.CurrentTab = panel.GetPositionInParent();
    }

    public void UpdateStatPanels(MsgUpdateStatPanels pUpdateStatPanels) {
        //Remove any panels the packet doesn't contain
        foreach (KeyValuePair<string, StatPanel> existingPanel in _statPanels) {
            if (!pUpdateStatPanels.StatPanels.ContainsKey(existingPanel.Key)) {
                _tabControl.RemoveChild(existingPanel.Value);
                _statPanels.Remove(existingPanel.Key);
            }
        }

        foreach (var updatingPanel in pUpdateStatPanels.StatPanels) {
            if (!_statPanels.TryGetValue(updatingPanel.Key, out var panel)) {
                panel = CreateStatPanel(updatingPanel.Key);
            }

            panel.UpdateLines(updatingPanel.Value);
        }

        // Tell the server we're ready to receive data
        if (!_defaultPanelSent && _tabControl.ChildCount > 0) {
            var msg = new MsgSelectStatPanel() {
                StatPanel = _tabControl.GetActualTabTitle(0)
            };

            _netManager.ClientSendMessage(msg);
            _defaultPanelSent = true;
        }
    }

    public bool HasVerbPanel(string name) {
        return _verbPanels.ContainsKey(name);
    }

    public void CreateVerbPanel(string name) {
        var panel = new VerbPanel(name);
        panel.UpdateElementDescriptor(InfoDescriptor);
        _verbPanels.Add(name, panel);
        SortPanels();
    }

    private StatPanel CreateStatPanel(string name) {
        var panel = new StatPanel(this, _entitySystemManager, name);
        panel.Margin = new Thickness(20, 2);
        panel.UpdateElementDescriptor(InfoDescriptor);
        _statPanels.Add(name, panel);
        SortPanels();
        return panel;
    }

    private void SortPanels() {
        _tabControl.Children.Clear();
        foreach (var (_, statPanel) in _statPanels) {
            _tabControl.AddChild(statPanel);
        }

        foreach (var (_, verbPanel) in _verbPanels) {
            _tabControl.AddChild(verbPanel);
        }
    }

    private void OnSelectionChanged(int tabIndex) {
        InfoPanel panel = (InfoPanel)_tabControl.GetChild(tabIndex);
        var msg = new MsgSelectStatPanel() {
            StatPanel = panel.PanelName
        };

        _netManager.ClientSendMessage(msg);
    }

    public void OnShowEvent() {
        ControlDescriptorInfo controlDescriptor = (ControlDescriptorInfo)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnShowCommand.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.OnShowCommand.AsRaw());
        }
    }

    public void OnHideEvent() {
        ControlDescriptorInfo controlDescriptor = (ControlDescriptorInfo)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnHideCommand.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.OnHideCommand.AsRaw());
        }
    }
}

internal sealed class VerbNameComparer(bool ordinal) : IComparer<(int, ClientObjectReference, VerbSystem.VerbInfo)> {
    // Verbs are displayed alphabetically with uppercase coming first (BYOND behavior)
    public static VerbNameComparer OrdinalInstance = new(true);

    // Verbs are displayed alphabetically according to the user's culture
    public static VerbNameComparer CultureInstance = new(false);

    public int Compare((int, ClientObjectReference, VerbSystem.VerbInfo) a,
        (int, ClientObjectReference, VerbSystem.VerbInfo) b) =>
        string.Compare(a.Item3.Name, b.Item3.Name, ordinal ? StringComparison.Ordinal : StringComparison.CurrentCulture);
}
