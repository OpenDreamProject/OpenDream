using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Rendering;

sealed class DreamMetaObjectImage : IDreamMetaObject {
    public bool ShouldCallNew => true;
    public IDreamMetaObject? ParentType { get; set; }
    [Dependency] private readonly IDreamObjectTree _objectTree = default!;
    [Dependency] private readonly IAtomManager _atomManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    private ServerAppearanceSystem? _appearanceSystem;

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

        DreamValue icon = creationArguments.GetArgument(0);
        if (!_atomManager.TryCreateAppearanceFrom(icon, out var appearance)) {
            // Use a default appearance, but log a warning about it if icon wasn't null
            appearance = new IconAppearance();
            if (icon != DreamValue.Null)
                Logger.Warning($"Attempted to create an /image from {icon}. This is invalid and a default image was created instead.");
        }

        int argIndex = 1;
        DreamValue loc = creationArguments.GetArgument(1);
        if (loc.Type == DreamValue.DreamValueType.DreamObject) { // If it's not a DreamObject, it's actually icon_state and not loc
            dreamObject.SetVariableValue("loc", loc);
            argIndex = 2;
        }

        foreach (string argName in IconCreationArgs) {
            var arg = creationArguments.GetArgument(argIndex++);
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
        // TODO: These should use their own special list types
        dreamObject.SetVariable("overlays", new(_objectTree.CreateList()));
        dreamObject.SetVariable("underlays", new(_objectTree.CreateList()));
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
            case "overlays": {
                if (oldValue.TryGetValueAsDreamList(out var oldList)) {
                    oldList.Cut();
                    oldList.ValueAssigned -= OverlayValueAssigned;
                    oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                    _atomManager.OverlaysListToAtom.Remove(oldList);
                }

                if (!value.TryGetValueAsDreamList(out var overlayList)) {
                    overlayList = _objectTree.CreateList();
                }

                overlayList.ValueAssigned += OverlayValueAssigned;
                overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                _atomManager.OverlaysListToAtom[overlayList] = dreamObject;
                dreamObject.SetVariableValue(varName, new DreamValue(overlayList));
                break;
            }
            case "underlays": {
                if (oldValue.TryGetValueAsDreamList(out var oldList)) {
                    oldList.Cut();
                    oldList.ValueAssigned -= UnderlayValueAssigned;
                    oldList.BeforeValueRemoved -= UnderlayBeforeValueRemoved;
                    _atomManager.UnderlaysListToAtom.Remove(oldList);
                }

                if (!value.TryGetValueAsDreamList(out var underlayList)) {
                    underlayList = _objectTree.CreateList();
                }

                underlayList.ValueAssigned += UnderlayValueAssigned;
                underlayList.BeforeValueRemoved += UnderlayBeforeValueRemoved;
                _atomManager.UnderlaysListToAtom[underlayList] = dreamObject;
                dreamObject.SetVariableValue(varName, new DreamValue(underlayList));
                break;
                }
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

    private IconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
            IconAppearance overlay;

            if (value.TryGetValueAsString(out var iconState)) {
                overlay = new IconAppearance() {
                    IconState = iconState
                };
            } else if (_atomManager.TryCreateAppearanceFrom(value, out var overlayAppearance)) {
                overlay = overlayAppearance;
            } else {
                return new IconAppearance(); // Not a valid overlay, use a default appearance
            }

            if (overlay.Icon == null) {
                overlay.Icon = _atomManager.MustGetAppearance(atom)?.Icon;
            }

            return overlay;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];

            _atomManager.UpdateAppearance(atom, appearance => {
                IconAppearance overlay = CreateOverlayAppearance(atom, value);
                uint id = _appearanceSystem.AddAppearance(overlay);

                appearance.Overlays.Add(id);
            });
        }

        private void OverlayBeforeValueRemoved(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];
            IconAppearance overlayAppearance = CreateOverlayAppearance(atom, value);
            uint? overlayAppearanceId = _appearanceSystem.GetAppearanceId(overlayAppearance);
            if (overlayAppearanceId == null) return;

            _atomManager.UpdateAppearance(atom, appearance => {
                appearance.Overlays.Remove(overlayAppearanceId.Value);
            });
        }

        private void UnderlayValueAssigned(DreamList underList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[underList];

            _atomManager.UpdateAppearance(atom, appearance => {
                IconAppearance underlay = CreateOverlayAppearance(atom, value);
                uint id = _appearanceSystem.AddAppearance(underlay);

                appearance.Underlays.Add(id);
            });
        }

        private void UnderlayBeforeValueRemoved(DreamList underlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[underlayList];
            IconAppearance underlayAppearance = CreateOverlayAppearance(atom, value);
            uint? underlayAppearanceId = _appearanceSystem.GetAppearanceId(underlayAppearance);
            if (underlayAppearanceId == null) return;

            _atomManager.UpdateAppearance(atom, appearance => {
                appearance.Underlays.Remove(underlayAppearanceId.Value);
            });
        }
}
