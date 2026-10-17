using System.Linq;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    public bool IsAssociative { get; }

    /// <summary>
    /// Set a key-value pair within a list.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="allowGrowth"></param>
    public void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false);
    /// <summary>
    /// Returns an Element at a given Index (numeric) or Key (non-numeric)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [MustDisposeResource] public DreamValue GetElement(DreamValue key);
    /// <summary>
    /// Returns if an element within the Keys list has an associated Value.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ElementHasAssociation(DreamValue key);
    public IEnumerable<DreamValue> EnumerateKeys();
    public int GetLength();
    public void Cut(int start = 1, int end = 0);
    public List<DreamValue> GetKeys();
    public Dictionary<DreamValue, DreamValue> GetAssociativeValues();
    /// <summary>
    /// Removes an Element from the Keys list if present.
    /// </summary>
    /// <param name="element"></param>
    public void RemoveKey(DreamValue element);
    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues();
    /// <summary>
    /// Adds an Element to the Keys list if present.
    /// </summary>
    /// <param name="newElement"></param>
    public void AddKey(DreamValue newElement);
    public IDreamList CreateCopy(int start = 1, int end = 0);
    /// <summary>
    /// Returns the position of the given Element from within the Keys list.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public int FindKeyIndex(DreamValue element, int start = 1, int end = 0);
    public void Insert(int index, DreamValue value);
    /// <summary>
    /// Returns whether or not the given element exists within the Keys list.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public bool ContainsKey(DreamValue element);
    public void Swap(int index1, int index2);

    public DreamValue[] CopyToArray() {
        return EnumerateKeys().ToArray();
    }

    public Dictionary<DreamValue, DreamValue> CopyAssocValues() {
        return new(EnumerateAssocValues());
    }
}
