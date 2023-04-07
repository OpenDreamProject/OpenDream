using System.Linq;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamShared.Dream;
using Robust.Shared.Serialization.Manager;

namespace OpenDreamRuntime.Objects {
    delegate void DreamListValueAssignedEventHandler(DreamList list, DreamValue key, DreamValue value);
    delegate void DreamListBeforeValueRemovedEventHandler(DreamList list, DreamValue key, DreamValue value);

    [Virtual]
    public class DreamList : DreamObject {
        internal event DreamListValueAssignedEventHandler? ValueAssigned;
        internal event DreamListBeforeValueRemovedEventHandler? BeforeValueRemoved;

        private List<DreamValue> _values;
        private Dictionary<DreamValue, DreamValue>? _associativeValues;

        public virtual bool IsAssociative => (_associativeValues != null && _associativeValues.Count > 0);

        public DreamList(DreamObjectDefinition listDef, int size) : base(listDef) {
            _values = new List<DreamValue>(size);
        }

        /// <summary>
        /// Create a new DreamList using an existing list of values (does not copy them)
        /// </summary>
        protected DreamList(DreamObjectDefinition listDef, List<DreamValue> values) : base(listDef) {
            _values = values;
        }

        public DreamList CreateCopy(int start = 1, int end = 0) {
            if (start == 0) ++start; //start being 0 and start being 1 are equivalent
            if (end > _values.Count + 1) throw new Exception("list index out of bounds");
            if (end == 0) end = _values.Count + 1;

            DreamList copy = new(ObjectDefinition, end);

            for (int i = start; i < end; i++) {
                DreamValue value = _values[i - 1];

                copy._values.Add(value);
                if (ContainsKey(value)) {
                    copy.SetValue(value, _associativeValues[value]);
                }
            }

            return copy;
        }

        /// <summary>
        /// Returns the list of array values. Doesn't include the associative values indexable by some of these.
        /// </summary>
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
            if (_associativeValues == null)
                return DreamValue.Null;

            return _associativeValues.TryGetValue(key, out DreamValue value) ? value : DreamValue.Null;
        }

        public virtual void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            ValueAssigned?.Invoke(this, key, value);

            if (key.TryGetValueAsInteger(out int keyInteger)) {
                if (allowGrowth && keyInteger == _values.Count + 1) {
                    _values.Add(value);
                } else {
                    _values[keyInteger - 1] = value;
                }
            } else {
                if (!ContainsValue(key)) _values.Add(key);

                _associativeValues ??= new Dictionary<DreamValue, DreamValue>(1);
                _associativeValues[key] = value;
            }
        }

        public void RemoveValue(DreamValue value) {
            int valueIndex = _values.LastIndexOf(value);

            if (valueIndex != -1) {
                BeforeValueRemoved?.Invoke(this, new DreamValue(valueIndex), _values[valueIndex]);

                _values.RemoveAt(valueIndex);
            }
        }

        public virtual void AddValue(DreamValue value) {
            _values.Add(value);

            ValueAssigned?.Invoke(this, new DreamValue(_values.Count), value);
        }

        //Does not include associations
        public virtual bool ContainsValue(DreamValue value) {
            return _values.Contains(value);
        }

        public virtual bool ContainsKey(DreamValue value) {
            return _associativeValues != null && _associativeValues.ContainsKey(value);
        }

        public int FindValue(DreamValue value, int start = 1, int end = 0) {
            if (end == 0 || end > _values.Count) end = _values.Count;

            for (int i = start; i <= end; i++) {
                if (_values[i - 1].Equals(value)) return i;
            }

            return 0;
        }

        public virtual void Cut(int start = 1, int end = 0) {
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

        public virtual int GetLength() {
            return _values.Count;
        }

        public DreamList Union(DreamList other) {
            DreamList newList = new DreamList(ObjectDefinition, _values.Union(other.GetValues()).ToList());
            foreach ((DreamValue key, DreamValue value) in other.GetAssociativeValues()) {
                newList.SetValue(key, value);
            }

            return newList;
        }

        public override string ToString() {
            string assoc = IsAssociative ? ", assoc" : "";
            return $"/list{{len={GetLength()}{assoc}}}";
        }
    }

    // /datum.vars list
    sealed class DreamListVars : DreamList {
        private readonly DreamObject _dreamObject;

        public override bool IsAssociative =>
            true; // We don't use the associative array but, yes, we behave like an associative list

        public DreamListVars(DreamObjectDefinition listDef, DreamObject dreamObject) : base(listDef, 0) {
            _dreamObject = dreamObject;
        }

        public override int GetLength() {
            return _dreamObject.GetVariableNames().Concat(_dreamObject.ObjectDefinition.GlobalVariables.Keys).Count();
        }

        public override List<DreamValue> GetValues() {
            return _dreamObject.GetVariableNames().Concat(_dreamObject.ObjectDefinition.GlobalVariables.Keys).Select(name => new DreamValue(name)).ToList();
        }

        public override bool ContainsKey(DreamValue value) {
            if (!value.TryGetValueAsString(out var varName)) {
                return false;
            }

            return _dreamObject.HasVariable(varName);
        }

        public override bool ContainsValue(DreamValue value) {
            return ContainsKey(value);
        }

        public override DreamValue GetValue(DreamValue key) {
            if (!key.TryGetValueAsString(out var varName)) {
                throw new Exception($"Invalid var index {key}");
            }

            if (!_dreamObject.TryGetVariable(varName, out var objectVar)) {
                throw new Exception(
                    $"Cannot get value of undefined var \"{key}\" on type {_dreamObject.ObjectDefinition.Type}");
            }

            return objectVar;
        }

        public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            if (key.TryGetValueAsString(out var varName)) {
                if (!_dreamObject.HasVariable(varName)) {
                    throw new Exception(
                        $"Cannot set value of undefined var \"{varName}\" on type {_dreamObject.ObjectDefinition.Type}");
                }

                _dreamObject.SetVariable(varName, value);
            } else {
                throw new Exception($"Invalid var index {key}");
            }
        }
    }

    // global.vars list
    sealed class DreamGlobalVars : DreamList {
        [Dependency] private readonly IDreamManager _dreamMan = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;

        public override bool IsAssociative =>
            true; // We don't use the associative array but, yes, we behave like an associative list

        public DreamGlobalVars(DreamObjectDefinition listDef) : base(listDef, 0) {
            IoCManager.InjectDependencies(this);
        }

        public override List<DreamValue> GetValues() {
            var root = _objectTree.Root.ObjectDefinition;
            List<DreamValue> values = new List<DreamValue>(root.GlobalVariables.Keys.Count - 1);
            // Skip world
            foreach (var key in root.GlobalVariables.Keys.Skip(1)) {
                values.Add(new DreamValue(key));
            }

            return values;
        }

        public override bool ContainsKey(DreamValue value) {
            if (!value.TryGetValueAsString(out var varName)) {
                return false;
            }

            return _objectTree.Root.ObjectDefinition.GlobalVariables.ContainsKey(varName);
        }

        public override bool ContainsValue(DreamValue value) {
            return ContainsKey(value);
        }

        public override DreamValue GetValue(DreamValue key) {
            if (!key.TryGetValueAsString(out var varName)) {
                throw new Exception($"Invalid var index {key}");
            }

            var root = _objectTree.Root.ObjectDefinition;
            if (!root.GlobalVariables.TryGetValue(varName, out var globalId)) {
                throw new Exception($"Invalid global {varName}");
            }

            return _dreamMan.Globals[globalId];
        }

        public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            if (key.TryGetValueAsString(out var varName)) {
                var root = _objectTree.Root.ObjectDefinition;
                if (!root.GlobalVariables.TryGetValue(varName, out var globalId)) {
                    throw new Exception($"Cannot set value of undefined global \"{varName}\"");
                }

                _dreamMan.Globals[globalId] = value;
            } else {
                throw new Exception($"Invalid var index {key}");
            }
        }
    }

    // verbs list
    // Keeps track of an atom's or client's verbs
    public sealed class VerbsList : DreamList {
        private readonly List<DreamProc> _verbs = new();

        public VerbsList(IDreamObjectTree objectTree, DreamObject dreamObject) : base(objectTree.List.ObjectDefinition, 0) {
            List<int>? verbs = dreamObject.ObjectDefinition.Verbs;
            if (verbs == null)
                return;

            _verbs.EnsureCapacity(verbs.Count);
            foreach (int verbId in verbs) {
                _verbs.Add(objectTree.Procs[verbId]);
            }
        }

        public override DreamValue GetValue(DreamValue key) {
            if (!key.TryGetValueAsInteger(out var index))
                throw new Exception($"Invalid index into verbs list: {key}");
            if (index < 1 || index > _verbs.Count)
                throw new Exception($"Out of bounds index on verbs list: {index}");

            return new DreamValue(_verbs[index - 1]);
        }

        public override List<DreamValue> GetValues() {
            List<DreamValue> values = new(_verbs.Count);

            foreach (DreamProc verb in _verbs)
                values.Add(new(verb));

            return values;
        }

        public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            throw new Exception("Cannot set the values of a verbs list");
        }

        public override void AddValue(DreamValue value) {
            if (!value.TryGetValueAsProc(out var verb))
                throw new Exception($"Cannot add {value} to verbs list");

            _verbs.Add(verb);
        }

        public override void Cut(int start = 1, int end = 0) {
            int verbCount = _verbs.Count + 1;
            if (end == 0 || end > verbCount) end = verbCount;

            _verbs.RemoveRange(start - 1, end - start);
        }

        public override int GetLength() {
            return _verbs.Count;
        }
    }

    // atom.filters list
    // Operates on an atom's appearance
    public sealed class DreamFilterList : DreamList {
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private readonly DreamObject _atom;

        public DreamFilterList(DreamObjectDefinition listDef, DreamObject atom) : base(listDef, 0) {
            IoCManager.InjectDependencies(this);
            _atom = atom;
        }

        public override void Cut(int start = 1, int end = 0) {
            _atomManager.UpdateAppearance(_atom, appearance => {
                int filterCount = appearance.Filters.Count + 1;
                if (end == 0 || end > filterCount) end = filterCount;

                appearance.Filters.RemoveRange(start - 1, end - start);
            });
        }

        public int GetIndexOfFilter(DreamFilter filter) {
            IconAppearance appearance = GetAppearance();

            return appearance.Filters.IndexOf(filter) + 1;
        }

        public void SetFilter(int index, DreamFilter filter) {
            IconAppearance appearance = GetAppearance();
            if (index < 1 || index > appearance.Filters.Count)
                throw new Exception($"Cannot index {index} on filter list");


            _atomManager.UpdateAppearance(_atom, appearance => {
                DreamFilter oldFilter = appearance.Filters[index - 1];

                DreamMetaObjectFilter.FilterAttachedTo.Remove(oldFilter);
                appearance.Filters[index - 1] = filter;
                DreamMetaObjectFilter.FilterAttachedTo[filter] = this;
            });
        }

        public override DreamValue GetValue(DreamValue key) {
            if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
                throw new Exception($"Invalid index into filter list: {key}");

            IconAppearance appearance = GetAppearance();
            if (filterIndex > appearance.Filters.Count)
                throw new Exception($"Atom only has {appearance.Filters.Count} filter(s), cannot index {filterIndex}");

            DreamFilter filter = appearance.Filters[filterIndex - 1];
            DreamObject filterObject = _objectTree.CreateObject(_objectTree.Filter);
            DreamMetaObjectFilter.DreamObjectToFilter[filterObject] = filter;
            return new DreamValue(filterObject);
        }

        public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            if (!value.TryGetValueAsDreamObjectOfType(_objectTree.Filter, out var filterObject))
                throw new Exception($"Cannot set value of filter list to {value}");
            if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
                throw new Exception($"Invalid index into filter list: {key}");

            DreamFilter filter = DreamMetaObjectFilter.DreamObjectToFilter[filterObject];
            SetFilter(filterIndex, filter);
        }

        public override void AddValue(DreamValue value) {
            if (!value.TryGetValueAsDreamObjectOfType(_objectTree.Filter, out var filterObject))
                throw new Exception($"Cannot add {value} to filter list");

            //This is dynamic to prevent the compiler from optimising the SerializationManager.CreateCopy() call to the DreamFilter type
            //so we can preserve the subclass information. Setting it to DreamFilter instead will cause filter parameters to stop working.
            dynamic filter = DreamMetaObjectFilter.DreamObjectToFilter[filterObject];
            DreamFilter copy = _serializationManager.CreateCopy(filter, notNullableOverride: true); // Adding a filter creates a copy

            DreamMetaObjectFilter.FilterAttachedTo[copy] = this;
            _atomManager.UpdateAppearance(_atom, appearance => {
                // If this appearance already has a color filter (from /atom.color)
                if(copy is DreamFilterColor && appearance.SillyColorFilter is not null) {
                    // This is to support an edge case where an atom's .color var has been set to a matrix, and then it also gets a color filter.
                    // In BYOND, this apparently causes a crash or something, depending on the order.
                    // Lets just... try to support it though, huh? :^)
                    appearance.SillyColorFilter = null;
                }
                appearance.Filters.Add(copy);
            });
        }

        public override int GetLength() {
            return GetAppearance().Filters.Count;
        }

        private IconAppearance GetAppearance() {
            IconAppearance? appearance = _atomManager.MustGetAppearance(_atom);
            if (appearance == null)
                throw new Exception("Atom has no appearance");

            return appearance;
        }
    }

    // world.contents list
    // Operates on a list of all atoms
    public sealed class WorldContentsList : DreamList {
        private readonly IDreamMapManager _mapManager;

        public WorldContentsList(DreamObjectDefinition listDef, IDreamMapManager mapManager) : base(listDef, 0) {
            _mapManager = mapManager;
        }

        public override DreamValue GetValue(DreamValue key) {
            if (!key.TryGetValueAsInteger(out var index))
                throw new Exception($"Invalid index into world contents list: {key}");
            if (index < 1 || index > _mapManager.AllAtoms.Count)
                throw new Exception($"Out of bounds index on world contents list: {index}");

            return new DreamValue(_mapManager.AllAtoms[index - 1]);
        }

        public override List<DreamValue> GetValues() {
            throw new NotImplementedException("Getting all values of the world contents list is not implemented");
        }

        public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            throw new Exception("Cannot set the value of world contents list");
        }

        public override void AddValue(DreamValue value) {
            throw new Exception("Cannot append to world contents list");
        }

        public override void Cut(int start = 1, int end = 0) {
            throw new Exception("Cannot cut world contents list");
        }

        public override int GetLength() {
            return _mapManager.AllAtoms.Count;
        }
    }

    // turf.contents list
    public sealed class TurfContentsList : DreamList {
        private readonly IDreamObjectTree _objectTree;
        private readonly IDreamMapManager.Cell _cell;

        public TurfContentsList(DreamObjectDefinition listDef, IDreamObjectTree objectTree, IDreamMapManager.Cell cell) : base(listDef, 0) {
            _objectTree = objectTree;
            _cell = cell;
        }

        public override DreamValue GetValue(DreamValue key) {
            if (!key.TryGetValueAsInteger(out var index))
                throw new Exception($"Invalid index into turf contents list: {key}");
            if (index < 1 || index > _cell.Movables.Count)
                throw new Exception($"Out of bounds index on turf contents list: {index}");

            return new DreamValue(_cell.Movables[index - 1]);
        }

        // TODO: This would preferably be an IEnumerable<> method. Probably as part of #985.
        public override List<DreamValue> GetValues() {
            List<DreamValue> values = new(_cell.Movables.Count);

            foreach (var movable in _cell.Movables) {
                values.Add(new(movable));
            }

            return values;
        }

        public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
            throw new Exception("Cannot set an index of turf contents list");
        }

        public override void AddValue(DreamValue value) {
            if (!value.TryGetValueAsDreamObjectOfType(_objectTree.Movable, out var movable))
                throw new Exception($"Cannot add {value} to turf contents");

            movable.SetVariable("loc", new(_cell.Turf));
        }

        public override void Cut(int start = 1, int end = 0) {
            int movableCount = _cell.Movables.Count + 1;
            if (end == 0 || end > movableCount) end = movableCount;

            for (int i = start; i < end; i++) {
                _cell.Movables[i - 1].SetVariable("loc", DreamValue.Null);
            }
        }

        public override int GetLength() {
            return _cell.Movables.Count;
        }
    }
}
