using System.Windows;

namespace OpenDreamClient {
    public class OpenDreamApplication : Application {
        public OpenDreamApplication() {
            MainWindow = new OpenDreamWindow(this);

            MainWindow.Show();
        }

        public void ConnectToDream(string ip, int port, string username) {
            Program.OpenDream = new OpenDream(username);
            Program.OpenDream.ConnectedToServer += OpenDream_ConnectedToServer;
            Program.OpenDream.DisconnectedFromServer += OpenDream_DisconnectedFromServer;
            Program.OpenDream.ConnectToServer(ip, port);
        }

        private void OpenDream_ConnectedToServer() {
            MainWindow.Hide();
        }

        private void OpenDream_DisconnectedFromServer() {
            Program.OpenDream = null;
            MainWindow.Show();
        }
    }
}
