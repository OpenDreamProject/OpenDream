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
        if (!_atomManager.TryCreateAppearanceFrom(icon, out var appearance)) {
            // Use a default appearance, but log a warning about it if icon wasn't null
            appearance = new IconAppearance();
            if (icon != DreamValue.Null)
                Logger.Warning($"Attempted to create an /image from {icon}. This is invalid and a default image was created instead.");
        }

        int argIndex = 1;
        DreamValue loc = creationArguments.GetArgument(1, "loc");
        if (loc.Type == DreamValue.DreamValueType.DreamObject) { // If it's not a DreamObject, it's actually icon_state and not loc
            dreamObject.SetVariableValue("loc", loc);
            argIndex = 2;
        }

        foreach (string argName in IconCreationArgs) {
            var arg = creationArguments.GetArgument(argIndex++, argName);
            if (arg == DreamValue.Null)
                continue;

            _atomManager.SetAppearanceVar(appearance, argName, arg);
            if (argName == "dir") {
                // If a dir is explicitly given in the constructor then overlays using this won't use their owner's dir
                // Setting dir after construction does not affect this
                // This is undocumented and I hate it
                appearance.InheritsDirection = false;
            }
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
                if (!_atomManager.TryCreateAppearanceFrom(value, out var newAppearance))
                    return; // Ignore attempts to set an invalid appearance

                // The dir does not get changed
                var oldDir = ObjectToAppearance[dreamObject].Direction;
                newAppearance.Direction = oldDir;

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
