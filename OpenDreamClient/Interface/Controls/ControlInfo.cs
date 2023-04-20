using System.Text;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;

namespace OpenDreamClient.Interface.Controls {
    [Virtual]
    public class InfoPanel : Control {
        public string PanelName { get; }

        protected InfoPanel(string name) {
            PanelName = name;
            TabContainer.SetTabTitle(this, name);
        }
    }

    public sealed class StatPanel : InfoPanel {
        private readonly Label _textBlock;

        public StatPanel(string name) : base(name) {
            _textBlock = new Label() {
                HorizontalAlignment = HAlignment.Stretch, VerticalAlignment = VAlignment.Stretch,
                // FontFamily = new FontFamily("Courier New")
            };

            var scrollViewer = new ScrollContainer() {
                Children = { _textBlock }
            };
            AddChild(scrollViewer);
        }

        public void UpdateLines(List<string> lines) {
            StringBuilder text = new StringBuilder();

            foreach (string line in lines) {
                text.Append(line + Environment.NewLine);
            }

            _textBlock.Text = text.ToString();
        }
    }

    public sealed class VerbPanel : InfoPanel {
        [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;
        private readonly GridContainer _grid;

        public VerbPanel(string name) : base(name) {
            _grid = new GridContainer { Columns = 4 };
            IoCManager.InjectDependencies(this);
            AddChild(_grid);
        }

        public void RefreshVerbs() {
            _grid.Children.Clear();

            foreach ((string verbName, string verbId, string verbCategory) in _dreamInterface.AvailableVerbs) {
                if (verbCategory != PanelName)
                    continue;

                Button verbButton = new Button() {
                    Margin = new Thickness(2),
                    MinWidth = 100,
                    Text = verbName
                };

                verbButton.Label.Margin = new Thickness(6, 0, 6, 2);
                verbButton.OnPressed += _ => {
                    EntitySystem.Get<DreamCommandSystem>().RunCommand(verbId);
                };

                _grid.Children.Add(verbButton);
            }
        }
    }

    public sealed class ControlInfo : InterfaceControl {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private TabContainer _tabControl;
        private readonly Dictionary<string, StatPanel> _statPanels = new();
        private readonly SortedDictionary<string, VerbPanel> _verbPanels = new();

        private bool _defaultPanelSent = false;

        public ControlInfo(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement() {
            _tabControl = new TabContainer() {
                /*BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)*/
            };
            _tabControl.OnTabChanged += OnSelectionChanged;

            RefreshVerbs();

            return _tabControl;
        }

        public void RefreshVerbs() {
            foreach (var panel in _verbPanels) {
                _verbPanels[panel.Key].RefreshVerbs();
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

            foreach (KeyValuePair<string, List<string>> updatingPanel in pUpdateStatPanels.StatPanels) {
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

        public VerbPanel CreateVerbPanel(string name) {
            var panel = new VerbPanel(name);
            _verbPanels.Add(name, panel);
            SortPanels();

            return panel;
        }

        public StatPanel CreateStatPanel(string name) {
            var panel = new StatPanel(name);
            panel.Margin = new Thickness(20, 2);
            _statPanels.Add(name, panel);
            SortPanels();
            return panel;
        }

        private void SortPanels() {
            _tabControl.Children.Clear();
            foreach(var (_, statPanel) in _statPanels) {
                _tabControl.AddChild(statPanel);
            }

            foreach(var (_, verbPanel) in _verbPanels) {
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
    }
}
