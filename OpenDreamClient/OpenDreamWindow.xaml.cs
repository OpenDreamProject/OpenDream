using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace OpenDreamClient {
    /// <summary>
    /// Interaction logic for OpenDreamWindow.xaml
    /// </summary>
    public partial class OpenDreamWindow : Window {
        private Regex _portInputRegex = new Regex("[^0-9.-]+");

        public OpenDreamWindow() {
            InitializeComponent();
        }

        private void ConnectButton_Clicked(object sender, RoutedEventArgs e) {
            Program.OpenDream.ConnectToServer(IPInput.Text, int.Parse(PortInput.Text), UsernameInput.Text);
        }

        private void PortInput_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !_portInputRegex.IsMatch(e.Text);
        }
    }
}
