namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectTurf : DreamObjectAtom {
    public readonly int X, Y, Z;
    public readonly IDreamMapManager.Cell Cell;
    public readonly TurfContentsList Contents;
    public int AppearanceId { get => _appearanceId; set => SetAppearanceId(value); }

    private int _appearanceId = -1;

    public DreamObjectTurf(DreamObjectDefinition objectDefinition, int x, int y, int z, IDreamMapManager.Cell cell) : base(objectDefinition) {
        X = x;
        Y = y;
        Z = z;
        Cell = cell;
        Contents = new TurfContentsList(ObjectTree.List.ObjectDefinition, Cell);
    }

    public void SetTurfType(DreamObjectDefinition objectDefinition) {
        if (!objectDefinition.IsSubtypeOf(ObjectTree.Turf))
            throw new Exception($"Cannot set turf's type to {objectDefinition.Type}");

        ObjectDefinition = objectDefinition;
        Variables?.Clear();

        Initialize(new());
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "x":
                value = new(X);
                return true;
            case "y":
                value = new(Y);
                return true;
            case "z":
                value = new(Z);
                return true;
            case "loc":
                value = new(Cell.Area);
                return true;
            case "contents":
                value = new(Contents);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "contents":
                Contents.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue contentValue in valueList.GetValues()) {
                        Contents.AddValue(contentValue);
                    }
                }

                break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    public void SetAppearanceId(int appearanceId) {
        if (_appearanceId != -1) {
            AppearanceSystem!.DecreaseAppearanceRefCount(_appearanceId);
        }

        _appearanceId = appearanceId;

        AppearanceSystem!.IncreaseAppearanceRefCount(_appearanceId);

    }
}
