using OpenDreamRuntime.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectArea : DreamObjectAtom {
    public int X, Y, Z;
    public readonly AreaContentsList Contents;
    public ImmutableIconAppearance Appearance;

    public DreamObjectArea(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Contents = new(ObjectTree.List.ObjectDefinition, this);
        Appearance = AppearanceSystem!.DefaultAppearance;
        AtomManager.SetAtomAppearance(this, AtomManager.GetAppearanceFromDefinition(ObjectDefinition));
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
            case "contents":
                value = new(Contents);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "x":
                value.TryGetValueAsInteger(out X);
                break;
            case "y":
                value.TryGetValueAsInteger(out Y);
                break;
            case "z":
                value.TryGetValueAsInteger(out Z);
                break;
            case "contents":
                // TODO
                break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    public override void OperatorOutput(DreamValue b) {
        if (b.TryGetValueAsDreamObject<DreamObjectSound>(out _)) {
            // Output the sound to every connection with a mob inside this area
            foreach (var connection in DreamManager.Connections) {
                var mob = connection.Mob;
                if (mob == null)
                    continue;

                if (!DreamMapManager.TryGetCellAt(mob.Position, mob.Z, out var cell))
                    continue;

                if (cell.Area != this)
                    continue;

                connection.OutputDreamValue(b);
            }

            return;
        }

        base.OperatorOutput(b);
    }
}
