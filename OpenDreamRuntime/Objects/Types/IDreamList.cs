using System.Linq;

namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    bool IsAssociative { get; }

    void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false);
    DreamValue GetValue(DreamValue key);
    bool ContainsKey(DreamValue key);
    IEnumerable<DreamValue> EnumerateValues();
    int GetLength();
    void Cut(int start = 1, int end = 0);
    List<DreamValue> GetValues();
    Dictionary<DreamValue, DreamValue> GetAssociativeValues();
    void RemoveValue(DreamValue value);
    IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues();
    void AddValue(DreamValue value);
    IDreamList CreateCopy(int start = 1, int end = 0);
    int FindValue(DreamValue value, int start = 1, int end = 0);
    void Insert(int index, DreamValue value);
    bool ContainsValue(DreamValue value);
    void Swap(int index1, int index2);

    DreamValue[] CopyToArray() {
        return EnumerateValues().ToArray();
    }

    Dictionary<DreamValue, DreamValue> CopyAssocValues() {
        return new(EnumerateAssocValues());
    }
}
