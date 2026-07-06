using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectParticles : DreamObject {
    private DreamParticlesComponent.ParticleData _particlesData;
    private List<MutableAppearance> _icons = new();
    private List<string> _iconStates = new();
    private Dictionary<DreamObjectMovable, DreamParticlesComponent> _owners;

    public DreamObjectParticles(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        _owners = [];
        _particlesData = new();

        //populate component with settings from type
        foreach (KeyValuePair<string,DreamValue> kv in ObjectDefinition.Variables) {
            if (kv.Key is not ("parent_type" or "type" or "vars"))
                SetVar(kv.Key, kv.Value);
        }
    }

    protected override void HandleDeletion() {
        foreach(var owner in _owners.Keys) {
            RemoveOwner(owner);
        }

        _icons = null!;
        _iconStates = null!;
        _particlesData = null!;
        _owners = null!;

        base.HandleDeletion();
    }

    public void AddOwner(DreamObjectMovable movable) {
        if(Deleting || Deleted)
            throw new InvalidOperationException($"{movable} tried to add new owner to deleted particles");
        if(movable.Deleting || movable.Deleted)
            throw new InvalidOperationException($"deleted movable tried to own particles {this}");
        if(_owners.ContainsKey(movable))
            throw new ArgumentException($"{movable} tried to own {this} while already owning");

        DreamParticlesComponent component = new() { Data = _particlesData };
        EntityManager.AddComponent(movable.Entity, component);
        _owners.Add(movable, component);
    }

    public void RemoveOwner(DreamObjectMovable movable) {
        if(!_owners.TryGetValue(movable, out var component)) {
            throw new ArgumentException($"{movable} tried to remove {this} but doesn't own it");
        }

        EntityManager.RemoveComponent(movable.Entity, component);
        component.Data = null!;
        _owners.Remove(movable);
    }

    private void MarkDirty() {
        foreach((var movable, var component) in _owners) {
            EntityManager.Dirty(movable.Entity, component);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        //good news, these only update on assignment, so we don't need to track the generator, list, or matrix objects
        switch (varName) {
            case "width": //num
                _particlesData.Width = (int)value.UnsafeGetValueAsFloat();
                MarkDirty();
                break;
            case "height": //num
                _particlesData.Height = (int)value.UnsafeGetValueAsFloat();
                MarkDirty();
                break;
            case "count": //num
                if (!value.TryGetValueAsInteger(out var count))
                    break;

                _particlesData.Count = count;
                MarkDirty();
                break;
            case "spawning": //num
                _particlesData.Spawning = value.UnsafeGetValueAsFloat();
                MarkDirty();
                break;
            case "bound1": //list or vector
                if (value.TryGetValueAsDreamList(out var bound1List) && bound1List.GetLength() >= 3) {
                    using var boundXValue = bound1List.GetValue(new(1));
                    using var boundYValue = bound1List.GetValue(new(2));
                    using var boundZValue = bound1List.GetValue(new(3));
                    var boundX = boundXValue.UnsafeGetValueAsFloat();
                    var boundY = boundYValue.UnsafeGetValueAsFloat();
                    var boundZ = boundZValue.UnsafeGetValueAsFloat();

                    _particlesData.Bound1 = new Vector3(boundX, boundY, boundZ);
                    MarkDirty();
                } //else if vector

                break;
            case "bound2": //list or vector
                if (value.TryGetValueAsDreamList(out var bound2List) && bound2List.GetLength() >= 3) {
                    using var boundXValue = bound2List.GetValue(new(1));
                    using var boundYValue = bound2List.GetValue(new(2));
                    using var boundZValue = bound2List.GetValue(new(3));
                    var boundX = boundXValue.UnsafeGetValueAsFloat();
                    var boundY = boundYValue.UnsafeGetValueAsFloat();
                    var boundZ = boundZValue.UnsafeGetValueAsFloat();

                    _particlesData.Bound2 = new Vector3(boundX, boundY, boundZ);
                    MarkDirty();
                } //else if vector

                break;
            case "gravity": //list or vector
                if (value.TryGetValueAsDreamList(out var gravityList) && gravityList.GetLength() >= 3) {
                    using var gravityXValue = gravityList.GetValue(new(1));
                    using var gravityYValue = gravityList.GetValue(new(2));
                    using var gravityZValue = gravityList.GetValue(new(3));
                    var gravityX = gravityXValue.UnsafeGetValueAsFloat();
                    var gravityY = gravityYValue.UnsafeGetValueAsFloat();
                    var gravityZ = gravityZValue.UnsafeGetValueAsFloat();

                    _particlesData.Gravity = new Vector3(gravityX, gravityY, gravityZ);
                    MarkDirty();
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

                    _particlesData.Gradient = grad;
                    MarkDirty();
                }

                break;
            case "transform": //matrix
                if (value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrix)) {
                    float[] m = DreamObjectMatrix.MatrixToTransformFloatArray(matrix);
                    _particlesData.Transform = new(m[0], m[1], m[2], m[3], m[4], m[5]);
                    MarkDirty();
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

                _particlesData.TextureList = immutableAppearances.ToArray();
                MarkDirty();
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

                _particlesData.TextureList = immutableAppearances.ToArray();
                MarkDirty();
                break;
            case "lifespan": //num or generator
                if (value.TryGetValueAsFloat(out float floatValue)) {
                    _particlesData.Lifespan = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Lifespan = generator.RequireType<IGeneratorNum>();
                    MarkDirty();
                }

                break;
            case "fadein": //num or generator
                if (value.TryGetValueAsInteger(out int intValue)) {
                    _particlesData.FadeIn = new GeneratorNum(intValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.FadeIn = generator.RequireType<IGeneratorNum>();
                    MarkDirty();
                }

                break;
            case "fade": //num or generator
                if (value.TryGetValueAsInteger(out intValue)) {
                    _particlesData.FadeOut = new GeneratorNum(intValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.FadeOut = generator.RequireType<IGeneratorNum>();
                    MarkDirty();
                }

                break;
            case "position": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.SpawnPosition = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.SpawnPosition = generator.RequireType<IGeneratorVector>();
                    MarkDirty();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesData.SpawnPosition = new GeneratorVector2(vector.AsVector2);
                    MarkDirty();
                    vector.DecRef();
                } else {
                    _particlesData.SpawnPosition = new GeneratorVector2(Vector2.Zero);
                    MarkDirty();
                }

                break;
            case "velocity": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.SpawnVelocity = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.SpawnVelocity = generator.RequireType<IGeneratorVector>();
                    MarkDirty();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesData.SpawnVelocity = new GeneratorVector2(vector.AsVector2);
                    MarkDirty();
                    vector.DecRef();
                } else {
                    _particlesData.SpawnVelocity = new GeneratorVector2(Vector2.Zero);
                    MarkDirty();
                }

                break;
            case "scale": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.Scale = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Scale = generator.RequireType<IGeneratorVector>();
                    MarkDirty();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesData.Scale = new GeneratorVector2(vector.AsVector2);
                    MarkDirty();
                    vector.DecRef();
                } else {
                    _particlesData.Scale = new GeneratorVector2(Vector2.One);
                    MarkDirty();
                }

                break;
            case "grow": //num, list, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.Growth = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Growth = generator.RequireType<IGeneratorVector>();
                    MarkDirty();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesData.Growth = new GeneratorVector2(vector.AsVector2);
                    MarkDirty();
                    vector.DecRef();
                } else {
                    _particlesData.Growth = new GeneratorVector2(Vector2.Zero);
                    MarkDirty();
                }

                break;
            case "rotation": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.Rotation = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Rotation = generator.RequireType<IGeneratorNum>();
                    MarkDirty();
                }

                break;
            case "spin": //num or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.Spin = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Spin = generator.RequireType<IGeneratorNum>();
                    MarkDirty();
                }

                break;
            case "friction": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.Friction = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Friction = generator.RequireType<IGeneratorVector>();
                    MarkDirty();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesData.Friction = new GeneratorVector2(vector.AsVector2);
                    MarkDirty();
                    vector.DecRef();
                } else {
                    _particlesData.Friction = new GeneratorVector2(Vector2.Zero);
                    MarkDirty();
                }

                break;
            case "drift": //num, vector, or generator
                if (value.TryGetValueAsFloat(out floatValue)) {
                    _particlesData.Drift = new GeneratorNum(floatValue);
                    MarkDirty();
                } else if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
                    _particlesData.Drift = generator.RequireType<IGeneratorVector>();
                    MarkDirty();
                } else if (DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var vector)) {
                    _particlesData.Drift = new GeneratorVector2(vector.AsVector2);
                    MarkDirty();
                    vector.DecRef();
                } else {
                    _particlesData.Drift = new GeneratorVector2(Vector2.Zero);
                    MarkDirty();
                }

                break;
        }

        base.SetVar(varName, value); //all calls should set the internal vars, so GetVar() can just be default also
     }
}
