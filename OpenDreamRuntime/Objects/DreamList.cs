using OpenDreamVM.Procs;
using System;
using System.Collections.Generic;

namespace OpenDreamVM.Objects {
    delegate void DreamListValueAssignedEventHandler(DreamList list, DreamValue key, DreamValue value);
    delegate void DreamListBeforeValueRemovedEventHandler(DreamList list, DreamValue key, DreamValue value);

    public class DreamList : DreamObject {
        internal event DreamListValueAssignedEventHandler ValueAssigned;
        internal event DreamListBeforeValueRemovedEventHandler BeforeValueRemoved;

        private List<DreamValue> _values = new();
        private Dictionary<DreamValue, DreamValue> _associativeValues = new();
        private object _listLock = new object();

        public DreamList(DreamRuntime runtime) : base(runtime, runtime.ListDefinition, new DreamProcArguments(null)) {}

        public DreamList(DreamRuntime runtime, DreamProcArguments creationArguments) : base(runtime, runtime.ListDefinition, creationArguments) { }

        public DreamList(DreamRuntime runtime, IEnumerable<object> collection) : base(runtime, runtime.ListDefinition, new DreamProcArguments(null)) {
            foreach (object value in collection) {
                _values.Add(new DreamValue(value));
            }
        }

        public bool IsAssociative() {
            return _associativeValues.Count > 0;
        }

        public DreamList CreateCopy(int start = 1, int end = 0) {
            DreamList copy = new DreamList(Runtime);

            if (end == 0 || end > _values.Count) end = _values.Count;

            lock (copy._listLock) {
                for (int i = start; i <= end; i++) {
                    DreamValue value = _values[i - 1];

                    copy._values.Add(value);
                    if (_associativeValues.ContainsKey(value)) {
                        copy._associativeValues.Add(value, _associativeValues[value]);
                    }
                }
            }

            return copy;
        }

        public virtual List<DreamValue> GetValues() {
            return _values;
        }

        public Dictionary<DreamValue, DreamValue> GetAssociativeValues() {
            return _associativeValues;
        }

        public virtual DreamValue GetValue(DreamValue key) {
            if (key.Type == DreamValue.DreamValueType.Integer) {
                return _values[key.GetValueAsInteger() - 1]; //1-indexed
            }
            if (IsValidAssociativeKey(key)) {
                return _associativeValues.TryGetValue(key, out DreamValue value) ? value : DreamValue.Null;
            }
            throw new ArgumentException("Invalid index " + key);
        }

        public virtual void SetValue(DreamValue key, DreamValue value) {
            ValueAssigned?.Invoke(this, key, value);

            lock (_listLock) {
                if (IsValidAssociativeKey(key)) {
                    if (!ContainsValue(key)) _values.Add(key);

                    _associativeValues[key] = value;
                } else if (key.Type == DreamValue.DreamValueType.Integer) {
                    _values[key.GetValueAsInteger() - 1] = value;
                } else {
                    throw new ArgumentException("Invalid index " + key);
                }
            }
        }

        public void RemoveValue(DreamValue value) {
            int valueIndex = _values.IndexOf(value);

            if (valueIndex != -1) {
                BeforeValueRemoved?.Invoke(this, new DreamValue(valueIndex), _values[valueIndex]);

                lock (_listLock) {
                    _values.RemoveAt(valueIndex);
                }
            }
        }

        public void AddValue(DreamValue value) {
            lock (_listLock) {
                _values.Add(value);
            }

            ValueAssigned?.Invoke(this, new DreamValue(_values.Count), value);
        }

        public static bool IsValidAssociativeKey(DreamValue key) {
            return (key == DreamValue.Null || key.IsType(DreamValue.DreamValueType.String |
                                                         DreamValue.DreamValueType.DreamPath |
                                                         DreamValue.DreamValueType.DreamObject |
                                                         DreamValue.DreamValueType.DreamResource));
        }

        //Does not include associations
        public bool ContainsValue(DreamValue value) {
            lock (_listLock) {
                foreach (DreamValue listValue in _values) {
                    if (value == listValue) return true;
                }
            }

            return false;
        }

        public int FindValue(DreamValue value, int start = 1, int end = 0) {
            if (end == 0 || end > _values.Count) end = _values.Count;

            for (int i = start; i <= end; i++) {
                if (_values[i - 1].Equals(value)) return i;
            }

            return 0;
        }

        public void Cut(int start = 1, int end = 0) {
            if (end == 0 || end > (_values.Count + 1)) end = _values.Count + 1;

            if (BeforeValueRemoved != null) {
                for (int i = end - 1; i >= start; i--) {
                    BeforeValueRemoved.Invoke(this, new DreamValue(i), _values[i - 1]);
                }
            }

            lock (_listLock) {
                _values.RemoveRange(start - 1, end - start);
            }
        }

        public void Insert(int index, DreamValue value) {
            lock (_listLock) {
                _values.Insert(index - 1, value);
            }
        }

        public void Swap(int index1, int index2) {
            DreamValue temp = GetValue(new DreamValue(index1));

            SetValue(new DreamValue(index1), GetValue(new DreamValue(index2)));
            SetValue(new DreamValue(index2), temp);
        }

        public void Resize(int size) {
            if (size > _values.Count) {
                _values.Capacity = size;

                for (int i = _values.Count; i < size; i++) {
                    AddValue(DreamValue.Null);
                }
            } else {
                Cut(size + 1);
            }
        }

        public int GetLength() {
            return _values.Count;
        }
    }

    // /datum.vars list
    class DreamListVars : DreamList {
        private DreamObject _dreamObject;

        public DreamListVars(DreamObject dreamObject) : base(dreamObject.Runtime) {
            _dreamObject = dreamObject;
        }

        public override List<DreamValue> GetValues() {
            return _dreamObject.GetVariableNames();
        }

        public override DreamValue GetValue(DreamValue key) {
            return _dreamObject.GetVariable(key.GetValueAsString());
        }

        public override void SetValue(DreamValue key, DreamValue value) {
            string varName = key.GetValueAsString();

            if (_dreamObject.HasVariable(varName)) {
                _dreamObject.SetVariable(varName, value);
            }
        }
    }
}
