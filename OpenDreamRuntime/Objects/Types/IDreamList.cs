using System.Linq;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    public bool IsAssociative { get; }

    public void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false);
    [MustDisposeResource] public DreamValue GetElement(DreamValue key);
    public bool ElementHasAssociation(DreamValue key);
    public IEnumerable<DreamValue> EnumerateKeys();
    public int GetLength();
    public void Cut(int start = 1, int end = 0);
    public List<DreamValue> GetKeys();
    public Dictionary<DreamValue, DreamValue> GetAssociativeValues();
    public void RemoveElement(DreamValue element);
    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues();
    public void AddElement(DreamValue newElement);
    public IDreamList CreateCopy(int start = 1, int end = 0);
    public int FindKey(DreamValue key, int start = 1, int end = 0);
    public void Insert(int index, DreamValue value);
    public bool ContainsKey(DreamValue value);
    public void Swap(int index1, int index2);

    public DreamValue[] CopyToArray() {
        return EnumerateKeys().ToArray();
    }

    public Dictionary<DreamValue, DreamValue> CopyAssocValues() {
        return new(EnumerateAssocValues());
    }
}
