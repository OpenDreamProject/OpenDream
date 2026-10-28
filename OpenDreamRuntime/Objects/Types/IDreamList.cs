using System.Linq;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    public bool IsAssociative { get; }

    /// <summary>
    /// Returns an Element at a given Index (numeric) or Key (non-numeric)
    /// </summary>
    [MustDisposeResource] public DreamValue GetElement(DreamValue key);

    /// <summary>
    /// Returns whether or not the given element exists within the first dimension of the list.
    /// </summary>
    public bool ContainsKey(DreamValue element);

    /// <summary>
    /// Returns the position of the given Element from within the Keys list.
    /// </summary>
    public int FindIndex(DreamValue element, int start = 1, int end = 0);

    /// <summary>
    /// Returns if an element within the Keys list has an associated Value.
    /// </summary>
    public bool ElementHasAssociation(DreamValue key);

    /// <summary>
    /// Returns the number of elements in the 1-dimensional portion of the list.
    /// </summary>
    public int GetLength();

    /// <summary>
    /// Returns a shallow copy of the list (and associations) from index Start (Inclusive) to End (Exclusive). End 0 will copy everything up to and including the final element.
    /// </summary>
    public IDreamList CreateCopy(int start = 1, int end = 0);

    /// <summary>
    /// Returns a Dictionary of key-value pairs. Any elements without an association are associated with DreamValue.Null.
    /// </summary>
    public Dictionary<DreamValue, DreamValue> CopyPairs() {
        return new(EnumeratePairs());
    }

    /// <summary>
    /// Returns an Array of the 1-dimensional list elements.
    /// </summary>
    public DreamValue[] ToArray() {
        return Enumerate().ToArray();
    }

    /// <summary>
    /// Returns a new List containing the 1-dimensional elements of the list. Prefer EnumerateKeys() or ToArray() if possible.
    /// </summary>
    public List<DreamValue> GetKeys();

    /// <summary>
    /// Returns an IEnumerable of the 1-dimensional elements of the list.
    /// </summary>
    public IEnumerable<DreamValue> Enumerate();

    /// <summary>
    /// Returns a dictionary of key-value associations. Does not copy or include any elements without associations.
    /// </summary>
    public Dictionary<DreamValue, DreamValue> GetPairs();

    /// <summary>
    /// Returns an enumerable of key-value associations. Elements without associations are included, associated with DreamValue.Null.
    /// </summary>
    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumeratePairs();

    /// <summary>
    /// Set a key-value pair within a list.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="allowGrowth">Whether a key equal to length+1 should grow the list</param>
    public void Set(DreamValue key, DreamValue value, bool allowGrowth = false);

    /// <summary>
    /// Adds an Element to the Keys list if present.
    /// </summary>
    public void Add(DreamValue newElement);

    /// <summary>
    /// Removes an Element from the Keys list if present.
    /// </summary>
    public void Remove(DreamValue element);

    /// <summary>
    /// Removes all elements from index Start (Inclusive) to End (Exclusive). End 0 will cut everything up to and including the final element.
    /// </summary>
    public void Cut(int start = 1, int end = 0);

    public void Insert(int index, DreamValue value);
    public void Swap(int index1, int index2);
}
