using System;

namespace OpenDreamClient {
    class Program {
        public static OpenDream OpenDream = new OpenDream();

        [STAThread]
        static void Main(string[] args) {
            OpenDream.MainWindow = new OpenDreamWindow();
            OpenDream.MainWindow.Show();
            OpenDream.Run();
        }
    }
}