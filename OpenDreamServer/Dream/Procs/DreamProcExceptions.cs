using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Procs {
    class DreamProcReturnException : Exception {
        public DreamValue ReturnValue;

        public DreamProcReturnException(DreamValue returnValue) {
            ReturnValue = returnValue;
        }
    }

    class DreamProcBreakException : Exception {

    }

    class DreamProcContinueException : Exception {

    }
}
