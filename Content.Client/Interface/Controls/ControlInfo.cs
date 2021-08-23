using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Interface;
using Content.Shared.Network.Messages;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Interface.Controls {
    class InfoPanel : Control {
        public string PanelName { get; private set; }

        public InfoPanel(string name) {
            PanelName = name;
            TabContainer.SetTabTitle(this, name);
        }
    }

    class StatPanel : InfoPanel {
        private Label _textBlock;

        public StatPanel(string name) : base(name) {
            _textBlock = new Label() {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
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

    class VerbPanel : InfoPanel {
        // TODO:
//        private WrapPanel _wrapPanel;

        public VerbPanel(string name) : base(name) {
            /*
            _wrapPanel = new WrapPanel();
            AddChild(_wrapPanel);
        */
        }

        public void RefreshVerbs() {
            /*_wrapPanel.Children.Clear();
            if (Program.OpenDream.AvailableVerbs == null) return;

            foreach (string verbName in Program.OpenDream.AvailableVerbs) {
                Button verbButton = new Button() {
                    Content = verbName.Replace("_", "__"), //WPF uses underscores for mnemonics; they need to be replaced with a double underscore
                    Margin = new Thickness(2),
                    Padding = new Thickness(6, 0, 6, 2),
                    MinWidth = 100
                };

                verbButton.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
                    Program.OpenDream.RunCommand(verbName);
                });

                _wrapPanel.Children.Add(verbButton);
            }*/
        }
    }

    class ControlInfo : InterfaceControl
    {
        // [Dependency]
        // private readonly OpenDream _openDream = default!;

        private TabContainer _tabControl;
        private Dictionary<string, StatPanel> _statPanels = new();
        private VerbPanel _verbPanel;

        public ControlInfo(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _tabControl = new TabContainer() {
                /*BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)*/
            };
            _tabControl.OnTabChanged += OnSelectionChanged;

            _verbPanel = new VerbPanel("Verbs");
            _tabControl.AddChild(_verbPanel);

            RefreshVerbs();

            return _tabControl;
        }

        public void RefreshVerbs() {
            _verbPanel.RefreshVerbs();
        }

        public void SelectStatPanel(string statPanelName) {
            if (_statPanels.TryGetValue(statPanelName, out StatPanel panel))
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
                StatPanel panel;

                if (!_statPanels.TryGetValue(updatingPanel.Key, out panel)) {
                    panel = new StatPanel(updatingPanel.Key);

                    _tabControl.AddChild(panel);
                    _statPanels.Add(updatingPanel.Key, panel);
                }

                panel.UpdateLines(updatingPanel.Value);
            }
        }

        private void OnSelectionChanged(int tabIndex) {
            InfoPanel panel = (InfoPanel)_tabControl.GetChild(tabIndex);

            // _openDream.Connection.SendPacket(new PacketSelectStatPanel(panel.PanelName));
        }
    }
}
