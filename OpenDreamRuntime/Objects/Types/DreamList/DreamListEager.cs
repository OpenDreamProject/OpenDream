namespace OpenDreamRuntime.Objects.Types;
public abstract class DreamEagerList : DreamListAbstract {
    private static readonly Stack<List<DreamValue>> ListPool = new();

    private readonly List<DreamValue> _elements;

    public DreamEagerList(DreamObjectDefinition listDef, int size) : base(listDef) {
        if (size >= DreamManager.ListPoolThreshold && ListPool.TryPop(out var poppedValues)) {
            _elements = poppedValues;
            _elements.EnsureCapacity(size);
        } else {
            _elements = new List<DreamValue>(size);
        }
    }
}
