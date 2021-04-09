using OpenDreamServer.Dream.Objects;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Procs {
    interface IDreamProcEnumerator {
        public bool TryMoveNext(out DreamValue value);
    }

    class DreamProcListEnumerator : IDreamProcEnumerator {
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

    class DreamProcRangeEnumerator : IDreamProcEnumerator {
        private float _current;
        private float _end;
        private float _step;

        public DreamProcRangeEnumerator(float rangeStart, float rangeEnd, float step) {
            _current = rangeStart;
            _end = rangeEnd;
            _step = step;
        }

        public bool TryMoveNext(out DreamValue value) {
            if (_current <= _end) {
                value = new DreamValue(_current);
                _current += _step;

                return true;
            } else {
                value = DreamValue.Null;

                return false;
            }
        }
    }
}
