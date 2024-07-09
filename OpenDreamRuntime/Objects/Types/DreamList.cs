using System.Linq;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameStates;
using Robust.Shared.Serialization.Manager;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamList : DreamObject {
    private readonly List<DreamValue> _values;
    private Dictionary<DreamValue, DreamValue>? _associativeValues;

    public override bool ShouldCallNew => false;

    public virtual bool IsAssociative => (_associativeValues != null && _associativeValues.Count > 0);

    public DreamList(DreamObjectDefinition listDef, int size) : base(listDef) {
        _values = new List<DreamValue>(size);
    }

    /// <summary>
    /// Create a new DreamList using an existing list of values (does not copy them)
    /// </summary>
    public DreamList(DreamObjectDefinition listDef, List<DreamValue> values, Dictionary<DreamValue, DreamValue>? associativeValues) : base(listDef) {
        _values = values;
        _associativeValues = associativeValues;
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        // Named arguments are ignored
        if (args.Count == 1 && args.GetArgument(0).TryGetValueAsInteger(out int size)) {
            Resize(size);
        } else if (args.Count > 1) {
            DreamList[] lists = { this };

            int dimensions = args.Count;
            for (int argIndex = 0; argIndex < dimensions; argIndex++) {
                DreamValue arg = args.GetArgument(argIndex);
                arg.TryGetValueAsInteger(out size);

                DreamList[] newLists = null;
                if (argIndex < dimensions) {
                    newLists = new DreamList[size * lists.Length];
                }

                for (int i = 0; i < lists.Length; i++) {
                    DreamList list = lists[i];

                    for (int j = 0; j < size; j++) {
                        if (argIndex < dimensions - 1) {
                            DreamList newList = ObjectTree.CreateList();

                            list.AddValue(new DreamValue(newList));
                            newLists[i * size + j] = newList;
                        } else {
                            list.AddValue(DreamValue.Null);
                        }
                    }
                }

                lists = newLists;
            }
        }
    }

    public DreamList CreateCopy(int start = 1, int end = 0) {
        if (start == 0) ++start; //start being 0 and start being 1 are equivalent

        var values = GetValues();
        if (end > values.Count + 1 || start > values.Count + 1) throw new Exception("list index out of bounds");
        if (end == 0) end = values.Count + 1;
        if (end <= start)
            return new(ObjectDefinition, 0);

        List<DreamValue> copyValues = values.GetRange(start - 1, end - start);

        Dictionary<DreamValue, DreamValue>? associativeValues = null;
        if (_associativeValues != null) {
            associativeValues = new(end - start);
            foreach (var key in copyValues) {
                if (_associativeValues.TryGetValue(key, out var value))
                    associativeValues[key] = value;
            }
        }

        return new(ObjectDefinition, copyValues, associativeValues);
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

    public virtual void RemoveValue(DreamValue value) {
        int valueIndex = _values.LastIndexOf(value);

        if (valueIndex != -1) {
            _associativeValues?.Remove(value);
            _values.RemoveAt(valueIndex);
        }
    }

    public virtual void AddValue(DreamValue value) {
        _values.Add(value);
    }

    //Does not include associations
    public virtual bool ContainsValue(DreamValue value) {
        for (int i = 0; i < _values.Count; i++) {
            if (_values[i].Equals(value))
                return true;
        }

        return false;
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

        if (_associativeValues != null) {
            for (int i = start; i < end; i++)
                _associativeValues.Remove(_values[i - 1]);
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
            _values.EnsureCapacity(size);

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
        DreamList newList = new DreamList(ObjectDefinition, _values.Union(other.GetValues()).ToList(), null);
        foreach ((DreamValue key, DreamValue value) in other.GetAssociativeValues()) {
            newList.SetValue(key, value);
        }

        return newList;
    }

    public override string ToString() {
        string assoc = IsAssociative ? ", assoc" : "";
        return $"/list{{len={GetLength()}{assoc}}}";
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        if (varName == "len") {
            value = new(GetLength());
            return true;
        }

        // Note that invalid vars on /list will give null and not error in BYOND
        // We don't replicate that
        return base.TryGetVar(varName, out value);
    }

    protected override void SetVar(string varName, DreamValue value) {
        if (varName == "len") {
            value.TryGetValueAsInteger(out var newLen);

            Resize(newLen);
        } else {
            base.SetVar(varName, value);
        }
    }

    #region Operators
    public override DreamValue OperatorIndex(DreamValue index) {
        return GetValue(index);
    }

    public override void OperatorIndexAssign(DreamValue index, DreamValue value) {
        SetValue(index, value);
    }

    public override ProcStatus OperatorAdd(DreamValue b, DMProcState state, out DreamValue result) {
        DreamList listCopy = CreateCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.GetValues()) {
                if (bList._associativeValues?.TryGetValue(value, out var assocValue) is true) {
                    listCopy.SetValue(value, assocValue);
                } else {
                    listCopy.AddValue(value);
                }
            }
        } else {
            listCopy.AddValue(b);
        }

        result = new DreamValue(listCopy);
        return ProcStatus.Continue;
    }

    public override ProcStatus OperatorSubtract(DreamValue b, DMProcState state, out DreamValue result) {
        DreamList listCopy = CreateCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.GetValues()) {
                listCopy.RemoveValue(value);
            }
        } else {
            listCopy.RemoveValue(b);
        }

        result = new DreamValue(listCopy);
        return ProcStatus.Continue;
    }

    public override ProcStatus OperatorOr(DreamValue b, DMProcState state, out DreamValue result) {
        DreamList list;

        if (b.TryGetValueAsDreamList(out var bList)) {  // List | List
            list = Union(bList);
        } else {                                        // List | x
            list = CreateCopy();
            list.AddValue(b);
        }

        result = new DreamValue(list);
        return ProcStatus.Continue;
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.GetValues()) {
                if (bList._associativeValues?.TryGetValue(value, out var assocValue) is true) {
                    SetValue(value, assocValue);
                } else {
                    AddValue(value);
                }
            }
        } else {
            AddValue(b);
        }

        return new(this);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            DreamValue[] values = bList.GetValues().ToArray();

            foreach (DreamValue value in values) {
                RemoveValue(value);
            }
        } else {
            RemoveValue(b);
        }

        return new(this);
    }

    public override DreamValue OperatorCombine(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.GetValues()) {
                if (ContainsValue(value))
                    continue;

                if (bList._associativeValues?.TryGetValue(value, out var associatedValue) is true)
                    SetValue(value, associatedValue);
                else
                    AddValue(value);
            }
        } else if (!ContainsValue(b)) {
            AddValue(b);
        }

        return new(this);
    }

    public override DreamValue OperatorMask(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            for (int i = 1; i <= GetLength(); i++) {
                if (!bList.ContainsValue(GetValue(new DreamValue(i)))) {
                    Cut(i, i + 1);
                    i--;
                }
            }
        } else {
            for (int i = 1; i <= GetLength(); i++) {
                if (GetValue(new DreamValue(i)) != b) {
                    Cut(i, i + 1);
                    i--;
                }
            }
        }

        return new(this);
    }

    public override DreamValue OperatorEquivalent(DreamValue b) {
        if (!b.TryGetValueAsDreamList(out var secondList))
            return DreamValue.False;
        if (GetLength() != secondList.GetLength())
            return DreamValue.False;

        var firstValues = GetValues();
        var secondValues = secondList.GetValues();
        for (var i = 0; i < firstValues.Count; i++) {
            if (!firstValues[i].Equals(secondValues[i]))
                return DreamValue.False;
        }

        return DreamValue.True;
    }
    #endregion Operators
}

// /datum.vars list
internal sealed class DreamListVars(DreamObjectDefinition listDef, DreamObject dreamObject) : DreamList(listDef, 0) {
    public readonly DreamObject DreamObject = dreamObject;

    public override bool IsAssociative =>
        true; // We don't use the associative array but, yes, we behave like an associative list

    public override int GetLength() {
        return DreamObject.GetVariableNames().Concat(DreamObject.ObjectDefinition.GlobalVariables.Keys).Count();
    }

    public override List<DreamValue> GetValues() {
        return DreamObject.GetVariableNames().Concat(DreamObject.ObjectDefinition.GlobalVariables.Keys).Select(name => new DreamValue(name)).ToList();
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsString(out var varName)) {
            return false;
        }

        return DreamObject.HasVariable(varName);
    }

    public override bool ContainsValue(DreamValue value) {
        return ContainsKey(value);
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsString(out var varName)) {
            throw new Exception($"Invalid var index {key}");
        }

        if (!DreamObject.TryGetVariable(varName, out var objectVar)) {
            throw new Exception(
                $"Cannot get value of undefined var \"{key}\" on type {DreamObject.ObjectDefinition.Type}");
        }

        return objectVar;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsString(out var varName)) {
            if (!DreamObject.HasVariable(varName)) {
                throw new Exception(
                    $"Cannot set value of undefined var \"{varName}\" on type {DreamObject.ObjectDefinition.Type}");
            }

            DreamObject.SetVariable(varName, value);
        } else {
            throw new Exception($"Invalid var index {key}");
        }
    }

    public override bool IsSaved(string name) {
        return DreamObject.IsSaved(name);
    }
}

// global.vars list
sealed class DreamGlobalVars : DreamList {
    [Dependency] private readonly DreamManager _dreamMan = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;

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

// client.verbs list
// Keeps track of a client's verbs
public sealed class ClientVerbsList : DreamList {
    public readonly List<DreamProc> Verbs = new();

    private readonly DreamObjectClient _client;
    private readonly ServerVerbSystem? _verbSystem;

    public ClientVerbsList(DreamObjectTree objectTree, ServerVerbSystem? verbSystem, DreamObjectClient client) : base(objectTree.List.ObjectDefinition, 0) {
        _client = client;
        _verbSystem = verbSystem;

        List<int>? verbs = _client.ObjectDefinition.Verbs;
        if (verbs == null)
            return;

        Verbs.EnsureCapacity(verbs.Count);
        foreach (int verbId in verbs) {
            Verbs.Add(objectTree.Procs[verbId]);
        }
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index into verbs list: {key}");
        if (index < 1 || index > Verbs.Count)
            throw new Exception($"Out of bounds index on verbs list: {index}");

        return new DreamValue(Verbs[index - 1]);
    }

    public override List<DreamValue> GetValues() {
        List<DreamValue> values = new(Verbs.Count);

        foreach (DreamProc verb in Verbs)
            values.Add(new(verb));

        return values;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot set the values of a verbs list");
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            throw new Exception($"Cannot add {value} to verbs list");
        if (Verbs.Contains(verb))
            return; // Even += won't add the verb if it's already in this list

        Verbs.Add(verb);
        _verbSystem?.RegisterVerb(verb);
        _verbSystem?.UpdateClientVerbs(_client);
    }

    public override void Cut(int start = 1, int end = 0) {
        int verbCount = Verbs.Count + 1;
        if (end == 0 || end > verbCount) end = verbCount;

        Verbs.RemoveRange(start - 1, end - start);
        _verbSystem?.UpdateClientVerbs(_client);
    }

    public override int GetLength() {
        return Verbs.Count;
    }
}

// atom's verbs list
// Keeps track of an appearance's verbs (atom.verbs, mutable_appearance.verbs, etc)
public sealed class VerbsList(DreamObjectTree objectTree, AtomManager atomManager, ServerVerbSystem? verbSystem, DreamObjectAtom atom) : DreamList(objectTree.List.ObjectDefinition, 0) {
   public override DreamValue GetValue(DreamValue key) {
        if (verbSystem == null)
           return DreamValue.Null;
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index into verbs list: {key}");

        var verbs = GetVerbs();
        if (index < 1 || index > verbs.Count)
            throw new Exception($"Out of bounds index on verbs list: {index}");

        return new DreamValue(verbSystem.GetVerb(verbs[index - 1]));
    }

    public override List<DreamValue> GetValues() {
        var appearance = atomManager.MustGetAppearance(atom);
        if (appearance == null || verbSystem == null)
            return new List<DreamValue>();

        var values = new List<DreamValue>(appearance.Verbs.Count);

        foreach (var verbId in appearance.Verbs) {
            var verb = verbSystem.GetVerb(verbId);

            values.Add(new(verb));
        }

        return values;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot set the values of a verbs list");
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            throw new Exception($"Cannot add {value} to verbs list");

        atomManager.UpdateAppearance(atom, appearance => {
            if (!verb.VerbId.HasValue)
                verbSystem?.RegisterVerb(verb);
            if (!verb.VerbId.HasValue || appearance.Verbs.Contains(verb.VerbId.Value))
                return; // Even += won't add the verb if it's already in this list

            appearance.Verbs.Add(verb.VerbId.Value);
        });
    }

    public override void Cut(int start = 1, int end = 0) {
        atomManager.UpdateAppearance(atom, appearance => {
            int count = appearance.Verbs.Count + 1;
            if (end == 0 || end > count) end = count;

            appearance.Verbs.RemoveRange(start - 1, end - start);
        });
    }

    public override int GetLength() {
        return GetVerbs().Count;
    }

    private List<int> GetVerbs() {
        IconAppearance? appearance = atomManager.MustGetAppearance(atom);
        if (appearance == null)
            throw new Exception("Atom has no appearance");

        return appearance.Verbs;
    }
}

// atom.overlays or atom.underlays list
// Operates on an object's appearance
public sealed class DreamOverlaysList : DreamList {
    [Dependency] private readonly AtomManager _atomManager = default!;
    private readonly ServerAppearanceSystem? _appearanceSystem;

    private readonly DreamObject _owner;
    private readonly bool _isUnderlays;

    public DreamOverlaysList(DreamObjectDefinition listDef, DreamObject owner, ServerAppearanceSystem? appearanceSystem, bool isUnderlays) : base(listDef, 0) {
        IoCManager.InjectDependencies(this);

        _owner = owner;
        _appearanceSystem = appearanceSystem;
        _isUnderlays = isUnderlays;
    }

    public override List<DreamValue> GetValues() {
        var appearance = _atomManager.MustGetAppearance(_owner);
        if (appearance == null || _appearanceSystem == null)
            return new List<DreamValue>();

        var overlays = GetOverlaysList(appearance);
        var values = new List<DreamValue>(overlays.Count);

        foreach (var overlay in overlays) {
            var overlayAppearance = _appearanceSystem.MustGetAppearance(overlay);

            values.Add(new(overlayAppearance));
        }

        return values;
    }

    public override void Cut(int start = 1, int end = 0) {
        _atomManager.UpdateAppearance(_owner, appearance => {
            List<int> overlaysList = GetOverlaysList(appearance);
            int count = overlaysList.Count + 1;
            if (end == 0 || end > count) end = count;

            overlaysList.RemoveRange(start - 1, end - start);
        });
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var overlayIndex) || overlayIndex < 1)
            throw new Exception($"Invalid index into {(_isUnderlays ? "underlays" : "overlays")} list: {key}");

        IconAppearance appearance = GetAppearance();
        List<int> overlaysList = GetOverlaysList(appearance);
        if (overlayIndex > overlaysList.Count)
            throw new Exception($"Atom only has {overlaysList.Count} {(_isUnderlays ? "underlay" : "overlay")}(s), cannot index {overlayIndex}");

        if (_appearanceSystem == null)
            return DreamValue.Null;

        int overlayId = GetOverlaysList(appearance)[overlayIndex - 1];
        IconAppearance overlayAppearance = _appearanceSystem.MustGetAppearance(overlayId);
        return new DreamValue(overlayAppearance);
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception($"Cannot write to an index of an {(_isUnderlays ? "underlays" : "overlays")} list");
    }

    public override void AddValue(DreamValue value) {
        if (_appearanceSystem == null)
            return;

        _atomManager.UpdateAppearance(_owner, appearance => {
            IconAppearance? overlayAppearance = CreateOverlayAppearance(_atomManager, value, appearance.Icon);
            overlayAppearance ??= new IconAppearance();

            GetOverlaysList(appearance).Add(_appearanceSystem.AddAppearance(overlayAppearance));
        });
    }

    public override void RemoveValue(DreamValue value) {
        if (_appearanceSystem == null)
            return;

        _atomManager.UpdateAppearance(_owner, appearance => {
            IconAppearance? overlayAppearance = CreateOverlayAppearance(_atomManager, value, appearance.Icon);
            if (overlayAppearance == null || !_appearanceSystem.TryGetAppearanceId(overlayAppearance, out var id))
                return;

            GetOverlaysList(appearance).Remove(id);
        });
    }

    public override int GetLength() {
        return GetOverlaysList(GetAppearance()).Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<int> GetOverlaysList(IconAppearance appearance) =>
        _isUnderlays ? appearance.Underlays : appearance.Overlays;

    private IconAppearance GetAppearance() {
        IconAppearance? appearance = _atomManager.MustGetAppearance(_owner);
        if (appearance == null)
            throw new Exception("Atom has no appearance");

        return appearance;
    }

    public static IconAppearance? CreateOverlayAppearance(AtomManager atomManager, DreamValue value, int? defaultIcon) {
        IconAppearance overlay;

        if (value.TryGetValueAsString(out var iconState)) {
            overlay = new IconAppearance() {
                IconState = iconState
            };
        } else if (atomManager.TryCreateAppearanceFrom(value, out var overlayAppearance)) {
            overlay = overlayAppearance;
        } else {
            return null; // Not a valid overlay
        }

        overlay.Icon ??= defaultIcon;
        return overlay;
    }
}

// atom.vis_contents list
// Operates on an atom's appearance
public sealed class DreamVisContentsList : DreamList {
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly PvsOverrideSystem? _pvsOverrideSystem;

    private readonly List<DreamObjectAtom> _visContents = new();
    private readonly DreamObject _atom;

    public DreamVisContentsList(DreamObjectDefinition listDef, PvsOverrideSystem? pvsOverrideSystem, DreamObject atom) : base(listDef, 0) {
        IoCManager.InjectDependencies(this);

        _pvsOverrideSystem = pvsOverrideSystem;
        _atom = atom;
    }

    public override List<DreamValue> GetValues() {
        var values = new List<DreamValue>(_visContents.Count);

        foreach (var visContent in _visContents) {
            values.Add(new(visContent));
        }

        return values;
    }

    public override void Cut(int start = 1, int end = 0) {
        int count = _visContents.Count + 1;
        if (end == 0 || end > count) end = count;

        _visContents.RemoveRange(start - 1, end - start);
        _atomManager.UpdateAppearance(_atom, appearance => {
            appearance.VisContents.RemoveRange(start - 1, end - start);
        });
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var visContentsIndex) || visContentsIndex < 1)
            throw new Exception($"Invalid index into vis_contents list: {key}");
        if (visContentsIndex > _visContents.Count)
            throw new Exception($"Atom only has {_visContents.Count} vis_contents element(s), cannot index {visContentsIndex}");

        return new DreamValue(_visContents[visContentsIndex - 1]);
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot write to an index of a vis_contents list");
    }

    public override void AddValue(DreamValue value) {
        EntityUid entity;
        if (value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable)) {
            if(_visContents.Contains(movable))
                return; // vis_contents cannot contain duplicates
            _visContents.Add(movable);
            entity = movable.Entity;
        } else if (value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
            if(_visContents.Contains(turf))
                return; // vis_contents cannot contain duplicates
            _visContents.Add(turf);
            entity = EntityUid.Invalid; // TODO: Support turfs in vis_contents
        } else {
            throw new Exception($"Cannot add {value} to a vis_contents list");
        }

        // TODO: Only override the entity's visibility if its parent atom is visible
        if (entity != EntityUid.Invalid)
            _pvsOverrideSystem?.AddGlobalOverride(entity);

        _atomManager.UpdateAppearance(_atom, appearance => {
            // Add even an invalid UID to keep this and _visContents in sync
            appearance.VisContents.Add(_entityManager.GetNetEntity(entity));
        });
    }

    public override void RemoveValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        _visContents.Remove(movable);
        _atomManager.UpdateAppearance(_atom, appearance => {
            appearance.VisContents.Remove(_entityManager.GetNetEntity(movable.Entity));
        });
    }

    public override int GetLength() {
        return _visContents.Count;
    }
}

// atom.filters list
// Operates on an object's appearance
public sealed class DreamFilterList : DreamList {
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    private readonly DreamObject _owner;

    public DreamFilterList(DreamObjectDefinition listDef, DreamObject owner) : base(listDef, 0) {
        IoCManager.InjectDependencies(this);
        _owner = owner;
    }

    public override void Cut(int start = 1, int end = 0) {
        _atomManager.UpdateAppearance(_owner, appearance => {
            int filterCount = appearance.Filters.Count + 1;
            if (end == 0 || end > filterCount) end = filterCount;

            appearance.Filters.RemoveRange(start - 1, end - start);
        });
    }

    public int GetIndexOfFilter(DreamFilter filter) {
        IconAppearance appearance = GetAppearance();

        return appearance.Filters.IndexOf(filter) + 1;
    }

    public void SetFilter(int index, DreamFilter? filter) {
        _atomManager.UpdateAppearance(_owner, appearance => {
            if (index < 1 || index > appearance.Filters.Count)
                throw new Exception($"Cannot index {index} on filter list");

            DreamFilter oldFilter = appearance.Filters[index - 1];

            DreamObjectFilter.FilterAttachedTo.Remove(oldFilter);

            if (filter == null) { // Setting an index to null is the same as removing it ("filters[1] = null")
                appearance.Filters.RemoveAt(index - 1);
            } else {
                appearance.Filters[index - 1] = filter;
                DreamObjectFilter.FilterAttachedTo[filter] = this;
            }
        });
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
            throw new Exception($"Invalid index into filter list: {key}");

        IconAppearance appearance = GetAppearance();
        if (filterIndex > appearance.Filters.Count)
            throw new Exception($"Atom only has {appearance.Filters.Count} filter(s), cannot index {filterIndex}");

        DreamFilter filter = appearance.Filters[filterIndex - 1];
        DreamObjectFilter filterObject = ObjectTree.CreateObject<DreamObjectFilter>(ObjectTree.Filter);
        filterObject.Filter = filter;
        return new DreamValue(filterObject);
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject) &&!value.IsNull)
            throw new Exception($"Cannot set value of filter list to {value}");
        if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
            throw new Exception($"Invalid index into filter list: {key}");

        SetFilter(filterIndex, filterObject?.Filter);
    }

    public override void AddValue(DreamValue value) {
        if (value.IsNull) // "filters += null" is just ignored
            return;
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject))
            throw new Exception($"Cannot add {value} to filter list");

        //This is dynamic to prevent the compiler from optimising the SerializationManager.CreateCopy() call to the DreamFilter type
        //so we can preserve the subclass information. Setting it to DreamFilter instead will cause filter parameters to stop working.
        dynamic filter = filterObject.Filter;
        DreamFilter copy = _serializationManager.CreateCopy(filter, notNullableOverride: true); // Adding a filter creates a copy

        DreamObjectFilter.FilterAttachedTo[copy] = this;
        _atomManager.UpdateAppearance(_owner, appearance => {
            appearance.Filters.Add(copy);
        });
    }

    public override int GetLength() {
        return GetAppearance().Filters.Count;
    }

    private IconAppearance GetAppearance() {
        IconAppearance? appearance = _atomManager.MustGetAppearance(_owner);
        if (appearance == null)
            throw new Exception("Atom has no appearance");

        return appearance;
    }
}

// client.screen list
public sealed class ClientScreenList : DreamList {
    private readonly DreamObjectTree _objectTree;
    private readonly ServerScreenOverlaySystem? _screenOverlaySystem;

    private readonly DreamConnection _connection;
    private readonly List<DreamValue> _screenObjects = new();

    public ClientScreenList(DreamObjectTree objectTree, ServerScreenOverlaySystem? screenOverlaySystem, DreamConnection connection) : base(objectTree.List.ObjectDefinition, 0) {
        _objectTree = objectTree;
        _screenOverlaySystem = screenOverlaySystem;

        _connection = connection;
    }

    public override bool ContainsValue(DreamValue value) {
        return _screenObjects.Contains(value);
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var screenIndex) || screenIndex < 1 || screenIndex > _screenObjects.Count)
            throw new Exception($"Invalid index into screen list: {key}");

        return _screenObjects[screenIndex - 1];
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot write to an index of a screen list");
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        _screenOverlaySystem?.AddScreenObject(_connection, movable);
        _screenObjects.Add(value);
    }

    public override void RemoveValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        _screenOverlaySystem?.RemoveScreenObject(_connection, movable);
        _screenObjects.Remove(value);
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > _screenObjects.Count + 1) end = _screenObjects.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            if (!_screenObjects[i].TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
                continue;

            _screenOverlaySystem?.RemoveScreenObject(_connection, movable);
        }

        _screenObjects.RemoveRange(start - 1, end - start);
    }

    public override int GetLength() {
        return _screenObjects.Count;
    }
}


// client.images list
public sealed class ClientImagesList : DreamList {
    private readonly ServerClientImagesSystem? _clientImagesSystem;
    private readonly DreamConnection _connection;
    private readonly List<DreamValue> _imageObjects = new();

    public ClientImagesList(DreamObjectTree objectTree, ServerClientImagesSystem? clientImagesSystem, DreamConnection connection) : base(objectTree.List.ObjectDefinition, 0) {
        _clientImagesSystem = clientImagesSystem;
        _connection = connection;
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var imageIndex) || imageIndex < 1 || imageIndex > _imageObjects.Count)
            throw new Exception($"Invalid index into client images list: {key}");

        return _imageObjects[imageIndex - 1];
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot write to an index of a client images list");
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        _clientImagesSystem?.AddImageObject(_connection, image);
        _imageObjects.Add(value);
    }

    public override void RemoveValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        _clientImagesSystem?.RemoveImageObject(_connection, image);
        _imageObjects.Remove(value);
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > _imageObjects.Count + 1) end = _imageObjects.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            if (!_imageObjects[i].TryGetValueAsDreamObject<DreamObjectImage>(out var image))
                continue;

            _clientImagesSystem?.RemoveImageObject(_connection, image);
        }

        _imageObjects.RemoveRange(start - 1, end - start);
    }

    public override int GetLength() {
        return _imageObjects.Count;
    }
}

// world.contents list
// Operates on a list of all atoms
public sealed class WorldContentsList : DreamList {
    private readonly AtomManager _atomManager;

    public WorldContentsList(DreamObjectDefinition listDef, AtomManager atomManager) : base(listDef, 0) {
        _atomManager = atomManager;
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index into world contents list: {key}");
        if (index < 1 || index > _atomManager.AtomCount)
            throw new Exception($"Out of bounds index on world contents list: {index}");

        var element = _atomManager.EnumerateAtoms().ElementAt(index - 1); // Ouch
        return new DreamValue(element);
    }

    public override List<DreamValue> GetValues() {
        return AtomManager.EnumerateAtoms().Select(atom => new DreamValue(atom)).ToList();
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
        return _atomManager.AtomCount;
    }
}

// turf.contents list
public sealed class TurfContentsList : DreamList {
    private readonly IDreamMapManager.Cell _cell;

    public TurfContentsList(DreamObjectDefinition listDef, IDreamMapManager.Cell cell) : base(listDef, 0) {
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
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
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

// area.contents list
public sealed class AreaContentsList : DreamList {
    private readonly DreamObjectArea _area;
    private readonly List<DreamObjectTurf> _turfs = new();

    public AreaContentsList(DreamObjectDefinition listDef, DreamObjectArea area) : base(listDef, 0) {
        _area = area;
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index into area contents list: {key}");

        foreach (var turf in _turfs) {
            if (index < 1)
                break;

            if (index == 1) // The index references this turf
                return new(turf);

            index -= 1;

            int contentsLength = turf.Contents.GetLength();

            if (index <= contentsLength) // The index references one of the turf's contents
                return turf.Contents.GetValue(new(index));

            index -= contentsLength;
        }

        throw new Exception($"Out of bounds index on turf contents list: {key}");
    }

    public override List<DreamValue> GetValues() {
        List<DreamValue> values = new(_turfs.Count);

        foreach (var turf in _turfs) {
            values.Add(new(turf));
            values.AddRange(turf.Contents.GetValues());
        }

        return values;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot set an index of area contents list");
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf))
            throw new Exception($"Cannot add {value} to area contents");

        turf.Cell.Area = _area;

        // TODO: Actually keep track of every turf in an area, not just which ones have been added by DM through .contents
        _turfs.Add(turf);
    }

    public override void RemoveValue(DreamValue value) {
        // TODO
    }

    public override void Cut(int start = 1, int end = 0) {
        // TODO
    }

    public override int GetLength() {
        int length = _turfs.Count;

        foreach (var turf in _turfs)
            length += turf.Contents.GetLength();

        return length;
    }

    public IEnumerable<DreamObjectTurf> GetTurfs() {
        return _turfs;
    }
}

// proc args list
sealed class ProcArgsList : DreamList {
    private readonly DMProcState _state;

    public ProcArgsList(DreamObjectDefinition listDef, DMProcState state) : base(listDef, 0) {
        _state = state;
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index into args list: {key}");
        if (index < 1 || index > _state.ArgumentCount)
            throw new Exception($"Out of bounds index on args list: {index}");

        return _state.GetArguments()[index - 1];
    }

    // TODO: This would preferably be an IEnumerable<> method. Probably as part of #985.
    public override List<DreamValue> GetValues() {
        List<DreamValue> values = new(_state.ArgumentCount);

        foreach (DreamValue value in _state.GetArguments()) {
            values.Add(value);
        }

        return values;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index into args list: {key}");
        if (index < 1 || index > _state.ArgumentCount)
            throw new Exception($"Out of bounds index on args list: {index}");

        _state.SetArgument(index - 1, value);
    }

    public override void AddValue(DreamValue value) {
        throw new Exception("Cannot add new values to args list");
    }

    public override void RemoveValue(DreamValue value) {
        throw new Exception("Cannot remove values to args list");
    }

    public override void Cut(int start = 1, int end = 0) {
        throw new Exception("Cannot cut args list");
    }

    public override int GetLength() {
        return _state.ArgumentCount;
    }
}

// Savefile Dir List - always sync'd with Savefiles currentDir. Only stores keys.
internal sealed class SavefileDirList : DreamList {
    private readonly DreamObjectSavefile _save;

    public SavefileDirList(DreamObjectDefinition listDef, DreamObjectSavefile backedSaveFile) : base(listDef, 0) {
        _save = backedSaveFile;
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index on savefile dir list: {key}");
        if (index < 1 || index > _save.CurrentDir.Count)
            throw new Exception($"Out of bounds index on savefile dir list: {index}");
        return new DreamValue(_save.CurrentDir.Keys.ElementAt(index - 1));
    }

    public override List<DreamValue> GetValues() {
        List<DreamValue> values = new(_save.CurrentDir.Count);

        foreach (string value in _save.CurrentDir.Keys.OrderBy(x => x))
            values.Add(new DreamValue(value));
        return values;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new Exception($"Invalid index on savefile dir list: {key}");
        if (!value.TryGetValueAsString(out var valueStr))
            throw new Exception($"Invalid value on savefile dir name: {value}");
        if (index < 1 || index > _save.CurrentDir.Count)
            throw new Exception($"Out of bounds index on savefile dir list: {index}");

        _save.RenameAndNullSavefileValue(_save.CurrentDir.Keys.ElementAt(index - 1), valueStr);
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsString(out var valueStr))
            throw new Exception($"Invalid value on savefile dir name: {value}");
        _save.AddSavefileDir(valueStr);

    }

    public override void RemoveValue(DreamValue value) {
        if (!value.TryGetValueAsString(out var valueStr))
            throw new Exception($"Invalid value on savefile dir name: {value}");
        _save.RemoveSavefileValue(valueStr);
    }

    public override void Cut(int start = 1, int end = 0) {
        throw new Exception("Cannot cut savefile dir list"); //BYOND actually throws undefined proc for this
    }

    public override int GetLength() {
        return _save.CurrentDir.Count;
    }
}
