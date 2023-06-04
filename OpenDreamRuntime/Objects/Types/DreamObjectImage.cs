using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectImage : DreamObject {
    public IconAppearance? Appearance;

    private DreamObject? _loc;

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

    public DreamObjectImage(DreamObjectDefinition objectDefinition) : base(objectDefinition) {

    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        DreamValue icon = args.GetArgument(0);
        if (!AtomManager.TryCreateAppearanceFrom(icon, out Appearance)) {
            // Use a default appearance, but log a warning about it if icon wasn't null
            Appearance = new IconAppearance();
            if (icon != DreamValue.Null)
                Logger.Warning($"Attempted to create an /image from {icon}. This is invalid and a default image was created instead.");
        }

        int argIndex = 1;
        DreamValue loc = args.GetArgument(1);
        if (loc.TryGetValueAsDreamObject(out _loc)) { // If it's not a DreamObject, it's actually icon_state and not loc
            argIndex = 2;
        }

        foreach (string argName in IconCreationArgs) {
            var arg = args.GetArgument(argIndex++);
            if (arg == DreamValue.Null)
                continue;

            AtomManager.SetAppearanceVar(Appearance, argName, arg);
            if (argName == "dir") {
                // If a dir is explicitly given in the constructor then overlays using this won't use their owner's dir
                // Setting dir after construction does not affect this
                // This is undocumented and I hate it
                Appearance.InheritsDirection = false;
            }
        }
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        if (AtomManager.IsValidAppearanceVar(varName)) {
            value = AtomManager.GetAppearanceVar(Appearance!, varName);
            return true;
        } else if (varName == "appearance") {
            IconAppearance appearanceCopy = new IconAppearance(Appearance!); // Return a copy

            value = new(appearanceCopy);
            return true;
        } else if (varName == "loc") {
            value = new(_loc);
            return true;
        }

        // TODO: overlays, underlays, filters, transform

        return base.TryGetVar(varName, out value);
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "appearance":
                if (!AtomManager.TryCreateAppearanceFrom(value, out var newAppearance))
                    return; // Ignore attempts to set an invalid appearance

                // The dir does not get changed
                var oldDir = Appearance!.Direction;
                newAppearance.Direction = oldDir;

                Appearance = newAppearance;
                break;
            case "loc":
                value.TryGetValueAsDreamObject(out _loc);
                break;
            default:
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    AtomManager.SetAppearanceVar(Appearance!, varName, value);
                    break;
                }

                base.SetVar(varName, value);
                break;
        }
    }
}
