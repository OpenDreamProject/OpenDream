using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectIcon : DreamObject {
    public DreamIcon Icon;

    public DreamObjectIcon(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Icon = new(DreamManager, DreamResourceManager);
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        // TODO confirm BYOND behavior of invalid args for icon, dir, and frame
        DreamValue icon = args.GetArgument(0);
        DreamValue state = args.GetArgument(1);
        DreamValue dir = args.GetArgument(2);
        DreamValue frame = args.GetArgument(3);
        DreamValue moving = args.GetArgument(4);

        if (!icon.IsNull) {
            if (icon.TryGetValueAsDreamObject<DreamObjectIcon>(out var iconObj)) {
                // Copy the DreamIcon rather than create the entire DMI from it
                Icon.CopyFrom(iconObj.Icon);
            } else {
                if (!DreamResourceManager.TryLoadIcon(icon, out var iconRsc))
                    throw new Exception($"Cannot create an icon from {icon}");

                Icon.InsertStates(iconRsc, state, dir, frame, isConstructor: true);
            }
        }
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "icon":
                // TODO: Figure out what this gives you (whatever ref ID 0xC is)
                value = DreamValue.Null;
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "icon":
                break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    public DreamObjectIcon Clone() {
        var newIcon = new DreamObjectIcon(ObjectDefinition) {
            Icon = Icon //TODO: actually clone the icon
        };

        return newIcon;
    }

    public void Turn(float angle) {
        //TODO: actually rotate the icon clockwise x degrees
    }
}
