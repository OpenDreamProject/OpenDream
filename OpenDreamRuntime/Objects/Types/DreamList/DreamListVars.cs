using System.Linq;
namespace OpenDreamRuntime.Objects.Types;

/// <summary>
/// The list type for /datum/var/vars.
/// </summary>
/// <param name="listDef"></param>
/// <param name="dreamObject"></param>
internal sealed class DreamListVars(DreamObjectDefinition listDef, DreamObject dreamObject) : DreamListAbstract(listDef) {
    public readonly DreamObject DreamObject = dreamObject;

    public override bool IsAssociative => true; // We don't use the associative array but, yes, we behave like an associative list

    public override int GetLength() {
        return DreamObject.GetVariableNames().Concat(DreamObject.ObjectDefinition.GlobalVariables.Keys).Count();
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return DreamObject.GetVariableNames().Concat(DreamObject.ObjectDefinition.GlobalVariables.Keys).Select(name => new DreamValue(name));
    }

    public override bool ElementHasAssociation(DreamValue value) {
        if (!value.TryGetValueAsString(out var varName)) {
            return false;
        }

        return DreamObject.HasVariable(varName);
    }

    public override bool ContainsKey(DreamValue value) {
        return ElementHasAssociation(value);
    }

    public override DreamValue GetElement(DreamValue key) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            return new DreamValue(DreamObject.GetVariableNames().ElementAt(keyInteger - 1)); //1-indexed
        } else if (key.TryGetValueAsString(out var varName)) {
            if (DreamObject.TryGetVariable(varName, out var objectVar)) {
                return objectVar;
            }

            throw new DMException($"Cannot get value of undefined var \"{key}\" on type {DreamObject.ObjectDefinition.Type}");
        } else {
            throw new DMException($"Invalid var index {key}");
        }
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsString(out var varName)) {
            if (!DreamObject.HasVariable(varName)) {
                throw new DMException(
                    $"Cannot set value of undefined var \"{varName}\" on type {DreamObject.ObjectDefinition.Type}");
            }

            DreamObject.SetVariable(varName, value);
        } else {
            throw new DMException($"Invalid var index {key}");
        }
    }

    public override void AddKey(DreamValue newElement) {
        throw new DMException("Cannot add to Vars list.");
    }

    public override void RemoveKey(DreamValue element) {
        throw new DMException("Cannot remove from Vars list.");
    }

    public override bool IsSaved(string name) {
        return DreamObject.IsSaved(name);
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        return GetKeys().IndexOf(value)+1; // IndexOf is 0 indexed, returns -1 on fail, DM is 1 indexed and returns 0 on fail, so +1
    }
}
