namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectArea : DreamObjectAtom {
    public int X {
        get {
            UpdateCoordinateCache();
            return _cachedX!.Value;
        }
    }

    public int Y {
        get {
            UpdateCoordinateCache();
            return _cachedY!.Value;
        }
    }

    public int Z {
        get {
            UpdateCoordinateCache();
            return _cachedZ!.Value;
        }
    }

    public readonly AreaContentsList Contents;
    public int AppearanceId;

    // Iterating all our turfs to find the one with the lowest coordinates is slow business
    private int? _cachedX, _cachedY, _cachedZ;

    public DreamObjectArea(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Contents = new(ObjectTree.List.ObjectDefinition, this);
        AtomManager.SetAtomAppearance(this, AtomManager.GetAppearanceFromDefinition(ObjectDefinition));
    }

    /// <summary>
    /// Forces us to find the up-to-date "lowest" turf on next coordinate var access
    /// </summary>
    public void ResetCoordinateCache() {
        _cachedX = _cachedY = _cachedZ = null;
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
            case "y":
            case "z":
                throw new Exception($"Cannot set coordinate var '{varName}' on an area");
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

    /// <summary>
    /// Updates our cached coordinates with the location of the "lowest" turf, if we don't already have them cached
    /// </summary>
    private void UpdateCoordinateCache() {
        if (_cachedX != null)
            return;

        foreach (var turf in Contents.GetTurfs()) {
            _cachedX = Math.Min(turf.X, _cachedX ?? int.MaxValue);
            _cachedY = Math.Min(turf.Y, _cachedY ?? int.MaxValue);
            _cachedZ = Math.Min(turf.Z, _cachedZ ?? int.MaxValue);
        }

        // 0 if there were no turfs
        _cachedX ??= _cachedY = _cachedZ = 0;
    }
}
