using System;
using System.Collections.Generic;
using System.Text;
using OpenDreamShared.Interface;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace OpenDreamClient.Interface.Controls
{
    class InfoPanel : Control
    {
        public string PanelName { get; private set; }

        public InfoPanel(string name)
        {
            PanelName = name;
            TabContainer.SetTabTitle(this, name);
        }
    }

    class StatPanel : InfoPanel
    {
        private Label _textBlock;

        public StatPanel(string name) : base(name)
        {
            _textBlock = new Label()
            {
                HorizontalAlignment = HAlignment.Stretch,
                VerticalAlignment = VAlignment.Stretch,
                // FontFamily = new FontFamily("Courier New")
            };

            var scrollViewer = new ScrollContainer()
            {
                Children = { _textBlock }
            };
            AddChild(scrollViewer);
        }

        public void UpdateLines(List<string> lines)
        {
            StringBuilder text = new StringBuilder();

            foreach (string line in lines)
            {
                text.Append(line + Environment.NewLine);
            }

            _textBlock.Text = text.ToString();
        }
    }

    class VerbPanel : InfoPanel
    {
        [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;
        private readonly GridContainer _grid;

        public VerbPanel(string name) : base(name)
        {
            _grid = new GridContainer { Columns = 4 };
            IoCManager.InjectDependencies(this);
            AddChild(_grid);
        }

        public void RefreshVerbs()
        {
            _grid.Children.Clear();

            foreach (string verbName in _dreamInterface.AvailableVerbs)
            {
                InterfaceButton verbButton = new InterfaceButton()
                {
                    Margin = new Thickness(2),
                    MinWidth = 100,
                    Text = verbName
                };

                verbButton.OnPressed += _ =>
                {
                    EntitySystem.Get<DreamCommandSystem>().RunCommand(verbName);
                };

                _grid.Children.Add(verbButton);
            }
        }
    }

    class ControlInfo : InterfaceControl
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private TabContainer _tabControl;
        private Dictionary<string, StatPanel> _statPanels = new();
        private VerbPanel _verbPanel;

        public ControlInfo(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement()
        {
            _tabControl = new TabContainer()
            {
                /*BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)*/
            };
            _tabControl.OnTabChanged += OnSelectionChanged;

            _verbPanel = new VerbPanel("Verbs");
            _tabControl.AddChild(_verbPanel);

            RefreshVerbs();

            return _tabControl;
        }

        public void RefreshVerbs()
        {
            _verbPanel.RefreshVerbs();
        }

        public void SelectStatPanel(string statPanelName)
        {
            if (_statPanels.TryGetValue(statPanelName, out StatPanel panel))
                _tabControl.CurrentTab = panel.GetPositionInParent();
        }

        public void UpdateStatPanels(MsgUpdateStatPanels pUpdateStatPanels)
        {
            //Remove any panels the packet doesn't contain
            foreach (KeyValuePair<string, StatPanel> existingPanel in _statPanels)
            {
                if (!pUpdateStatPanels.StatPanels.ContainsKey(existingPanel.Key))
                {
                    _tabControl.RemoveChild(existingPanel.Value);
                    _statPanels.Remove(existingPanel.Key);
                }
            }

            foreach (KeyValuePair<string, List<string>> updatingPanel in pUpdateStatPanels.StatPanels)
            {
                StatPanel panel;

                if (!_statPanels.TryGetValue(updatingPanel.Key, out panel))
                {
                    panel = new StatPanel(updatingPanel.Key);

                    _tabControl.AddChild(panel);
                    _statPanels.Add(updatingPanel.Key, panel);
                }

                panel.UpdateLines(updatingPanel.Value);
            }
        }

        private void OnSelectionChanged(int tabIndex)
        {
            InfoPanel panel = (InfoPanel)_tabControl.GetChild(tabIndex);

            var msg = _netManager.CreateNetMessage<MsgSelectStatPanel>();
            msg.StatPanel = panel.PanelName;
            _netManager.ClientSendMessage(msg);
        }
    }
}
