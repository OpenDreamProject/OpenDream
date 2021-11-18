﻿using System.Collections.Generic;

namespace OpenDreamShared.Json {
    public class DreamCompiledJson {
        public List<string> Strings { get; set; }
        public List<object> Globals { get; set; }
        public ProcDefinitionJson GlobalInitProc { get; set; }
        public List<DreamMapJson> Maps { get; set; }
        public string Interface { get; set; }
        public DreamObjectJson RootObject { get; set; }
    }
}
