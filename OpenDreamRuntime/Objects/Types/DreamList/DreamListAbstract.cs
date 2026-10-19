using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects.Types;
public abstract class DreamListAbstract : DreamObject {

	public override bool ShouldCallNew => false;

	public virtual bool IsAssociative { get; }

	/// <summary>
	/// Some list structures, namely eager lists, can directly return their internal lists instead of creating a new list from enumeration.
	/// </summary>
	/// <returns>Either a direct reference to the internal backing List, or a new List from enumeration depending on the implementation.</returns>
	public virtual List<DreamValue> GetKeysFast() {
		return GetKeys();
	}

	public DreamListAbstract(DreamObjectDefinition listDef) : base(listDef) {
	}

	/// <summary>
	/// Returns an Element at a given Index (numeric) or Key (non-numeric)
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	[MustDisposeResource] public virtual DreamValue GetElement(DreamValue key) {
		if (key.TryGetValueAsInteger(out int keyInteger)) {
			var value = GetKeysFast()[keyInteger]; //1-indexed

			value.IncRef();
			return value;
		}
		return DreamValue.Null;
	}

	public virtual bool TryGetElementByIndex(int index, [MustDisposeResource][NotNullWhen(true)] out DreamValue? element) {
		List<DreamValue> keys = GetKeysFast();
		if(!keys.TryGetValue(index, out var _value)) {
			element = _value;
			return false;
		}

		element = _value;
		element.Value.IncRef();
		return true;
	}

	[MustDisposeResource] public virtual DreamValue MustGetElementByIndex(int index) {
		List<DreamValue> keys = GetKeysFast();
		if(!keys.TryGetValue(index, out var element)) {
			throw new Exception($"Could not get index of {index}");
		}

		element.IncRef();
		return element;
	}

    /// <summary>
    /// Returns an associated Value using the given Key, if present.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
	public virtual bool TryGetElementByKey(DreamValue key, [MustDisposeResource][NotNullWhen(true)] out DreamValue? element) {
		Dictionary<DreamValue, DreamValue> pairs = GetLoosePairs();
		if(!pairs.TryGetValue(key, out var _value)) {
			element = _value;
			return false;
		}

		element = _value;
		element.Value.IncRef();
		return true;
	}


	/// <summary>
	/// Returns TRUE and the Value associated with Key, if present.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	[MustDisposeResource] public virtual bool TryGetAssocValue(DreamValue key, [MustDisposeResource][NotNullWhen(true)] out DreamValue? value) {
		if (!IsAssociative) {
			value = null;
			return false;
		}

		if (!ElementHasAssociation(key)) {
			value = null;
			return false;
		}

		value = GetElement(key);
		value.Value.IncRef();
		return true;
	}

	/// <summary>
	/// Returns whether or not the given element exists within the first dimension of the list.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	public abstract bool ContainsKey(DreamValue element);

	/// <summary>
	/// Returns the position of the given Element from within the Keys list.
	/// </summary>
	/// <param name="element"></param>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	public abstract int FindKeyIndex(DreamValue element, int start = 1, int end = 0);

	/// <summary>
	/// Returns if an element within the Keys list has an associated Value.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual bool ElementHasAssociation(DreamValue key) {
		return false;
	}

	/// <summary>
	/// Returns the number of elements in the 1-dimensional portion of the list.
	/// </summary>
	/// <returns></returns>
	public abstract int GetLength();

	/// <summary>
	/// Returns an Array of the 1-dimensional list elements.
	/// </summary>
	/// <returns></returns>
	public DreamValue[] ToArray() {
		return EnumerateKeys().ToArray();
	}

	/// <summary>
	/// Returns a new List containing the 1-dimensional elements of the list. Prefer EnumerateKeys() or ToArray() if possible.
	/// </summary>
	/// <returns></returns>
	public abstract List<DreamValue> GetKeys();

	/// <summary>
	/// Returns an IEnumerable of the 1-dimensional elements of the list.
	/// </summary>
	/// <returns></returns>
	public abstract IEnumerable<DreamValue> EnumerateKeys();

	/// <summary>
	/// Returns a dictionary of key-value associations. Does not copy or include any elements without associations.
	/// </summary>
	/// <returns></returns>
	public virtual Dictionary<DreamValue, DreamValue> GetTightPairs() {
		if(!IsAssociative) {
			return new Dictionary<DreamValue, DreamValue>();
		}

		return EnumerateTightPairs().ToDictionary();
	}

	/// <summary>
	/// Returns a Dictionary of key-value pairs. Any elements without an association are associated with DreamValue.Null.
	/// </summary>
	/// <returns></returns>
	public virtual Dictionary<DreamValue, DreamValue> GetLoosePairs() {
		return new(EnumerateLoosePairs());
	}

	/// <summary>
	/// Returns an enumerable of key-value associations. Elements without associations are included, associated with DreamValue.Null.
	/// </summary>
	/// <returns></returns>
	public virtual IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateLoosePairs() {
		return EnumerateKeys()
			.Distinct()
			.Select(key => KeyValuePair.Create(key, DreamValue.Null));
	}

	/// <summary>
	/// Returns an enumerable of key-value associations. Elements without associations are excluded.
	/// </summary>
	/// <returns></returns>
	public virtual IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateTightPairs() {
        return Enumerable.Empty<KeyValuePair<DreamValue, DreamValue>>();
	}

	/// <summary>
	/// Set a key-value pair within a list.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <param name="allowGrowth"></param>
	public abstract void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false);

	/// <summary>
	/// Set an element by numeric index. Unimplemented on most types of lists.
	/// </summary>
	/// <param name="index"></param>
	/// <param name="element"></param>
	/// <param name="allowGrowth"></param>
	/// <exception cref="Exception"></exception>
	public virtual void SetElementByIndex(DreamValue index, DreamValue element, bool allowGrowth = false) {
		throw new Exception($"Unable to index list of type {ObjectDefinition.Type}");
	}

	/// <summary>
	/// Adds an Element to the Keys list if present.
	/// </summary>
	/// <param name="newElement"></param>
	public abstract void AddKey(DreamValue newElement);

	/// <summary>
	/// Removes an Element from the Keys list if present.
	/// </summary>
	/// <param name="element"></param>
	public abstract void RemoveKey(DreamValue element);

	/// <summary>
	/// Removes all elements from index Start (Inclusive) to End (Exclusive). End 0 will cut everything up to and including the final element.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	public virtual void Cut(int start = 1, int end = 0) {
        throw new DMException($"Cut() not implemented for type ({GetType().Name})");
    }

	public virtual void Insert(int index, DreamValue value) {
        throw new DMException($"Insert() not implemented for type ({GetType().Name})");
    }

	public virtual void Swap(int index1, int index2) {
        throw new DMException($"Swap() not implemented for type ({GetType().Name})");
    }

    [Obsolete("Consider ToDreamList() instead.")]
	/// <summary>
	/// Returns a shallow copy of the list (and associations) from index Start (Inclusive) to End (Exclusive). End 0 will copy everything up to and including the final element.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	public virtual DreamListAbstract ToCopy(int start = 1, int end = 0) {
        throw new NotImplementedException();
    }

	/// <summary>
	/// Returns a shallow copy of the list (and associations) from index Start (Inclusive) to End (Exclusive) as a DreamList. End 0 will copy everything up to and including the final element.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	public virtual DreamList ToDreamList(int start = 1, int end = 0) {
		if (start == 0) ++start; //start being 0 and start being 1 are equivalent

		var keys = GetKeysFast();
		if (end > keys.Count + 1 || start > keys.Count + 1)
			throw new DMException("list index out of bounds");

		if (end == 0)
			end = keys.Count + 1;

		if (end <= start)
			return new DreamList(ObjectDefinition, 0);

		List<DreamValue> copyValues = keys.GetRange(start - 1, end - start);
		Dictionary<DreamValue, DreamValue> associativeValues = GetTightPairs();

		Dictionary<DreamValue, DreamValue>? newAssociativeValues;

		if (IsAssociative) {
			newAssociativeValues = new(end - start);
			foreach (var key in copyValues) {
				if (associativeValues.TryGetValue(key, out var value))
					newAssociativeValues[key] = value;
			}
		}

		return new DreamList(ObjectDefinition, copyValues, associativeValues);
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
}
