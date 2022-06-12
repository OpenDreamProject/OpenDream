using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Procs {
    sealed class DreamProcRangeEnumerator : IEnumerator<DreamValue> {
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

    sealed class DreamObjectEnumerator : IEnumerator<DreamValue> {
        private IEnumerator<DreamObject> _dreamObjectEnumerator;
        private DreamPath? _filterType;

        public DreamObjectEnumerator(IEnumerable<DreamObject> dreamObjects, DreamPath? filterType = null) {
            _dreamObjectEnumerator = dreamObjects.GetEnumerator();
            _filterType = filterType;
        }

        DreamValue IEnumerator<DreamValue>.Current => new DreamValue(_dreamObjectEnumerator.Current);

        public object Current => new DreamValue(_dreamObjectEnumerator.Current);

        public bool MoveNext() {
            bool hasNext = _dreamObjectEnumerator.MoveNext();
            if (_filterType != null) {
                while (hasNext && !_dreamObjectEnumerator.Current.IsSubtypeOf(_filterType.Value)) {
                    hasNext = _dreamObjectEnumerator.MoveNext();
                }
            }

            return hasNext;
        }

        public void Reset() {
            _dreamObjectEnumerator.Reset();
        }

        public void Dispose() {
            _dreamObjectEnumerator.Dispose();
        }
    }

    sealed class DreamValueAsObjectEnumerator : IEnumerator<DreamValue> {
        private IEnumerator<DreamValue> _dreamValueEnumerator;
        private DreamPath? _filterType;

        public DreamValueAsObjectEnumerator(IEnumerable<DreamValue> dreamValues, DreamPath? filterType = null) {
            _dreamValueEnumerator = dreamValues.GetEnumerator();
            _filterType = filterType;
        }

        DreamValue IEnumerator<DreamValue>.Current => _dreamValueEnumerator.Current;

        public object Current => _dreamValueEnumerator.Current;

        public bool MoveNext() {
            bool hasNext = _dreamValueEnumerator.MoveNext();
            if (_filterType != null) {
                while (hasNext && !_dreamValueEnumerator.Current.TryGetValueAsDreamObjectOfType(_filterType.Value, out _))
                {
                    hasNext = _dreamValueEnumerator.MoveNext();
                }
            }
            else
            {
                while (hasNext && (_dreamValueEnumerator.Current.Type != DreamValue.DreamValueType.DreamObject || _dreamValueEnumerator.Current == DreamValue.Null))
                {
                    hasNext = _dreamValueEnumerator.MoveNext();
                }
            }

            return hasNext;
        }

        public void Reset() {
            _dreamValueEnumerator.Reset();
        }

        public void Dispose() {
            _dreamValueEnumerator.Dispose();
        }
    }
}
