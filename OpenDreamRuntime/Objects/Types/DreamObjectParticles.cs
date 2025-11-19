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
                    ParticlesComponent.Lifespan = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Lifespan = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "fadein": //num or generator
                if (value.TryGetValueAsInteger(out int intValue)) {
                    ParticlesComponent.FadeIn = new GeneratorNum(intValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.FadeIn = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "fade": //num or generator
                if (value.TryGetValueAsInteger(out intValue)) {
                    ParticlesComponent.FadeOut = new GeneratorNum(intValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.FadeOut = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "position": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.SpawnPosition = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.SpawnPosition = generator.RequireType<IGeneratorVector>();
                } else {
                    var spawnPosition = DreamObjectVector.CreateFromValue(value, ObjectTree);

                    ParticlesComponent.SpawnPosition = new GeneratorVector3(spawnPosition.AsVector3);
                }

                break;
            case "velocity": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.SpawnVelocity = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.SpawnVelocity = generator.RequireType<IGeneratorVector>();
                } else {
                    var spawnVelocity = DreamObjectVector.CreateFromValue(value, ObjectTree);

                    ParticlesComponent.SpawnVelocity = new GeneratorVector3(spawnVelocity.AsVector3);
                }

                break;
            case "scale": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.Scale = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Scale = generator.RequireType<IGeneratorVector>();
                } else {
                    var scale = DreamObjectVector.CreateFromValue(value, ObjectTree);

                    ParticlesComponent.Scale = new GeneratorVector2(scale.AsVector2);
                }

                break;
            case "grow": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.Growth = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Growth = generator.RequireType<IGeneratorVector>();
                } else {
                    var growth = DreamObjectVector.CreateFromValue(value, ObjectTree);

                    ParticlesComponent.Growth = new GeneratorVector2(growth.AsVector2);
                }

                break;
            case "rotation": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.Rotation = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Rotation = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "spin": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.Spin = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Spin = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "friction": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.Friction = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Friction = generator.RequireType<IGeneratorVector>();
                } else {
                    var friction = DreamObjectVector.CreateFromValue(value, ObjectTree);

                    ParticlesComponent.Friction = new GeneratorVector3(friction.AsVector3);
                }

                break;
            case "drift": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    ParticlesComponent.Drift = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    ParticlesComponent.Drift = generator.RequireType<IGeneratorVector>();
                } else {
                    var drift = DreamObjectVector.CreateFromValue(value, ObjectTree);

                    ParticlesComponent.Drift = new GeneratorVector3(drift.AsVector3);
                }

                break;
        }

        base.SetVar(varName, value); //all calls should set the internal vars, so GetVar() can just be default also
     }
}
