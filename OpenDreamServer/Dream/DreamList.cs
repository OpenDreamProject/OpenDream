using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamServer.Dream {
    delegate void DreamListValueAssignedEventHandler(DreamList list, DreamValue key, DreamValue value);
    delegate void DreamListBeforeValueRemovedEventHandler(DreamList list, DreamValue key, DreamValue value);

    class DreamList {
        public event DreamListValueAssignedEventHandler ValueAssigned;
        public event DreamListBeforeValueRemovedEventHandler BeforeValueRemoved;

        private List<DreamValue> _values = new List<DreamValue>();
        private Dictionary<object, DreamValue> _associativeValues = new Dictionary<object, DreamValue>();

        public DreamList CreateCopy(int start = 1, int end = 0) {
            DreamList copy = new DreamList();

            if (end == 0 || end > _values.Count) end = _values.Count;

            for (int i = start; i <= end; i++) {
                copy._values.Add(_values[i - 1]);
            }

            foreach (KeyValuePair<object, DreamValue> associativeValue in _associativeValues) {
                copy._associativeValues.Add(associativeValue.Key, associativeValue.Value);
            }

            return copy;
        }

        public List<DreamValue> GetValues() {
            return _values;
        }

        public Dictionary<object, DreamValue> GetAssociativeValues() {
            return _associativeValues;
        }

        public DreamValue GetValue(DreamValue key) {
            if (key.Type == DreamValue.DreamValueType.String) {
                string keyString = key.GetValueAsString();

                if (_associativeValues.ContainsKey(keyString)) {
                    return _associativeValues[keyString];
                } else {
                    return new DreamValue((DreamObject)null);
                }
            } else if (key.Type == DreamValue.DreamValueType.DreamPath) {
                DreamPath keyPath = key.GetValueAsPath();

                if (_associativeValues.ContainsKey(keyPath)) {
                    return _associativeValues[keyPath];
                } else {
                    return new DreamValue((DreamObject)null);
                }
            } else if (key.TryGetValueAsDreamObject(out DreamObject keyObject)) {
               if (_associativeValues.ContainsKey(keyObject)) {
                    return _associativeValues[keyObject];
                } else {
                    return new DreamValue((DreamObject)null);
                }
            } else if (key.Type == DreamValue.DreamValueType.Integer) {
                return _values[key.GetValueAsInteger() - 1]; //1-indexed
            } else {
                throw new ArgumentException("Invalid index " + key);
            }
        }

        public void SetValue(DreamValue key, DreamValue value) {
            if (ValueAssigned != null) ValueAssigned.Invoke(this, key, value);
            if (key.IsType(DreamValue.DreamValueType.String | DreamValue.DreamValueType.DreamPath | DreamValue.DreamValueType.DreamObject) && key.Value != null) {
                if (!ContainsValue(key)) _values.Add(key);

                _associativeValues[key.Value] = value;
            } else if (key.Type == DreamValue.DreamValueType.Integer) {
                _values[key.GetValueAsInteger() - 1] = value;
            } else {
                throw new ArgumentException("Invalid index " + key);
            }

        }

        public void RemoveValue(DreamValue value) {
            int valueIndex = _values.IndexOf(value);

            if (valueIndex != -1) {
                if (BeforeValueRemoved != null) BeforeValueRemoved.Invoke(this, new DreamValue(valueIndex), _values[valueIndex]);
                _values.RemoveAt(valueIndex);
            }
        }

        public void AddValue(DreamValue value) {
            _values.Add(value);
            if (ValueAssigned != null) ValueAssigned.Invoke(this, new DreamValue(_values.Count), value);
        }

        //Does not include associations
        public bool ContainsValue(DreamValue value) {
            foreach (DreamValue listValue in _values) {
                if (value == listValue) return true;
            }

            return false;
        }

        public int FindValue(DreamValue value, int start = 1, int end = 0) {
            if (end == 0 || end > _values.Count) end = _values.Count;

            for (int i = start; i < end; i++) {
                if (_values[i - 1].Equals(value)) return i;
            }

            return 0;
        }

        public void Cut(int start = 1, int end = 0) {
            if (end == 0 || end > (_values.Count + 1)) end = _values.Count + 1;

            for (int i = end - 1; i >= start; i--) {
                if (BeforeValueRemoved != null) BeforeValueRemoved.Invoke(this, new DreamValue(i), _values[i - 1]);
                _values.RemoveAt(i - 1);
            }
        }

        public string Join(string glue, int start = 1, int end = 0) {
            if (end == 0 || end > (_values.Count + 1)) end = _values.Count + 1;

            string result = String.Empty;
            for (int i = start; i < end; i++) {
                result += _values[i - 1].Stringify();
                if (i != end - 1) result += glue;
            }

            return result;
        }

        public int GetLength() {
            return _values.Count;
        }
    }
}
