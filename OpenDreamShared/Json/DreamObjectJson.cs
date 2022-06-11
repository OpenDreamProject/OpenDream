using System.Collections.Generic;

namespace OpenDreamShared.Json {
    public enum JsonVariableType {
        Resource = 0,
        Path = 1,
        List = 2
    }

    public sealed class DreamTypeJson {
        public string Path { get; set; }
        public int? Parent { get; set; }
        public int? InitProc { get; set; }
        public List<List<int>> Procs { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, int> GlobalVariables { get; set; }
        public List<int> Children { get; set; }
    }

    public sealed class GlobalListJson {
        public int GlobalCount { get; set; }
        public Dictionary<int, object> Globals { get; set; }
    }
}
