using System.Linq;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    public bool IsAssociative { get; }

    /// <summary>
    /// Returns an Element at a given Index (numeric) or Key (non-numeric)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [MustDisposeResource] public DreamValue GetElement(DreamValue key);

    /// <summary>
    /// Returns whether or not the given element exists within the first dimension of the list.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public bool ContainsKey(DreamValue element);

    /// <summary>
    /// Returns the position of the given Element from within the Keys list.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public int FindKeyIndex(DreamValue element, int start = 1, int end = 0);

    /// <summary>
    /// Returns if an element within the Keys list has an associated Value.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ElementHasAssociation(DreamValue key);

    /// <summary>
    /// Returns the number of elements in the 1-dimensional portion of the list.
    /// </summary>
    /// <returns></returns>
    public int GetLength();

    /// <summary>
    /// Returns a shallow copy of the list (and associations) from index Start (Inclusive) to End (Exclusive). End 0 will copy everything up to and including the final element.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public IDreamList CreateCopy(int start = 1, int end = 0);

    /// <summary>
    /// Returns a Dictionary of key-value pairs. Any elements without an association are associated with DreamValue.Null.
    /// </summary>
    /// <returns></returns>
    public Dictionary<DreamValue, DreamValue> CopyPairs() {
        return new(EnumeratePairs());
    }

    /// <summary>
    /// Returns an Array of the 1-dimensional list elements.
    /// </summary>
    /// <returns></returns>
    public DreamValue[] ToArray() {
        return EnumerateKeys().ToArray();
    }

    /// <summary>
    /// Returns a new List containing the 1-dimensional elements of the list.
    /// </summary>
    /// <returns></returns>
    public List<DreamValue> GetKeys();

    /// <summary>
    /// Returns an IEnumerable of the 1-dimensional elements of the list.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DreamValue> EnumerateKeys();

    /// <summary>
    /// Returns a dictionary of key-value associations. Does not copy or include any elements without associations.
    /// </summary>
    /// <returns></returns>
    public Dictionary<DreamValue, DreamValue> GetPairs();

    /// <summary>
    /// Returns an enumerable of key-value associations. Elements without associations are included, associated with DreamValue.Null.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumeratePairs();

    /// <summary>
    /// Set a key-value pair within a list.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="allowGrowth"></param>
    public void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false);

    /// <summary>
    /// Adds an Element to the Keys list if present.
    /// </summary>
    /// <param name="newElement"></param>
    public void AddKey(DreamValue newElement);

    /// <summary>
    /// Removes an Element from the Keys list if present.
    /// </summary>
    /// <param name="element"></param>
    public void RemoveKey(DreamValue element);

    /// <summary>
    /// Removes all elements from index Start (Inclusive) to End (Exclusive). End 0 will cut everything up to and including the final element.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public void Cut(int start = 1, int end = 0);
    public void Insert(int index, DreamValue value);
    public void Swap(int index1, int index2);

}
