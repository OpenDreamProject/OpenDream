using System;
using System.Collections.Generic;
using System.Linq;
using OpenDreamRuntime.Procs;
using Robust.Shared.IoC;

namespace OpenDreamRuntime.Objects {
    delegate void DreamListValueAssignedEventHandler(DreamList list, DreamValue key, DreamValue value);
    delegate void DreamListBeforeValueRemovedEventHandler(DreamList list, DreamValue key, DreamValue value);

    public class DreamList : DreamObject {
        internal event DreamListValueAssignedEventHandler ValueAssigned;
        internal event DreamListBeforeValueRemovedEventHandler BeforeValueRemoved;

        private List<DreamValue> _values = new();
        private Dictionary<DreamValue, DreamValue> _associativeValues = null;

        protected DreamList() : base(null) {
            ObjectDefinition = IoCManager.Resolve<IDreamManager>().ObjectTree.List.ObjectDefinition;
        }

        public static DreamList CreateUninitialized() {
            return new DreamList();
        }

        public static DreamList Create() {
            return new DreamList();
        }

        public static DreamList Create(IEnumerable<object> collection) {
            var list = new DreamList();

            foreach (object value in collection) {
                list._values.Add(new DreamValue(value));
            }

            return list;
        }

        public bool IsAssociative() {
            return _associativeValues != null && _associativeValues.Count > 0;
        }

        public DreamList CreateCopy(int start = 1, int end = 0) {
            DreamList copy = Create();

            if (start == 0) ++start; //start being 0 and start being 1 are equivalent
            if (end > _values.Count + 1) throw new Exception("list index out of bounds");
            if (end == 0) end = _values.Count + 1;

            for (int i = start; i < end; i++) {
                DreamValue value = _values[i - 1];

                copy._values.Add(value);
                if (ContainsKey(value)) {
                    copy.SetValue(value, _associativeValues[value]);
                }
            }

            return copy;
        }

        public virtual List<DreamValue> GetValues() {
            return _values;
        }

        public Dictionary<DreamValue, DreamValue> GetAssociativeValues() {
            return _associativeValues ??= new Dictionary<DreamValue, DreamValue>();
        }

        public virtual DreamValue GetValue(DreamValue key) {
            if (key.TryGetValueAsInteger(out int keyInteger)) {
                return _values[keyInteger - 1]; //1-indexed
            }

            return _associativeValues == null ? DreamValue.Null : (_associativeValues.TryGetValue(key, out DreamValue value) ? value : DreamValue.Null);
        }

        public virtual void SetValue(DreamValue key, DreamValue value) {
            ValueAssigned?.Invoke(this, key, value);

            if (key.TryGetValueAsInteger(out int keyInteger)) {
                _values[keyInteger - 1] = value;
            } else {
                if (!ContainsValue(key)) _values.Add(key);

                _associativeValues ??= new Dictionary<DreamValue, DreamValue>(1);
                _associativeValues[key] = value;
            }
        }

        public void RemoveValue(DreamValue value) {
            int valueIndex = _values.IndexOf(value);

            if (valueIndex != -1) {
                BeforeValueRemoved?.Invoke(this, new DreamValue(valueIndex), _values[valueIndex]);

                _values.RemoveAt(valueIndex);
            }
        }

        public void AddValue(DreamValue value) {
            _values.Add(value);

            ValueAssigned?.Invoke(this, new DreamValue(_values.Count), value);
        }

        //Does not include associations
        public bool ContainsValue(DreamValue value) {
            return _values.Contains(value);
        }

        public bool ContainsKey(DreamValue value)
        {
            return _associativeValues != null && _associativeValues.ContainsKey(value);
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

            _values.RemoveRange(start - 1, end - start);
        }

        public void Insert(int index, DreamValue value) {
            _values.Insert(index - 1, value);
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

        public DreamList Union(DreamList other) {
            DreamList newList = new DreamList();
            newList._values = _values.Union(other.GetValues()).ToList();
            foreach ((DreamValue key, DreamValue value) in other.GetAssociativeValues()) {
                newList.SetValue(key, value);
            }

            return newList;
        }
    }

    // /datum.vars list
    class DreamListVars : DreamList {
        private DreamObject _dreamObject;

        private DreamListVars(DreamObject dreamObject) : base() {
            _dreamObject = dreamObject;
        }

        public static DreamListVars Create(DreamObject dreamObject) {
            var list = new DreamListVars(dreamObject);
            list.InitSpawn(new DreamProcArguments(null));
            return list;
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
