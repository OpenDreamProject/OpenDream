using System.Linq;

namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    public bool IsAssociative { get; }

    public void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false);
    public DreamValue GetValue(DreamValue key);
    public bool ContainsKey(DreamValue key);
    public IEnumerable<DreamValue> EnumerateValues();
    public int GetLength();
    public void Cut(int start = 1, int end = 0);
    public List<DreamValue> GetValues();
    public Dictionary<DreamValue, DreamValue> GetAssociativeValues();
    public void RemoveValue(DreamValue value);
    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues();

    public DreamValue[] CopyToArray() {
        return EnumerateValues().ToArray();
    }

    public Dictionary<DreamValue, DreamValue> CopyAssocValues() {
        return new(EnumerateAssocValues());
    }
}
