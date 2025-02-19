using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.Map;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectParticles : DreamObject {
    private static readonly DreamResourceManager _resourceManager = IoCManager.Resolve<DreamResourceManager>();
    public EntityUid Entity = EntityUid.Invalid;
    public DreamParticlesComponent ParticlesComponent;

    private List<MutableAppearance> _icons = new();
    private List<string> _iconStates = new();

    public DreamObjectParticles(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Entity = EntityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace)); //spawning an entity in nullspace means it never actually gets sent to any clients until it's put in the particles list on an atom, when PVS override happens
        ParticlesComponent = EntityManager.AddComponent<DreamParticlesComponent>(Entity);
        //populate component with settings from type
        foreach(KeyValuePair<string,DreamValue> kv in objectDefinition.Variables){
            if(objectDefinition.ConstVariables is not null && !objectDefinition.ConstVariables.Contains(kv.Key))
                SetVar(kv.Key, kv.Value);
        }
        //check if I need to manually send update events to the component?
    }

     protected override void SetVar(string varName, DreamValue value) {
        //good news, these only update on assignment, so we don't need to track the generator, list, or matrix objects
        switch (varName) {
            case "width": //num
                ParticlesComponent.Width = value.MustGetValueAsInteger();
                break;
            case "height": //num
                ParticlesComponent.Height = value.MustGetValueAsInteger();
                break;
            case "count": //num
                ParticlesComponent.Count = value.MustGetValueAsInteger();
                break;
            case "spawning": //num
                ParticlesComponent.Spawning = value.MustGetValueAsFloat();
                break;
            case "bound1": //list or vector
                if(value.TryGetValueAsDreamList(out var bound1List) && bound1List.GetLength() >= 3) {
                    List<DreamValue> dreamValues = bound1List.GetValues();
                    ParticlesComponent.Bound1 = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                } //else if vector
                break;
            case "bound2": //list or vector
                 if(value.TryGetValueAsDreamList(out var bound2List) && bound2List.GetLength() >= 3) {
                    List<DreamValue> dreamValues = bound2List.GetValues();
                    ParticlesComponent.Bound2 = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                } //else if vector
                break;
            case "gravity": //list or vector
                if(value.TryGetValueAsDreamList(out var gravityList) && gravityList.GetLength() >= 3) {
                    List<DreamValue> dreamValues = gravityList.GetValues();
                    ParticlesComponent.Gravity = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                } //else if vector
                break;
            case "gradient": //color gradient list
                if(value.TryGetValueAsDreamList(out var colorList)){
                    List<Color> grad = new(colorList.GetLength());
                    foreach(DreamValue colorValue in colorList.GetValues()){
                        if (ColorHelpers.TryParseColor(colorValue.MustGetValueAsString(), out var c, defaultAlpha: string.Empty))
                            grad.Add(c);
                    }
                    ParticlesComponent.Gradient = grad.ToArray();
                }
                break;
            case "transform": //matrix
                if(value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrix)){
                    float[] m = DreamObjectMatrix.MatrixToTransformFloatArray(matrix);
                    ParticlesComponent.Transform = new(m[0],m[1],m[2],m[3],m[4],m[5]);
                }
                break;
            case "icon": //list or icon
                _icons.Clear();
                if(value.TryGetValueAsDreamList(out var iconList)){
                    foreach(DreamValue iconValue in iconList.GetValues()){
                        if(iconValue.TryGetValueAsDreamObject<DreamObjectIcon>(out var icon)){
                            _icons.Add(AtomManager.MustGetAppearance(icon).ToMutable());
                        }
                    }
                } else if(value.TryGetValueAsDreamObject<DreamObjectIcon>(out var dreamObjectIcon)) {
                    _icons.Add(AtomManager.MustGetAppearance(dreamObjectIcon).ToMutable());
                }
                List<ImmutableAppearance> immutableAppearances = new();
                foreach(var icon in _icons){
                    foreach(var iconState in _iconStates){
                        MutableAppearance iconCombo = MutableAppearance.GetCopy(icon);
                        iconCombo.IconState = iconState;
                        immutableAppearances.Add(AppearanceSystem!.AddAppearance(iconCombo));
                    }
                }
                ParticlesComponent.TextureList = immutableAppearances.ToArray();
                break;
            case "icon_state": //list or string
                _iconStates.Clear();
                if(value.TryGetValueAsDreamList(out var iconStateList)){
                    foreach(DreamValue iconValue in iconStateList.GetValues()){
                        if(iconValue.TryGetValueAsString(out var iconState)){
                            _iconStates.Add(iconState);
                        }
                    }
                } else if(value.TryGetValueAsString(out var iconState)) {
                    _iconStates.Add(iconState);
                }
                immutableAppearances = new();
                foreach(var icon in _icons){
                    foreach(var iconState in _iconStates){
                        MutableAppearance iconCombo = MutableAppearance.GetCopy(icon);
                        iconCombo.IconState = iconState;
                        immutableAppearances.Add(AppearanceSystem!.AddAppearance(iconCombo));
                    }
                }
                ParticlesComponent.TextureList = immutableAppearances.ToArray();
                break;
            case "lifespan": //num or generator
                if(value.TryGetValueAsFloat(out float floatValue)){
                    ParticlesComponent.LifespanHigh = floatValue;
                    ParticlesComponent.LifespanLow = floatValue;
                    ParticlesComponent.LifespanType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.LifespanHigh = dreamObjectGenerator.B.MustGetValueAsFloat();
                    ParticlesComponent.LifespanLow = dreamObjectGenerator.A.MustGetValueAsFloat();
                    ParticlesComponent.LifespanType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "fadein": //num or generator
                if(value.TryGetValueAsInteger(out int intValue)){
                    ParticlesComponent.FadeInHigh = intValue;
                    ParticlesComponent.FadeInLow = intValue;
                    ParticlesComponent.FadeInType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.FadeInHigh = dreamObjectGenerator.B.MustGetValueAsInteger();
                    ParticlesComponent.FadeInLow = dreamObjectGenerator.A.MustGetValueAsInteger();
                    ParticlesComponent.FadeInType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "fade": //num or generator
                if(value.TryGetValueAsInteger(out intValue)){
                    ParticlesComponent.FadeOutHigh = intValue;
                    ParticlesComponent.FadeOutLow = intValue;
                    ParticlesComponent.FadeOutType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.FadeOutHigh = dreamObjectGenerator.B.MustGetValueAsInteger();
                    ParticlesComponent.FadeOutLow = dreamObjectGenerator.A.MustGetValueAsInteger();
                    ParticlesComponent.FadeOutType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "position": //num, list, vector, or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.SpawnPositionHigh = new Vector3(floatValue);
                    ParticlesComponent.SpawnPositionLow = new Vector3(floatValue);
                    ParticlesComponent.SpawnPositionType = ParticlePropertyType.HighValue;
                }
                if(value.TryGetValueAsDreamList(out var vectorList) && vectorList.GetLength() >= 3){
                    List<DreamValue> dreamValues = vectorList.GetValues();
                    ParticlesComponent.SpawnPositionHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.SpawnPositionLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.SpawnPositionType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    List<DreamValue> dreamValues = dreamObjectGenerator.B.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.SpawnPositionHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    dreamValues = dreamObjectGenerator.A.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.SpawnPositionLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.SpawnPositionType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "velocity": //num, list, vector, or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.SpawnVelocityHigh = new Vector3(floatValue);
                    ParticlesComponent.SpawnVelocityLow = new Vector3(floatValue);
                    ParticlesComponent.SpawnVelocityType = ParticlePropertyType.HighValue;
                }
                if(value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 3){
                    List<DreamValue> dreamValues = vectorList.GetValues();
                    ParticlesComponent.SpawnVelocityHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.SpawnVelocityLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.SpawnVelocityType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    List<DreamValue> dreamValues = dreamObjectGenerator.B.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.SpawnVelocityHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    dreamValues = dreamObjectGenerator.A.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.SpawnVelocityLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.SpawnVelocityType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "scale": //num, list, vector, or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.ScaleHigh = new Vector2(floatValue);
                    ParticlesComponent.ScaleLow = new Vector2(floatValue);
                    ParticlesComponent.ScaleType = ParticlePropertyType.HighValue;
                }
                if(value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 2){
                    List<DreamValue> dreamValues = vectorList.GetValues();
                    ParticlesComponent.ScaleHigh = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    ParticlesComponent.ScaleLow = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    ParticlesComponent.ScaleType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    List<DreamValue> dreamValues = dreamObjectGenerator.B.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.ScaleHigh = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    dreamValues = dreamObjectGenerator.A.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.ScaleLow = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    ParticlesComponent.ScaleType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "grow": //num, list, vector, or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.GrowthHigh = new Vector2(floatValue);
                    ParticlesComponent.GrowthLow = new Vector2(floatValue);
                    ParticlesComponent.GrowthType = ParticlePropertyType.HighValue;
                }
                if(value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 2){
                    List<DreamValue> dreamValues = vectorList.GetValues();
                    ParticlesComponent.GrowthHigh = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    ParticlesComponent.GrowthLow = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    ParticlesComponent.GrowthType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    List<DreamValue> dreamValues = dreamObjectGenerator.B.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.GrowthHigh = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    dreamValues = dreamObjectGenerator.A.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.GrowthLow = new Vector2(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat());
                    ParticlesComponent.GrowthType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "rotation": //num or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.RotationHigh = floatValue;
                    ParticlesComponent.RotationLow = floatValue;
                    ParticlesComponent.RotationType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.RotationHigh = dreamObjectGenerator.B.MustGetValueAsFloat();
                    ParticlesComponent.RotationLow = dreamObjectGenerator.A.MustGetValueAsFloat();
                    ParticlesComponent.RotationType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "spin": //num or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.SpinHigh = floatValue;
                    ParticlesComponent.SpinLow = floatValue;
                    ParticlesComponent.SpinType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    ParticlesComponent.SpinHigh = dreamObjectGenerator.B.MustGetValueAsFloat();
                    ParticlesComponent.SpinLow = dreamObjectGenerator.A.MustGetValueAsFloat();
                    ParticlesComponent.SpinType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "friction": //num, vector, or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.FrictionHigh = new Vector3(floatValue);
                    ParticlesComponent.FrictionLow = new Vector3(floatValue);
                    ParticlesComponent.FrictionType = ParticlePropertyType.HighValue;
                }
                if(value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 3){
                    List<DreamValue> dreamValues = vectorList.GetValues();
                    ParticlesComponent.FrictionHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.FrictionLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.FrictionType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    List<DreamValue> dreamValues = dreamObjectGenerator.B.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.FrictionHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    dreamValues = dreamObjectGenerator.A.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.FrictionLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.FrictionType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
            case "drift": //num, vector, or generator
                if(value.TryGetValueAsFloat(out floatValue)){
                    ParticlesComponent.DriftHigh = new Vector3(floatValue);
                    ParticlesComponent.DriftLow = new Vector3(floatValue);
                    ParticlesComponent.DriftType = ParticlePropertyType.HighValue;
                }
                if(value.TryGetValueAsDreamList(out vectorList) && vectorList.GetLength() >= 3){
                    List<DreamValue> dreamValues = vectorList.GetValues();
                    ParticlesComponent.DriftHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.DriftLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.DriftType = ParticlePropertyType.HighValue;
                } else if(value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var dreamObjectGenerator)) {
                    List<DreamValue> dreamValues = dreamObjectGenerator.B.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.DriftHigh = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    dreamValues = dreamObjectGenerator.A.MustGetValueAsDreamList().GetValues();
                    ParticlesComponent.DriftLow = new Vector3(dreamValues[0].MustGetValueAsFloat(), dreamValues[1].MustGetValueAsFloat(), dreamValues[2].MustGetValueAsFloat());
                    ParticlesComponent.DriftType = ParticlePropertyType.RandomUniform; //TODO all the other distributions
                }
                break;
        }

        base.SetVar(varName, value); //all calls should set the internal vars, so GetVar() can just be default also
     }
}
