using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamRuntime {
    public class DreamGlobalVariable {
        public DreamValue Value;

        public DreamGlobalVariable(DreamValue value) {
            Value = value;
        }
    }
}
