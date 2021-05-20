using OpenDreamRuntime;
using System;

namespace OpenDreamRuntime.Resources {
    //A special resource that outputs to the console
    //world.log defaults to this
    class ConsoleOutputResource : DreamResource {
        public ConsoleOutputResource() : base(null, null) { }

        public override string ReadAsString() {
            return null;
        }

        public override void Output(DreamValue value) {
            if (value.TryGetValueAsString(out string text)) {
                Console.WriteLine(text);
            } else {
                Console.WriteLine(value.ToString());
            }
        }
    }
}
