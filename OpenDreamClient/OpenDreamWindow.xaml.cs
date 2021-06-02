using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace OpenDreamClient {
    /// <summary>
    /// Interaction logic for OpenDreamWindow.xaml
    /// </summary>
    public partial class OpenDreamWindow : Window {
        private readonly Regex _portInputRegex = new Regex("[^0-9.-]+");

        private OpenDreamApplication _application;

        public OpenDreamWindow(OpenDreamApplication application) {
            _application = application;

            InitializeComponent();
        }

        private void ConnectButton_Clicked(object sender, RoutedEventArgs e) {
            _application.ConnectToDream(IPInput.Text, int.Parse(PortInput.Text), UsernameInput.Text);
        }

        private void PortInput_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !_portInputRegex.IsMatch(e.Text);
        }
    }
}
