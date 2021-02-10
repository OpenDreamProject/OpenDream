using OpenDreamServer.Dream.Objects;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Procs {
    class DreamProcListEnumerator {
        public List<DreamValue> Values;
        public int CurrentIndex = 0;

        public DreamProcListEnumerator(List<DreamValue> values) {
            Values = values;
        }

        public bool TryMoveNext(out DreamValue value) {
            if (CurrentIndex < Values.Count) {
                value = Values[CurrentIndex++];

                return true;
            } else {
                value = new DreamValue((DreamObject) null);

                return false;
            }
        }
    }
}
