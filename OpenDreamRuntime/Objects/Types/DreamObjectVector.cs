using System.Linq;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectVector(DreamObjectDefinition definition) : DreamObject(definition) {
    public double X, Y;

    public double Z {
        get => Is3D ? _z : 0;
        set {
            if (!Is3D)
                return;
            _z = value;
        }
    }

    public bool Is3D { get; private set; }

    public double Size {
        get => Math.Sqrt(X * X + Y * Y + Z * Z);
        set {
            if (X == 0 && Y == 0 && Z == 0)
                return;

            var magnitude = Size;
            X = X / magnitude * value;
            Y = Y / magnitude * value;
            Z = Z / magnitude * value;
        }
    }

    public Vector2 AsVector2 => new((float)X, (float)Y);
    public Vector3 AsVector3 => new((float)X, (float)Y, Is3D ? (float)Z : 0f);

    private double _z;

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        var arg1 = args.GetArgument(0);
        if (arg1.TryGetValueAsFloat(out var x) && args.Count is 2 or 3) { // X, Y, optionally Z
            X = x;
            Y = args.GetArgument(1).UnsafeGetValueAsFloat();
            if (args.Count == 3) {
                Is3D = true;
                Z = args.GetArgument(2).UnsafeGetValueAsFloat();
            }

            return;
        } else if (arg1.TryGetValueAsString(out var vectorStr)) { // Numbers with a comma or 'x' as a delimiter
            var components = vectorStr.Split(',', 'x');

            if (components.Length is 2 or 3) {
                X = float.Parse(components[0]);
                Y = float.Parse(components[1]);
                if (components.Length == 3) {
                    Is3D = true;
                    Z = float.Parse(components[2]);
                }

                return;
            }
        } else if (arg1.TryGetValueAsDreamList(out var vectorList)) { // list(X, Y) or list(X, Y, Z)
            var components = vectorList.GetValues();

            if (components.Count is 2 or 3 && components.All(v => v.Type == DreamValue.DreamValueType.Float)) {
                X = components[0].UnsafeGetValueAsFloat();
                Y = components[1].UnsafeGetValueAsFloat();
                if (components.Count == 3) {
                    Is3D = true;
                    Z = components[2].UnsafeGetValueAsFloat();
                }

                return;
            }
        } else if (arg1.TryGetValueAsDreamObject<DreamObjectVector>(out var vectorCopy)) { // new /vector(vector)
            Is3D = vectorCopy.Is3D;
            X = vectorCopy.X;
            Y = vectorCopy.Y;
            Z = vectorCopy.Z;

            return;
        }

        // TODO: Allow pixloc as an arg
        throw new Exception($"Bad vector arguments {args.ToString()}");
    }

    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            var output = new DreamObjectVector(ObjectDefinition) {
                X = X + right.X,
                Y = Y + right.Y,
                Is3D = Is3D || right.Is3D,
                Z = Z + right.Z
            };

            return new DreamValue(output);
        }

        return base.OperatorAdd(b, state);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            var output = new DreamObjectVector(ObjectDefinition) {
                X = X - right.X,
                Y = Y - right.Y,
                Is3D = Is3D || right.Is3D,
                Z = Z - right.Z
            };

            return new DreamValue(output);
        }

        return base.OperatorSubtract(b, state);
    }

    public override DreamValue OperatorMultiply(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsFloat(out float scalar)) {
            var output = new DreamObjectVector(ObjectDefinition) {
                X = X * scalar,
                Y = Y * scalar,
                Is3D = Is3D,
                Z = Z * scalar
            };
            return new DreamValue(output);
        } else if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            var output = new DreamObjectVector(ObjectDefinition) {
                X = X * right.X,
                Y = Y * right.Y,
                Is3D = Is3D || right.Is3D,
                Z = Z * right.Z
            };
            return new DreamValue(output);
        }

        return base.OperatorMultiply(b, state);
    }

    public override DreamValue OperatorMultiplyRef(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsFloat(out float scalar)) {
            X *= scalar;
            Y *= scalar;
            Z *= scalar;
            return new DreamValue(this);
        } else if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            X *= right.X;
            Y *= right.Y;
            Z *= right.Z;
            return new DreamValue(this);
        }

        return base.OperatorMultiplyRef(b, state);
    }

    public override DreamValue OperatorDivide(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsFloat(out float scalar)) {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide vector by zero");

            var output = new DreamObjectVector(ObjectDefinition) {
                X = X / scalar,
                Y = Y / scalar,
                Is3D = Is3D,
                Z = Z / scalar
            };

            return new DreamValue(output);
        } else if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            if (right.X == 0 || right.Y == 0 || (Is3D && right.Z == 0))
                throw new DivideByZeroException("Cannot divide vector by zero vector component");

            var output = new DreamObjectVector(ObjectDefinition) {
                X = X / right.X,
                Y = Y / right.Y,
                Is3D = Is3D || right.Is3D,
                Z = right.Z == 0 ? 0 : Z / right.Z
            };
            return new DreamValue(output);
        }

        return base.OperatorDivide(b, state);
    }

    public override DreamValue OperatorDivideRef(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsFloat(out float scalar)) {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide vector by zero");
            X /= scalar;
            Y /= scalar;
            Z /= scalar;
            return new DreamValue(this);
        } else if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            if (right.X == 0 || right.Y == 0 || (Is3D && right.Z == 0))
                throw new DivideByZeroException("Cannot divide vector by zero vector component");
            X /= right.X;
            Y /= right.Y;
            Z = right.Z == 0 ? 0 : Z / right.Z;
            return new DreamValue(this);
        }

        return base.OperatorDivideRef(b, state);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            X += right.X;
            Y += right.Y;
            Z += right.Z;
            Is3D = Is3D || right.Is3D;

            return new DreamValue(this);
        }

        return base.OperatorAppend(b);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsDreamObject<DreamObjectVector>(out var right)) {
            X -= right.X;
            Y -= right.Y;
            Z -= right.Z;
            Is3D = Is3D || right.Is3D;

            return new DreamValue(this);
        }

        return base.OperatorRemove(b);
    }

    #endregion Operators

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "type":
                value = new(ObjectDefinition.TreeEntry);
                return true;
            case "len":
                value = new(Is3D ? 3 : 2);
                return true;
            case "size":
                value = new(Size);
                return true;
            case "x":
                value = new(X);
                return true;
            case "y":
                value = new(Y);
                return true;
            case "z":
                value = new(Z);
                return true;
            default:
                // Hide the base vars
                throw new Exception($"Invalid vector variable \"{varName}\"");
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "type":
                throw new Exception("Cannot set type var");
            case "len":
                var newLen = value.UnsafeGetValueAsFloat();

                // Something like 2.3 actually isn't valid here; it doesn't cast to an int
                if (!newLen.Equals(2f) && !newLen.Equals(3f)) {
                    throw new Exception($"Invalid vector len {value}");
                }

                Is3D = newLen.Equals(3f);
                break;
            case "size":
                Size = value.UnsafeGetValueAsFloat();
                break;
            case "x":
                X = value.UnsafeGetValueAsFloat();
                break;
            case "y":
                Y = value.UnsafeGetValueAsFloat();
                break;
            case "z":
                Z = value.UnsafeGetValueAsFloat();
                break;
            default:
                // Hide the base vars
                throw new Exception($"Invalid vector variable \"{varName}\"");
        }
    }

    public static DreamObjectVector CreateFromValue(DreamValue value, DreamObjectTree tree) {
        if (value.TryGetValueAsDreamObject<DreamObjectVector>(out var vector))
            return vector;

        vector = tree.CreateObject<DreamObjectVector>(tree.Vector);

        if (value.TryGetValueAsDreamList(out var list)) {
            var length = list.GetLength();

            if (length >= 3) {
                var x = list.GetValue(new(1));
                var y = list.GetValue(new(2));
                var z = list.GetValue(new(3));

                vector.Initialize(new(x, y, z));
            } else if (length == 2) {
                var x = list.GetValue(new(1));
                var y = list.GetValue(new(2));

                vector.Initialize(new(x, y));
            } else {
                // Fall back to a Vector2.Zero
                vector.Initialize(new(new(0f), new(0f)));
            }
        } else {
            // Fall back to a Vector2.Zero
            vector.Initialize(new(new(0f), new(0f)));
        }

        return vector;
    }

    // TODO: Operators, supports indexing and "most math"
    // TODO: For loop support
}
