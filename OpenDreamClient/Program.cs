using System;
using System.Globalization;

namespace OpenDreamClient {
    class Program {
        public static OpenDream OpenDream;

        [STAThread]
        static void Main(string[] args) {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            new OpenDreamApplication().Run();
        }
    }
}
