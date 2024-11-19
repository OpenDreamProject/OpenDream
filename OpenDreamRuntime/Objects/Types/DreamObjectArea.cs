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

    public readonly HashSet<DreamObjectTurf> Turfs;
    public int AppearanceId;

    private readonly AreaContentsList _contents;

    // Iterating all our turfs to find the one with the lowest coordinates is slow business
    private int? _cachedX, _cachedY, _cachedZ;

    public DreamObjectArea(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Turfs = new();
        AtomManager.SetAtomAppearance(this, AtomManager.GetAppearanceFromDefinition(ObjectDefinition));
        _contents = new(ObjectTree.List.ObjectDefinition, this);
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
                value = new(_contents);
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
    /// Updates our cached coordinates with the location of the "lowest" turf, if we don't already have them cached.
    /// <br/>
    /// The "lowest" turf is the turf with the lowest z, y, then x.
    /// </summary>
    private void UpdateCoordinateCache() {
        if (_cachedX != null)
            return;

        foreach (var turf in Turfs) {
            if (_cachedX != null) {
                if (turf.Z > _cachedZ)
                    continue;

                int index = turf.Y * DreamMapManager.Size.X + turf.X;
                if (index >= _cachedY * DreamMapManager.Size.X + _cachedX)
                    continue;
            }

            _cachedX = turf.X;
            _cachedY = turf.Y;
            _cachedZ = turf.Z;
        }

        // 0 if there were no turfs
        _cachedX ??= _cachedY = _cachedZ = 0;
    }
}
