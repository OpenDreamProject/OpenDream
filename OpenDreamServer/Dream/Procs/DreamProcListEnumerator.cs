using OpenDreamServer.Dream.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Procs {
    class DreamProcListEnumerator {
        public DreamList List;
        public int CurrentIndex = 0;

        public DreamProcListEnumerator(DreamList list) {
            List = list;
        }

        public bool TryMoveNext(out DreamValue value) {
            CurrentIndex++;

            if (CurrentIndex <= List.GetLength()) {
                value = List.GetValue(new DreamValue(CurrentIndex));

                return true;
            } else {
                value = new DreamValue((DreamObject) null);

                return false;
            }
        }
    }
}
