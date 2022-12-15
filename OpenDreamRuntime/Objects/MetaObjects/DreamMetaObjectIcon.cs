using System.IO;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using ParsedDMIDescription = OpenDreamShared.Resources.DMIParser.ParsedDMIDescription;

namespace OpenDreamRuntime.Objects.MetaObjects;

sealed class DreamMetaObjectIcon : IDreamMetaObject {
    public bool ShouldCallNew => true;
    public IDreamMetaObject? ParentType { get; set; }

    [Dependency] private readonly DreamResourceManager _rscMan = default!;
    [Dependency] private readonly IDreamObjectTree _objectTree = default!;

    public DreamMetaObjectIcon() {
        IoCManager.InjectDependencies(this);
    }

    public static readonly Dictionary<DreamObject, DreamIcon> ObjectToDreamIcon = new();

    public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
        ParentType?.OnObjectCreated(dreamObject, creationArguments);

        // TODO confirm BYOND behavior of invalid args for icon, dir, and frame
        DreamValue icon = creationArguments.GetArgument(0, "icon");
        DreamValue state = creationArguments.GetArgument(1, "icon_state");
        DreamValue dir = creationArguments.GetArgument(2, "dir");
        DreamValue frame = creationArguments.GetArgument(3, "frame");
        DreamValue moving = creationArguments.GetArgument(4, "moving");

        DreamIcon dreamIcon = new(_rscMan);
        ObjectToDreamIcon.Add(dreamObject, dreamIcon);

        if (icon != DreamValue.Null) {
            // TODO: Could maybe have an alternative path for /icon values so the DMI doesn't have to be generated
            var (iconRsc, iconDescription) = GetIconResourceAndDescription(_objectTree, _rscMan, icon);

            dreamIcon.InsertStates(iconRsc, iconDescription, state, dir, frame, useStateName: false);
        }
    }

    public void OnObjectDeleted(DreamObject dreamObject) {
        ObjectToDreamIcon.Remove(dreamObject);

        ParentType?.OnObjectDeleted(dreamObject);
    }

    public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
        ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

        switch (varName) {
            case "icon":
                // Setting the icon to anything other than a DreamResource will actually set it to null
                if (value.Type != DreamValue.DreamValueType.DreamResource) {
                    dreamObject.SetVariableValue("icon", DreamValue.Null);
                }

                break;
        }
    }

    public static (DreamResource Resource, ParsedDMIDescription Description) GetIconResourceAndDescription(
        IDreamObjectTree objectTree, DreamResourceManager resourceManager, DreamValue value) {
        if (value.TryGetValueAsDreamObjectOfType(objectTree.Icon, out var iconObj)) {
            DreamIcon dreamIcon = ObjectToDreamIcon[iconObj];

            return dreamIcon.GenerateDMI();
        }

        DreamResource? iconRsc;

        if (value.TryGetValueAsString(out var fileString)) {
            var ext = Path.GetExtension(fileString);

            switch (ext) {
                case ".dmi":
                    iconRsc = resourceManager.LoadResource(fileString);
                    break;

                // TODO implement other icon file types
                case ".png":
                case ".jpg":
                case ".rsi": // RT-specific, not in BYOND
                case ".gif":
                case ".bmp":
                    throw new NotImplementedException($"Unimplemented icon type '{ext}'");
                default:
                    throw new Exception($"Invalid icon file {fileString}");
            }
        } else if (!value.TryGetValueAsDreamResource(out iconRsc)) {
            throw new Exception($"Invalid icon {value}");
        }

        byte[]? rscData = iconRsc.ResourceData;
        if (rscData == null)
            throw new Exception($"No data in file {iconRsc} to construct icon from");

        return (iconRsc, DMIParser.ParseDMI(new MemoryStream(rscData)));
    }
}
