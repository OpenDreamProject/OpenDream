using System.Collections.Generic;

namespace OpenDreamShared.Json {
    public enum JsonVariableType {
        Resource = 0,
        Type = 1,
        Proc = 2,
        List = 3,
        ProcStub = 4,
        VerbStub = 5,
        PositiveInfinity = 6,
        NegativeInfinity = 7
    }

    public sealed class DreamTypeJson {
        public string Path { get; set; }
        public int? Parent { get; set; }
        public int? InitProc { get; set; }
        public List<List<int>> Procs { get; set; }
        public List<int> Verbs { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, int> GlobalVariables { get; set; }
        public HashSet<string>? ConstVariables { get; set; }
        public HashSet<string>? TmpVariables { get; set; }
    }

    public sealed class GlobalListJson {
        public int GlobalCount { get; set; }
        public List<string> Names { get; set; }
        public Dictionary<int, object> Globals { get; set; }
    }
}
