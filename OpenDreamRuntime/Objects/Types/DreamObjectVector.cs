using System.Linq;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectVector(DreamObjectDefinition definition) : DreamObject(definition) {
    public float X, Y;

    public float Z {
        get => Is3D ? _z : 0;
        set {
            if (!Is3D)
                return;
            _z = value;
        }
    }

    public bool Is3D { get; private set; }

    public float Size {
        get => MathF.Sqrt(X * X + Y * Y + Z * Z);
        set {
            if (X == 0 && Y == 0 && Z == 0)
                return;

            var magnitude = Size;
            X = X / magnitude * value;
            Y = Y / magnitude * value;
            Z = Z / magnitude * value;
        }
    }

    private float _z;

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

    // TODO: Operators, supports indexing and "most math"
    // TODO: For loop support
}
