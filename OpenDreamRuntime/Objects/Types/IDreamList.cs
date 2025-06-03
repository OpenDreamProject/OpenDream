namespace OpenDreamRuntime.Objects.Types;

public interface IDreamList {
    public bool IsAssociative { get; }

    public void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false);
    public DreamValue GetValue(DreamValue key);
    public bool ContainsKey(DreamValue key);
    public IEnumerable<DreamValue> EnumerateValues();
}
