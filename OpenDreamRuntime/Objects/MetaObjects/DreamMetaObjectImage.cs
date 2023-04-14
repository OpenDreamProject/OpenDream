using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects;

sealed class DreamMetaObjectImage : IDreamMetaObject {
    public bool ShouldCallNew => true;
    public IDreamMetaObject? ParentType { get; set; }

    [Dependency] private readonly IAtomManager _atomManager = default!;

    public DreamMetaObjectImage() {
        IoCManager.InjectDependencies(this);
    }

    public static readonly Dictionary<DreamObject, IconAppearance> ObjectToAppearance = new();

    /// <summary>
    /// All the args in /image/New() after "icon" and "loc", in their correct order
    /// </summary>
    private static readonly string[] IconCreationArgs = {
        "icon_state",
        "layer",
        "dir",
        "pixel_x",
        "pixel_y"
    };

    public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
        ParentType?.OnObjectCreated(dreamObject, creationArguments);

        DreamValue icon = creationArguments.GetArgument(0, "icon");
        IconAppearance appearance = _atomManager.CreateAppearanceFrom(icon);

        int argIndex = 1;
        DreamValue loc = creationArguments.GetArgument(1, "loc");
        if (loc.Type != DreamValue.DreamValueType.String) { // If it's a string, it's actually icon_state and not loc
            dreamObject.SetVariableValue("loc", loc);
            argIndex = 2;
        }

        foreach (string argName in IconCreationArgs) {
            var arg = creationArguments.GetArgument(argIndex++, argName);
            if (arg == DreamValue.Null)
                continue;

            _atomManager.SetAppearanceVar(appearance, argName, arg);
        }

        ObjectToAppearance.Add(dreamObject, appearance);
    }

    public void OnObjectDeleted(DreamObject dreamObject) {
        ObjectToAppearance.Remove(dreamObject);

        ParentType?.OnObjectDeleted(dreamObject);
    }

    public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
        switch (varName) {
            case "appearance":
                var newAppearance = _atomManager.CreateAppearanceFrom(value);

                ObjectToAppearance[dreamObject] = newAppearance;
                break;
            default:
                if (_atomManager.IsValidAppearanceVar(varName)) {
                    IconAppearance appearance = ObjectToAppearance[dreamObject];

                    _atomManager.SetAppearanceVar(appearance, varName, value);
                } else {
                    ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);
                }

                break;
        }
    }

    public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
        if (_atomManager.IsValidAppearanceVar(varName)) {
            IconAppearance appearance = ObjectToAppearance[dreamObject];

            return _atomManager.GetAppearanceVar(appearance, varName);
        } else if (varName == "appearance") {
            IconAppearance appearance = ObjectToAppearance[dreamObject];
            IconAppearance appearanceCopy = new IconAppearance(appearance);

            // TODO: overlays, underlays, filters, transform
            return new(appearanceCopy);
        }

        return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
    }
}
