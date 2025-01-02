using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectTurf : DreamObjectAtom {
    public readonly int X, Y, Z;
    public readonly TurfContentsList Contents;
    public ImmutableAppearance Appearance;
    public IDreamMapManager.Cell Cell;

    public DreamObjectTurf(DreamObjectDefinition objectDefinition, int x, int y, int z) : base(objectDefinition) {
        X = x;
        Y = y;
        Z = z;

        Cell = default!; // NEEDS to be set by DreamMapManager after creation
        Contents = new TurfContentsList(ObjectTree.List.ObjectDefinition, this);
        Appearance = AppearanceSystem!.AddAppearance(AtomManager.GetAppearanceFromDefinition(ObjectDefinition));
    }

    public void SetTurfType(DreamObjectDefinition objectDefinition) {
        if (!objectDefinition.IsSubtypeOf(ObjectTree.Turf))
            throw new Exception($"Cannot set turf's type to {objectDefinition.Type}");

        ObjectDefinition = objectDefinition;
        Variables?.Clear();

        Initialize(new());
    }

    public void OnAreaChange(DreamObjectArea oldArea) {
        if (Cell == null!)
            return;

        var newAppearance = Appearance.ToMutable();

        newAppearance.Overlays.Remove(oldArea.Appearance);
        DreamMapManager.SetTurfAppearance(this, newAppearance);
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
}
