using System.Linq;
namespace OpenDreamRuntime.Objects.Types;


// global.vars list
internal sealed class GlobalVarsList(DreamObjectDefinition listDef) : DreamList(listDef) {
    public override bool IsAssociative =>
        true; // We don't use the associative array but, yes, we behave like an associative list

    public override List<DreamValue> CopyKeys() {
        return EnumerateKeys().ToList();
    }

    public override int GetLength() {
        return ObjectTree.Root.ObjectDefinition.GlobalVariables.Keys.Count;
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        var root = ObjectTree.Root.ObjectDefinition;

        foreach (var key in root.GlobalVariables.Keys) {
            yield return new DreamValue(key);
        }
    }

    public override bool ElementHasAssociation(DreamValue value) {
        if (!value.TryGetValueAsString(out var varName)) {
            return false;
        }

        return ObjectTree.Root.ObjectDefinition.GlobalVariables.ContainsKey(varName);
    }

    public override bool ContainsKey(DreamValue value) {
        return ElementHasAssociation(value);
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsString(out var varName)) {
            throw new DMException($"Invalid var index {key}");
        }

        var root = ObjectTree.Root.ObjectDefinition;
        if (!root.GlobalVariables.TryGetValue(varName, out var globalId)) {
            throw new DMException($"Invalid global {varName}");
        }

        var value = DreamManager.Globals[globalId];
        value.IncRef();
        return value;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsString(out var varName)) {
            var root = ObjectTree.Root.ObjectDefinition;
            if (!root.GlobalVariables.TryGetValue(varName, out var globalId)) {
                throw new DMException($"Cannot set value of undefined global \"{varName}\"");
            }

            DreamManager.SetGlobal(globalId, value);
        } else {
            throw new DMException($"Invalid var index {key}");
        }
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override void AddKey(DreamValue newElement) {
        throw new DMException("Cannot add to global.vars list.");
    }

    public override void RemoveKey(DreamValue element) {
        throw new DMException("Cannot remove from global.vars list.");
    }
}
