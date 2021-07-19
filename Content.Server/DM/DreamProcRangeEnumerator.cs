using System.Collections.Generic;

namespace Content.Server.DM {
    class DreamProcRangeEnumerator : IEnumerator<DreamValue> {
        private float _current;
        private float _start;
        private float _end;
        private float _step;

        public DreamProcRangeEnumerator(float rangeStart, float rangeEnd, float step) {
            _current = rangeStart - step;
            _start = rangeStart;
            _end = rangeEnd;
            _step = step;
        }

        DreamValue IEnumerator<DreamValue>.Current => new DreamValue(_current);

        public object Current => new DreamValue(_current);

        public bool MoveNext() {
            _current += _step;

            return _current <= _end;
        }

        public void Reset() {
            _current = _start - _step;
        }

        public void Dispose() {

        }
    }
}
