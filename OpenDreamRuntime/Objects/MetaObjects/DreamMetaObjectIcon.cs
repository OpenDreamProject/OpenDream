using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Objects.MetaObjects;

sealed class DreamMetaObjectIcon : IDreamMetaObject {
    public bool ShouldCallNew => true;
    public IDreamMetaObject? ParentType { get; set; }

    [Dependency] private readonly DreamResourceManager _rscMan = default!;

    public DreamMetaObjectIcon() {
        IoCManager.InjectDependencies(this);
    }

    public static readonly Dictionary<DreamObject, DreamIcon> ObjectToDreamIcon = new();

    public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
        ParentType?.OnObjectCreated(dreamObject, creationArguments);

        // TODO confirm BYOND behavior of invalid args for icon, dir, and frame
        DreamValue icon = creationArguments.GetArgument(0);
        DreamValue state = creationArguments.GetArgument(1);
        DreamValue dir = creationArguments.GetArgument(2);
        DreamValue frame = creationArguments.GetArgument(3);
        DreamValue moving = creationArguments.GetArgument(4);

        var dreamIcon = InitializeIcon(_rscMan, dreamObject);

        if (icon != DreamValue.Null) {
            // TODO: Could maybe have an alternative path for /icon values so the DMI doesn't have to be generated
            if (!_rscMan.TryLoadIcon(icon, out var iconRsc))
                throw new Exception($"Cannot create an icon from {icon}");

            dreamIcon.InsertStates(iconRsc, state, dir, frame, useStateName: false);
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

    /// <summary>
    /// A fast path for initializing an /icon object
    /// </summary>
    /// <remarks>Doesn't call any DM code</remarks>
    /// <returns>The /icon's DreamIcon</returns>
    public static DreamIcon InitializeIcon(DreamResourceManager rscMan, DreamObject icon) {
        DreamIcon dreamIcon = new(rscMan);

        ObjectToDreamIcon.Add(icon, dreamIcon);
        return dreamIcon;
    }

    public static DreamObject CloneIcon(IDreamObjectTree objectTree, DreamObject icon) {
        // var newIcon = objectTree.CreateObject(icon.ObjectDefinition.TreeEntry);
        // newIcon.InitSpawn(new Procs.DreamProcArguments(args));
        //TODO: actually clone the icon
        return icon;
    }

    public static DreamIcon TurnIcon(DreamIcon icon, float angle) {
        //TODO: actually rotate the icon clockwise x degrees
        return icon;
    }
}
