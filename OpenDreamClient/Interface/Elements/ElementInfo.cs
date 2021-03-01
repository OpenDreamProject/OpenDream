using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class ElementInfo : Border, IElement {
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorInfo)value;
            }
        }

        private ElementDescriptorInfo _elementDescriptor;
        private WrapPanel _verbPanel;

        public string[] Verbs {
            get => _verbs;
            set {
                _verbs = value;
                UpdateVisuals();
            }
        }

        private string[] _verbs = new string[0];

        public ElementInfo() {
            this.BorderBrush = Brushes.Black;
            this.BorderThickness = new Thickness(1);

            _verbPanel = new WrapPanel();
            this.Child = _verbPanel;
        }

        public void UpdateVisuals() {
            _verbPanel.Children.Clear();

            foreach (string verbName in Verbs) {
                Button verbButton = new Button();

                verbButton.Content = verbName.Replace("_", "__"); //WPF uses underscores for mnemonics; they need to be replaced with a double underscore
                verbButton.Margin = new Thickness(2);
                verbButton.Padding = new Thickness(6, 0, 6, 2);
                verbButton.MinWidth = 100;
                verbButton.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
                    Program.OpenDream.Connection.SendPacket(new PacketCallVerb(verbName));
                });

                _verbPanel.Children.Add(verbButton);
            }
        }
    }
}
