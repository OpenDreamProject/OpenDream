using System;

namespace OpenDreamClient {
    class Program {
        public static OpenDream OpenDream;

        [STAThread]
        static void Main(string[] args) {
            new OpenDreamApplication().Run();
        }
    }
}
