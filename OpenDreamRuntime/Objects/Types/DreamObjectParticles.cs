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
        Entity = EntityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace)); //spawning an entity in nullspace means it never actually gets sent to any clients until it's placed on the map, or it gets a PVS override
        ParticlesComponent = EntityManager.AddComponent<DreamParticlesComponent>(Entity);
        //populate component with settings from type
        //do set/get var to grab those also
        //check if I need to manually send update events to the component?
        //add entity array to appearance objects
        //collect entities client-side for the rendermetadata
        //idk I guess bodge generators right now?
        //set up a special list type on /atom for /particles

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
                ParticlesComponent.LifespanHigh = value.GetValueAsFloat();
                ParticlesComponent.LifespanLow = value.GetValueAsFloat();
                ParticlesComponent.LifespanType = value.GetValueAsParticlePropertyType();
                break;
            case "fadein": //num or generator
                ParticlesComponent.FadeInHigh = value.GetValueAsInteger();
                ParticlesComponent.FadeInLow = value.GetValueAsInteger();
                ParticlesComponent.FadeInType = value.GetValueAsParticlePropertyType();
                break;
            case "fade": //num or generator
                ParticlesComponent.FadeOutHigh = value.GetValueAsInteger();
                ParticlesComponent.FadeOutLow = value.GetValueAsInteger();
                ParticlesComponent.FadeOutType = value.GetValueAsParticlePropertyType();
                break;
            case "position": //list, vector, or generator
                ParticlesComponent.SpawnPositionHigh = value.GetValueAsVector3();
                ParticlesComponent.SpawnPositionLow = value.GetValueAsVector3();
                ParticlesComponent.SpawnPositionType = value.GetValueAsParticlePropertyType();
                break;
            case "velocity": //list, vector, or generator
                ParticlesComponent.SpawnPositionHigh = value.GetValueAsVector3();
                ParticlesComponent.SpawnPositionLow = value.GetValueAsVector3();
                ParticlesComponent.SpawnPositionType = value.GetValueAsParticlePropertyType();
                break;
            case "scale": //num, list, vector, or generator
                ParticlesComponent.ScaleHigh = value.GetValueAsVector2();
                ParticlesComponent.ScaleLow = value.GetValueAsVector2();
                ParticlesComponent.ScaleType = value.GetValueAsParticlePropertyType();
                break;
            case "grow": //num, list, vector, or generator
                ParticlesComponent.GrowthHigh = value.GetValueAsFloat();
                ParticlesComponent.GrowthLow = value.GetValueAsFloat();
                ParticlesComponent.GrowthType = value.GetValueAsParticlePropertyType();
                break;
            case "rotation": //num or generator
                ParticlesComponent.RotationHigh = value.GetValueAsFloat();
                ParticlesComponent.RotationLow = value.GetValueAsFloat();
                ParticlesComponent.RotationType = value.GetValueAsParticlePropertyType();
                break;
            case "spin": //num or generator
                ParticlesComponent.SpinHigh = value.GetValueAsFloat();
                ParticlesComponent.SpinLow = value.GetValueAsFloat();
                ParticlesComponent.SpinType = value.GetValueAsParticlePropertyType();
                break;
            case "friction": //num or generator
                ParticlesComponent.FrictionHigh = value.GetValueAsFloat();
                ParticlesComponent.FrictionLow = value.GetValueAsFloat();
                ParticlesComponent.FrictionType = value.GetValueAsParticlePropertyType();
                break;
            case "drift": //num or generator
                ParticlesComponent.DriftHigh = value.GetValueAsVector3();
                ParticlesComponent.DriftLow = value.GetValueAsVector3();
                ParticlesComponent.DriftType = value.GetValueAsParticlePropertyType();
                break;
            default:
                base.SetVar(varName, value);
                break;
        }
     }
}
