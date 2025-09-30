using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectParticles : DreamObject {
    public readonly DreamParticlesComponent ParticlesComponent;

    private readonly List<MutableAppearance> _icons = new();
    private readonly List<string> _iconStates = new();

    public DreamObjectParticles(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        ParticlesComponent = new DreamParticlesComponent();

        //populate component with settings from type
        foreach (KeyValuePair<string,DreamValue> kv in objectDefinition.Variables) {
            if (kv.Key is not ("parent_type" or "type" or "vars"))
                SetVar(kv.Key, kv.Value);
        }
    }

     protected override void SetVar(string varName, DreamValue value) {
        //good news, these only update on assignment, so we don't need to track the generator, list, or matrix objects
        switch (varName) {
            case "width": //num
                ParticlesComponent.Width = (int)value.UnsafeGetValueAsFloat();
                break;
            case "height": //num
                ParticlesComponent.Height = (int)value.UnsafeGetValueAsFloat();
                break;
            case "count": //num
                if (!value.TryGetValueAsInteger(out var count))
                    break;

                ParticlesComponent.Count = count;
                break;
            case "spawning": //num
                ParticlesComponent.Spawning = value.UnsafeGetValueAsFloat();
                break;
            case "bound1": //list or vector
                if (value.TryGetValueAsDreamList(out var bound1List) && bound1List.GetLength() >= 3) {
                    var boundX = bound1List.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var boundY = bound1List.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var boundZ = bound1List.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.Bound1 = new Vector3(boundX, boundY, boundZ);
                } //else if vector

                break;
            case "bound2": //list or vector
                if (value.TryGetValueAsDreamList(out var bound2List) && bound2List.GetLength() >= 3) {
                    var boundX = bound2List.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var boundY = bound2List.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var boundZ = bound2List.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.Bound2 = new Vector3(boundX, boundY, boundZ);
                } //else if vector

                break;
            case "gravity": //list or vector
                if (value.TryGetValueAsDreamList(out var gravityList) && gravityList.GetLength() >= 3) {
                    var gravityX = gravityList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var gravityY = gravityList.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var gravityZ = gravityList.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.Gravity = new Vector3(gravityX, gravityY, gravityZ);
                } //else if vector

                break;
            case "gradient": //color gradient list
                if (value.TryGetValueAsDreamList(out var colorList)) {
                    Color[] grad = new Color[colorList.GetLength()];
                    int i = 0;
                    foreach (DreamValue colorValue in colorList.EnumerateValues()) {
                        if (!colorValue.TryGetValueAsString(out var colorStr))
                            continue;
                        if (!ColorHelpers.TryParseColor(colorStr, out var c, defaultAlpha: string.Empty))
                            continue;

                        grad[i++] = c;
                    }

                    ParticlesComponent.Gradient = grad;
                }

                break;
            case "transform": //matrix
                if (value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrix)) {
                    float[] m = DreamObjectMatrix.MatrixToTransformFloatArray(matrix);
                    ParticlesComponent.Transform = new(m[0], m[1], m[2], m[3], m[4], m[5]);
                }

                break;
            case "icon": //list or icon
                _icons.Clear();
                if (value.TryGetValueAsIDreamList(out var iconList)) {
                    foreach (DreamValue iconValue in iconList.EnumerateValues()) {
                        if (!DreamResourceManager.TryLoadIcon(iconValue, out var iconRsc))
                            continue;

                        MutableAppearance iconAppearance = MutableAppearance.Get();
                        iconAppearance.Icon = iconRsc.Id;
                        _icons.Add(iconAppearance);
                    }
                } else if (DreamResourceManager.TryLoadIcon(value, out var iconRsc)) {
                    MutableAppearance iconAppearance = MutableAppearance.Get();
                    iconAppearance.Icon = iconRsc.Id;
                    _icons.Add(iconAppearance);
                }

                List<ImmutableAppearance> immutableAppearances = new();
                foreach (var icon in _icons) {
                    foreach (var iconState in _iconStates) {
                        MutableAppearance iconCombo = MutableAppearance.GetCopy(icon);
                        iconCombo.IconState = iconState;
                        immutableAppearances.Add(AppearanceSystem!.AddAppearance(iconCombo));
                    }
                }

                ParticlesComponent.TextureList = immutableAppearances.ToArray();
                break;
            case "icon_state": //list or string
                _iconStates.Clear();
                if (value.TryGetValueAsIDreamList(out var iconStateList)) {
                    foreach (DreamValue iconValue in iconStateList.EnumerateValues()) {
                        if (!iconValue.TryGetValueAsString(out var iconState))
                            continue;

                        _iconStates.Add(iconState);
                    }
                } else if (value.TryGetValueAsString(out var iconState)) {
                    _iconStates.Add(iconState);
                }

                immutableAppearances = new();
                foreach (var icon in _icons) {
                    foreach (var iconState in _iconStates) {
                        MutableAppearance iconCombo = MutableAppearance.GetCopy(icon);
                        iconCombo.IconState = iconState;
                        immutableAppearances.Add(AppearanceSystem!.AddAppearance(iconCombo));
                    }
                }

                ParticlesComponent.TextureList = immutableAppearances.ToArray();
                break;
            case "lifespan": //num or generator
                if (value.TryGetValueAsFloat(out float floatValue)) {
                    ParticlesComponent.LifespanHigh = floatValue;
                    ParticlesComponent.LifespanLow = floatValue;
                    ParticlesComponent.LifespanDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.LifespanHigh = dreamObjectGenerator.B.UnsafeGetValueAsFloat();
                    ParticlesComponent.LifespanLow = dreamObjectGenerator.A.UnsafeGetValueAsFloat();
                    ParticlesComponent.LifespanDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.LifespanType = dreamObjectGenerator.OutputType;
                }

                break;
            case "fadein": //num or generator
                if (value.TryGetValueAsInteger(out int intValue)) {
                    ParticlesComponent.FadeInHigh = intValue;
                    ParticlesComponent.FadeInLow = intValue;
                    ParticlesComponent.FadeInDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.FadeInHigh = (int)dreamObjectGenerator.B.UnsafeGetValueAsFloat();
                    ParticlesComponent.FadeInLow = (int)dreamObjectGenerator.A.UnsafeGetValueAsFloat();
                    ParticlesComponent.FadeInDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.FadeInType = dreamObjectGenerator.OutputType;
                }

                break;
            case "fade": //num or generator
                if (value.TryGetValueAsInteger(out intValue)) {
                    ParticlesComponent.FadeOutHigh = intValue;
                    ParticlesComponent.FadeOutLow = intValue;
                    ParticlesComponent.FadeOutDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.FadeOutHigh = (int)dreamObjectGenerator.B.UnsafeGetValueAsFloat();
                    ParticlesComponent.FadeOutLow = (int)dreamObjectGenerator.A.UnsafeGetValueAsFloat();
                    ParticlesComponent.FadeOutDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.FadeOutType = dreamObjectGenerator.OutputType;
                }

                break;
            case "position": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.SpawnPositionHigh = new Vector3(floatValue);
                    ParticlesComponent.SpawnPositionLow = new Vector3(floatValue);
                    ParticlesComponent.SpawnPositionDist = GeneratorDistribution.Constant;
                }

                if (value.TryGetValueAsDreamList(out var vectorList) && vectorList.GetLength() >= 3) {
                    var x = vectorList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var y = vectorList.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var z = vectorList.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.SpawnPositionHigh = new Vector3(x, y, z);
                    ParticlesComponent.SpawnPositionLow = new Vector3(x, y, z);
                    ParticlesComponent.SpawnPositionDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.SpawnPositionHigh = GetGeneratorValueAsVector3(dreamObjectGenerator.B);
                    ParticlesComponent.SpawnPositionLow = GetGeneratorValueAsVector3(dreamObjectGenerator.A);
                    ParticlesComponent.SpawnPositionDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.SpawnPositionType = dreamObjectGenerator.OutputType;
                }

                break;
            case "velocity": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.SpawnVelocityHigh = new Vector3(floatValue);
                    ParticlesComponent.SpawnVelocityLow = new Vector3(floatValue);
                    ParticlesComponent.SpawnVelocityDist = GeneratorDistribution.Constant;
                }

                if (value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 3) {
                    var x = vectorList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var y = vectorList.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var z = vectorList.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.SpawnVelocityHigh = new Vector3(x, y, z);
                    ParticlesComponent.SpawnVelocityLow = new Vector3(x, y, z);
                    ParticlesComponent.SpawnVelocityDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.SpawnVelocityHigh = GetGeneratorValueAsVector3(dreamObjectGenerator.B);
                    ParticlesComponent.SpawnVelocityLow = GetGeneratorValueAsVector3(dreamObjectGenerator.A);
                    ParticlesComponent.SpawnVelocityDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.SpawnVelocityType = dreamObjectGenerator.OutputType;
                }

                break;
            case "scale": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.ScaleHigh = new Vector2(floatValue);
                    ParticlesComponent.ScaleLow = new Vector2(floatValue);
                    ParticlesComponent.ScaleDist = GeneratorDistribution.Constant;
                }

                if (value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 2) {
                    var x = vectorList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var y = vectorList.GetValue(new(2)).UnsafeGetValueAsFloat();

                    ParticlesComponent.ScaleHigh = new Vector2(x,y);
                    ParticlesComponent.ScaleLow = new Vector2(x,y);
                    ParticlesComponent.ScaleDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.ScaleHigh = GetGeneratorValueAsVector2(dreamObjectGenerator.B);
                    ParticlesComponent.ScaleLow = GetGeneratorValueAsVector2(dreamObjectGenerator.A);
                    ParticlesComponent.ScaleDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.ScaleType = dreamObjectGenerator.OutputType;
                }

                break;
            case "grow": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.GrowthHigh = new Vector2(floatValue);
                    ParticlesComponent.GrowthLow = new Vector2(floatValue);
                    ParticlesComponent.GrowthDist = GeneratorDistribution.Constant;
                }

                if (value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 2) {
                    var x = vectorList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var y = vectorList.GetValue(new(2)).UnsafeGetValueAsFloat();

                    ParticlesComponent.GrowthHigh = new Vector2(x, y);
                    ParticlesComponent.GrowthLow = new Vector2(x, y);
                    ParticlesComponent.GrowthDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.GrowthHigh = GetGeneratorValueAsVector2(dreamObjectGenerator.B);
                    ParticlesComponent.GrowthLow = GetGeneratorValueAsVector2(dreamObjectGenerator.A);
                    ParticlesComponent.GrowthDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.GrowthType = dreamObjectGenerator.OutputType;
                }

                break;
            case "rotation": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.RotationHigh = floatValue;
                    ParticlesComponent.RotationLow = floatValue;
                    ParticlesComponent.RotationDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.RotationHigh = dreamObjectGenerator.B.MustGetValueAsFloat();
                    ParticlesComponent.RotationLow = dreamObjectGenerator.A.MustGetValueAsFloat();
                    ParticlesComponent.RotationDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.RotationType = dreamObjectGenerator.OutputType;
                }

                break;
            case "spin": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.SpinHigh = floatValue;
                    ParticlesComponent.SpinLow = floatValue;
                    ParticlesComponent.SpinDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.SpinHigh = dreamObjectGenerator.B.MustGetValueAsFloat();
                    ParticlesComponent.SpinLow = dreamObjectGenerator.A.MustGetValueAsFloat();
                    ParticlesComponent.SpinDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.SpinType = dreamObjectGenerator.OutputType;
                }

                break;
            case "friction": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.FrictionHigh = new Vector3(floatValue);
                    ParticlesComponent.FrictionLow = new Vector3(floatValue);
                    ParticlesComponent.FrictionDist = GeneratorDistribution.Constant;
                }

                if (value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 3) {
                    var x = vectorList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var y = vectorList.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var z = vectorList.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.FrictionHigh = new Vector3(x, y, z);
                    ParticlesComponent.FrictionLow = new Vector3(x, y, z);
                    ParticlesComponent.FrictionDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.FrictionHigh = GetGeneratorValueAsVector3(dreamObjectGenerator.B);
                    ParticlesComponent.FrictionLow =  GetGeneratorValueAsVector3(dreamObjectGenerator.A);
                    ParticlesComponent.FrictionDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.FrictionType = dreamObjectGenerator.OutputType;
                }

                break;
            case "drift": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.DriftHigh = new Vector3(floatValue);
                    ParticlesComponent.DriftLow = new Vector3(floatValue);
                    ParticlesComponent.DriftDist = GeneratorDistribution.Constant;
                }

                if (value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 3) {
                    var x = vectorList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var y = vectorList.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var z = vectorList.GetValue(new(3)).UnsafeGetValueAsFloat();

                    ParticlesComponent.DriftHigh = new Vector3(x, y, z);
                    ParticlesComponent.DriftLow = new Vector3(x, y, z);
                    ParticlesComponent.DriftDist = GeneratorDistribution.Constant;
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.DriftHigh = GetGeneratorValueAsVector3(dreamObjectGenerator.B);
                    ParticlesComponent.DriftLow = GetGeneratorValueAsVector3(dreamObjectGenerator.A);
                    ParticlesComponent.DriftDist = dreamObjectGenerator.Distribution;
                    ParticlesComponent.DriftType = dreamObjectGenerator.OutputType;
                }

                break;
        }

        base.SetVar(varName, value); //all calls should set the internal vars, so GetVar() can just be default also
     }

     private Vector2 GetGeneratorValueAsVector2(DreamValue value) {
        if (value.TryGetValueAsFloat(out float floatValue)) {
            return new Vector2(floatValue);
        } //else vector

        //else list
        if (value.TryGetValueAsDreamList(out var valueList) && valueList.GetLength() >= 2) {
            var x = valueList.GetValue(new(1)).UnsafeGetValueAsFloat();
            var y = valueList.GetValue(new(2)).UnsafeGetValueAsFloat();

            return new Vector2(x, y);
        }

        throw new InvalidCastException("Expected a float, list, or vector");
     }

    private Vector3 GetGeneratorValueAsVector3(DreamValue value) {
        if (value.TryGetValueAsFloat(out float floatValue)) {
            return new Vector3(floatValue);
        } //else vector

        //else list
        if (value.TryGetValueAsDreamList(out var valueList) && valueList.GetLength() >= 3) {
            var x = valueList.GetValue(new(1)).UnsafeGetValueAsFloat();
            var y = valueList.GetValue(new(2)).UnsafeGetValueAsFloat();
            var z = valueList.GetValue(new(3)).UnsafeGetValueAsFloat();

            return new Vector3(x, y, z);
        }

        throw new InvalidCastException("Expected a float, list, or vector");
     }
}
