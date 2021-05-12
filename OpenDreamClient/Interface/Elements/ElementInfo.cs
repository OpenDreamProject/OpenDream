using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class InfoPanel : TabItem {
        public InfoPanel(string name) {
            Header = name;
        }
    }

    class StatPanel : InfoPanel {
        private TextBlock _textBlock;

        public StatPanel(string name) : base(name) {
            _textBlock = new TextBlock();
            _textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
            _textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            _textBlock.FontFamily = new FontFamily("Courier New");

            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.Content = _textBlock;
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
        private WrapPanel _wrapPanel;

        public VerbPanel(string name) : base(name) {
            _wrapPanel = new WrapPanel();
            AddChild(_wrapPanel);
        }

        public void UpdateVerbs(string[] verbs) {
            _wrapPanel.Children.Clear();

            foreach (string verbName in verbs) {
                Button verbButton = new Button();

                verbButton.Content = verbName.Replace("_", "__"); //WPF uses underscores for mnemonics; they need to be replaced with a double underscore
                verbButton.Margin = new Thickness(2);
                verbButton.Padding = new Thickness(6, 0, 6, 2);
                verbButton.MinWidth = 100;
                verbButton.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
                    Program.OpenDream.Connection.SendPacket(new PacketCallVerb(verbName));
                });

                _wrapPanel.Children.Add(verbButton);
            }
        }
    }

    class ElementInfo : TabControl, IElement {
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorInfo)value;
            }
        }

        private ElementDescriptorInfo _elementDescriptor;
        private Dictionary<string, StatPanel> _statPanels = new();
        private VerbPanel _verbPanel;

        private string[] _verbs = Array.Empty<string>();

        public ElementInfo() {
            this.BorderBrush = Brushes.Black;
            this.BorderThickness = new Thickness(1);

            _verbPanel = new VerbPanel("Verbs");
            Items.Add(_verbPanel);
        }

        public void UpdateVisuals() {
            
        }

        public void UpdateVerbs(PacketUpdateAvailableVerbs pUpdateAvailableVerbs) {
            _verbPanel.UpdateVerbs(pUpdateAvailableVerbs.AvailableVerbs);
        }

        public void UpdateStatPanels(PacketUpdateStatPanels pUpdateStatPanels) {
            //Remove any panels the packet doesn't contain
            foreach (KeyValuePair<string, StatPanel> existingPanel in _statPanels) {
                if (!pUpdateStatPanels.StatPanels.ContainsKey(existingPanel.Key)) {
                    Items.Remove(existingPanel.Value);
                    _statPanels.Remove(existingPanel.Key);
                }
            }

            foreach (KeyValuePair<string, List<string>> updatingPanel in pUpdateStatPanels.StatPanels) {
                StatPanel panel;

                if (!_statPanels.TryGetValue(updatingPanel.Key, out panel)) {
                    panel = new StatPanel(updatingPanel.Key);

                    Items.Insert(Items.Count - 1, panel);
                    _statPanels.Add(updatingPanel.Key, panel);
                }

                panel.UpdateLines(updatingPanel.Value);
            }
        }
    }
}
