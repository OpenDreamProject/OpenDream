using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectParticles : DreamObject {
    public DreamObjectMovable? Owner {
        set {
            if (field != null)
                EntityManager.RemoveComponent(field.Entity, _particlesComponent);

            field = value;
            if (field != null)
                EntityManager.AddComponent(field.Entity, _particlesComponent);
        }
    }

    private DreamParticlesComponent _particlesComponent;
    private List<MutableAppearance> _icons = new();
    private List<string> _iconStates = new();

    public DreamObjectParticles(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        _particlesComponent = new DreamParticlesComponent();

        //populate component with settings from type
        foreach (KeyValuePair<string,DreamValue> kv in ObjectDefinition.Variables) {
            if (kv.Key is not ("parent_type" or "type" or "vars"))
                SetVar(kv.Key, kv.Value);
        }
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        Owner = null;
        _icons = null!;
        _iconStates = null!;
        _particlesComponent = null!;

        base.HandleDeletion(possiblyThreaded);
    }

    protected override void SetVar(string varName, DreamValue value) {
        //good news, these only update on assignment, so we don't need to track the generator, list, or matrix objects
        switch (varName) {
            case "width": //num
                _particlesComponent.Width = (int)value.UnsafeGetValueAsFloat();
                break;
            case "height": //num
                _particlesComponent.Height = (int)value.UnsafeGetValueAsFloat();
                break;
            case "count": //num
                if (!value.TryGetValueAsInteger(out var count))
                    break;

                _particlesComponent.Count = count;
                break;
            case "spawning": //num
                _particlesComponent.Spawning = value.UnsafeGetValueAsFloat();
                break;
            case "bound1": //list or vector
                if (value.TryGetValueAsDreamList(out var bound1List) && bound1List.GetLength() >= 3) {
                    var boundX = bound1List.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var boundY = bound1List.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var boundZ = bound1List.GetValue(new(3)).UnsafeGetValueAsFloat();

                    _particlesComponent.Bound1 = new Vector3(boundX, boundY, boundZ);
                } //else if vector

                break;
            case "bound2": //list or vector
                if (value.TryGetValueAsDreamList(out var bound2List) && bound2List.GetLength() >= 3) {
                    var boundX = bound2List.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var boundY = bound2List.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var boundZ = bound2List.GetValue(new(3)).UnsafeGetValueAsFloat();

                    _particlesComponent.Bound2 = new Vector3(boundX, boundY, boundZ);
                } //else if vector

                break;
            case "gravity": //list or vector
                if (value.TryGetValueAsDreamList(out var gravityList) && gravityList.GetLength() >= 3) {
                    var gravityX = gravityList.GetValue(new(1)).UnsafeGetValueAsFloat();
                    var gravityY = gravityList.GetValue(new(2)).UnsafeGetValueAsFloat();
                    var gravityZ = gravityList.GetValue(new(3)).UnsafeGetValueAsFloat();

                    _particlesComponent.Gravity = new Vector3(gravityX, gravityY, gravityZ);
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

                    _particlesComponent.Gradient = grad;
                }

                break;
            case "transform": //matrix
                if (value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrix)) {
                    float[] m = DreamObjectMatrix.MatrixToTransformFloatArray(matrix);
                    _particlesComponent.Transform = new(m[0], m[1], m[2], m[3], m[4], m[5]);
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

                _particlesComponent.TextureList = immutableAppearances.ToArray();
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

                _particlesComponent.TextureList = immutableAppearances.ToArray();
                break;
            case "lifespan": //num or generator
                if (value.TryGetValueAsFloat(out float floatValue)) {
                    _particlesComponent.Lifespan = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Lifespan = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "fadein": //num or generator
                if (value.TryGetValueAsInteger(out int intValue)) {
                    _particlesComponent.FadeIn = new GeneratorNum(intValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.FadeIn = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "fade": //num or generator
                if (value.TryGetValueAsInteger(out intValue)) {
                    _particlesComponent.FadeOut = new GeneratorNum(intValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.FadeOut = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "position": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.SpawnPosition = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.SpawnPosition = generator.RequireType<IGeneratorVector>();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesComponent.SpawnPosition = new GeneratorVector2(vector.AsVector2);
                } else {
                    _particlesComponent.SpawnPosition = new GeneratorVector2(Vector2.Zero);
                }

                break;
            case "velocity": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.SpawnVelocity = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.SpawnVelocity = generator.RequireType<IGeneratorVector>();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesComponent.SpawnVelocity = new GeneratorVector2(vector.AsVector2);
                } else {
                    _particlesComponent.SpawnVelocity = new GeneratorVector2(Vector2.Zero);
                }

                break;
            case "scale": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.Scale = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Scale = generator.RequireType<IGeneratorVector>();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesComponent.Scale = new GeneratorVector2(vector.AsVector2);
                } else {
                    _particlesComponent.Scale = new GeneratorVector2(Vector2.One);
                }

                break;
            case "grow": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.Growth = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Growth = generator.RequireType<IGeneratorVector>();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesComponent.Growth = new GeneratorVector2(vector.AsVector2);
                } else {
                    _particlesComponent.Growth = new GeneratorVector2(Vector2.Zero);
                }

                break;
            case "rotation": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.Rotation = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Rotation = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "spin": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.Spin = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Spin = generator.RequireType<IGeneratorNum>();
                }

                break;
            case "friction": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.Friction = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Friction = generator.RequireType<IGeneratorVector>();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesComponent.Friction = new GeneratorVector2(vector.AsVector2);
                } else {
                    _particlesComponent.Friction = new GeneratorVector2(Vector2.Zero);
                }

                break;
            case "drift": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesComponent.Drift = new GeneratorNum(floatValue);
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesComponent.Drift = generator.RequireType<IGeneratorVector>();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesComponent.Drift = new GeneratorVector2(vector.AsVector2);
                } else {
                    _particlesComponent.Drift = new GeneratorVector2(Vector2.Zero);
                }

                break;
        }

        base.SetVar(varName, value); //all calls should set the internal vars, so GetVar() can just be default also
     }
}
