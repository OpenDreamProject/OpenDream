using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Map;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace OpenDreamRuntime;

public sealed class AtomManager {
    public int AtomCount { get; private set; }

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;

    private readonly List<DreamObjectMob?> _mobs = new();
    private readonly List<DreamObjectMovable?> _movables = new();
    private readonly List<DreamObjectArea?> _areas = new();
    private readonly List<DreamObjectTurf?> _turfs = new();
    private int _nextEmptyMobSlot;
    private int _nextEmptyMovableSlot;
    private int _nextEmptyAreaSlot;
    private int _nextEmptyTurfSlot;

    private readonly Dictionary<EntityUid, DreamObjectMovable> _entityToAtom = new();
    private readonly Dictionary<DreamObjectDefinition, MutableAppearance> _definitionAppearanceCache = new();
    private readonly Dictionary<DreamObjectDefinition, AtomMouseEvents> _enabledMouseEvents = new();

    private ServerAppearanceSystem? AppearanceSystem {
        get {
            if(_appearanceSystem is null)
                _entitySystemManager.TryGetEntitySystem(out _appearanceSystem);
            return _appearanceSystem;
        }
    }

    private DMISpriteSystem? DMISpriteSystem {
        get {
            if(_dmiSpriteSystem is null)
                _entitySystemManager.TryGetEntitySystem(out _dmiSpriteSystem);
            return _dmiSpriteSystem;
        }
    }

    private ServerVerbSystem? VerbSystem {
        get {
            if(_verbSystem is null)
                _entitySystemManager.TryGetEntitySystem(out _verbSystem);
            return _verbSystem;
        }
    }

    private ServerAppearanceSystem? _appearanceSystem;
    private ServerVerbSystem? _verbSystem;
    private DMISpriteSystem? _dmiSpriteSystem;

    // ReSharper disable ForCanBeConvertedToForeach (the collections could be added to)
    public IEnumerable<DreamObjectAtom> EnumerateAtoms(TreeEntry? filterType = null) {
        // Order of world.contents:
        //  Mobs + Other Movables + Areas + Turfs

        if (filterType == _objectTree.Atom) // Filtering by /atom is the same as no filter
            filterType = null;

        if (filterType?.IsSubtypeOf(_objectTree.Mob) != false) {
            for (int i = 0; i < _mobs.Count; i++) {
                var mob = _mobs[i];

                if (mob != null && (filterType == null || mob.IsSubtypeOf(filterType)))
                    yield return mob;
            }
        }

        if (filterType?.IsSubtypeOf(_objectTree.Movable) != false) {
            for (int i = 0; i < _movables.Count; i++) {
                var movable = _movables[i];
                if (movable != null && (filterType == null || movable.IsSubtypeOf(filterType)))
                    yield return movable;
            }
        }

        if (filterType?.IsSubtypeOf(_objectTree.Area) != false) {
            for (int i = 0; i < _areas.Count; i++) {
                var area = _areas[i];
                if (area != null && (filterType == null || area.IsSubtypeOf(filterType)))
                    yield return area;
            }
        }

        if (filterType?.IsSubtypeOf(_objectTree.Turf) != false) {
            for (int i = 0; i < _turfs.Count; i++) {
                var turf = _turfs[i];
                if (turf != null && (filterType == null || turf.IsSubtypeOf(filterType)))
                    yield return turf;
            }
        }
    }
    // ReSharper restore ForCanBeConvertedToForeach

    public void AddAtom(DreamObjectAtom atom) {
        AtomCount++;

        switch (atom) {
            case DreamObjectArea area: {
                var nextSlot = _nextEmptyAreaSlot++;
                if (nextSlot >= _areas.Count) {
                    _areas.Add(area);
                    return;
                }

                _areas[nextSlot] = area;
                for (; _nextEmptyAreaSlot < _areas.Count; _nextEmptyAreaSlot++) {
                    if (_areas[_nextEmptyAreaSlot] == null)
                        break;
                }

                break;
            }
            case DreamObjectTurf turf: {
                var nextSlot = _nextEmptyTurfSlot++;
                if (nextSlot >= _turfs.Count) {
                    _turfs.Add(turf);
                    return;
                }

                _turfs[nextSlot] = turf;
                for (; _nextEmptyTurfSlot < _turfs.Count; _nextEmptyTurfSlot++) {
                    if (_turfs[_nextEmptyTurfSlot] == null)
                        break;
                }

                break;
            }
            case DreamObjectMob mob: {
                var nextSlot = _nextEmptyMobSlot++;
                if (nextSlot >= _mobs.Count) {
                    _mobs.Add(mob);
                    return;
                }

                _mobs[nextSlot] = mob;
                for (; _nextEmptyMobSlot < _mobs.Count; _nextEmptyMobSlot++) {
                    if (_mobs[_nextEmptyMobSlot] == null)
                        break;
                }

                break;
            }
            case DreamObjectMovable movable: {
                var nextSlot = _nextEmptyMovableSlot++;
                if (nextSlot >= _movables.Count) {
                    _movables.Add(movable);
                    return;
                }

                _movables[nextSlot] = movable;
                for (; _nextEmptyMovableSlot < _movables.Count; _nextEmptyMovableSlot++) {
                    if (_movables[_nextEmptyMovableSlot] == null)
                        break;
                }

                break;
            }
        }
    }

    public void RemoveAtom(DreamObjectAtom atom) {
        AtomCount--;

        int index;
        switch (atom) {
            case DreamObjectArea area:
                index = _areas.IndexOf(area);
                if (index == -1)
                    return;

                _nextEmptyAreaSlot = Math.Min(_nextEmptyAreaSlot, index);
                _areas[index] = null;
                break;
            case DreamObjectTurf turf:
                index = _turfs.IndexOf(turf);
                if (index == -1)
                    return;

                _nextEmptyTurfSlot = Math.Min(_nextEmptyTurfSlot, index);
                _turfs[index] = null;
                break;
            case DreamObjectMob mob:
                index = _mobs.IndexOf(mob);
                if (index == -1)
                    return;

                _nextEmptyMobSlot = Math.Min(_nextEmptyMobSlot, index);
                _mobs[index] = null;
                break;
            case DreamObjectMovable movable:
                index = _movables.IndexOf(movable);
                if (index == -1)
                    return;

                _nextEmptyMovableSlot = Math.Min(_nextEmptyMovableSlot, index);
                _movables[index] = null;
                break;
        }
    }

    public EntityUid CreateMovableEntity(DreamObjectMovable movable) {
        var entity = _entityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace));

        DMISpriteComponent sprite = _entityManager.AddComponent<DMISpriteComponent>(entity);
        DMISpriteSystem?.SetSpriteAppearance(new(entity, sprite), GetAppearanceFromDefinition(movable.ObjectDefinition));

        _entityToAtom.Add(entity, movable);
        return entity;
    }

    public bool TryGetMovableFromEntity(EntityUid entity, [NotNullWhen(true)] out DreamObjectMovable? movable) {
        return _entityToAtom.TryGetValue(entity, out movable);
    }

    public void DeleteMovableEntity(DreamObjectMovable movable) {
        _entityToAtom.Remove(movable.Entity);
        _entityManager.DeleteEntity(movable.Entity);
    }

    public bool IsValidAppearanceVar(string name) {
        switch (name) {
            case "name":
            case "desc":
            case "icon":
            case "icon_state":
            case "dir":
            case "pixel_x":
            case "pixel_y":
            case "pixel_w":
            case "pixel_z":
            case "color":
            case "layer":
            case "invisibility":
            case "opacity":
            case "mouse_opacity":
            case "plane":
            case "blend_mode":
            case "appearance_flags":
            case "alpha":
            case "glide_size":
            case "render_source":
            case "render_target":
            case "transform":
            case "appearance":
            case "verbs":
            case "overlays":
            case "underlays":
            case "maptext":
            case "maptext_width":
            case "maptext_height":
            case "maptext_x":
            case "maptext_y":
                return true;

            // Get/SetAppearanceVar doesn't handle filters right now
            case "filters":
            default:
                return false;
        }
    }

    public void SetAppearanceVar(MutableAppearance appearance, string varName, DreamValue value) {
        switch (varName) {
            case "name":
                value.TryGetValueAsString(out var name);
                appearance.Name = name ?? string.Empty;
                break;
            case "desc":
                value.TryGetValueAsString(out var desc);
                appearance.Desc = desc;
                break;
            case "icon":
                if (_resourceManager.TryLoadIcon(value, out var icon)) {
                    appearance.Icon = icon.Id;
                } else {
                    appearance.Icon = null;
                }

                break;
            case "icon_state":
                value.TryGetValueAsString(out appearance.IconState);
                break;
            case "dir":
                value.TryGetValueAsInteger(out var dir);

                if (dir <= 0) // Ignore any sets <= 0 or non-number
                    break;

                if (dir > 0xFF) // Clamp to 1 byte
                    dir = 0xFF;

                appearance.Direction = (AtomDirection)dir;
                break;
            case "pixel_x":
                value.TryGetValueAsInteger(out appearance.PixelOffset.X);
                break;
            case "pixel_y":
                value.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                break;
            case "pixel_w":
                value.TryGetValueAsInteger(out appearance.PixelOffset2.X);
                break;
            case "pixel_z":
                value.TryGetValueAsInteger(out appearance.PixelOffset2.Y);
                break;
            case "color":
                if(value.TryGetValueAsDreamList(out var list)) {
                    if(DreamProcNativeHelpers.TryParseColorMatrix(list, out var matrix)) {
                        appearance.SetColor(in matrix);
                        break;
                    }

                    throw new ArgumentException($"Cannot set appearance's color to {value}");
                }

                value.TryGetValueAsString(out var colorString);
                colorString ??= "white";
                appearance.SetColor(colorString);
                break;
            case "layer":
                value.TryGetValueAsFloat(out appearance.Layer);
                break;
            case "invisibility":
                value.TryGetValueAsInteger(out int vis);
                vis = Math.Clamp(vis, -127, 127); // DM ref says [0, 101]. BYOND compiler says [-127, 127]
                appearance.Invisibility = (sbyte)vis;
                break;
            case "opacity":
                value.TryGetValueAsInteger(out var opacity);
                appearance.Opacity = (opacity != 0);
                break;
            case "mouse_opacity":
                //TODO figure out the weird inconsistencies with this being internally clamped
                value.TryGetValueAsInteger(out var mouseOpacity);
                appearance.MouseOpacity = (MouseOpacity)mouseOpacity;
                break;
            case "plane":
                value.TryGetValueAsInteger(out appearance.Plane);
                break;
            case "blend_mode":
                value.TryGetValueAsInteger(out int blendMode);
                appearance.BlendMode = Enum.IsDefined((BlendMode)blendMode) ? (BlendMode)blendMode : BlendMode.Default;
                break;
            case "appearance_flags":
                value.TryGetValueAsInteger(out int flagsVar);
                appearance.AppearanceFlags = (AppearanceFlags) flagsVar;
                break;
            case "alpha":
                value.TryGetValueAsFloat(out float floatAlpha);
                appearance.Alpha = (byte) Math.Clamp(floatAlpha, 0, 255);
                break;
            case "glide_size":
                value.TryGetValueAsFloat(out float glideSize);
                appearance.GlideSize = glideSize;
                break;
            case "render_source":
                value.TryGetValueAsString(out appearance.RenderSource);
                break;
            case "render_target":
                value.TryGetValueAsString(out appearance.RenderTarget);
                break;
            case "transform":
                float[] transformArray = value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var transform)
                    ? DreamObjectMatrix.MatrixToTransformFloatArray(transform)
                    : DreamObjectMatrix.IdentityMatrixArray;

                appearance.Transform = transformArray;
                break;
            case "verbs":
                appearance.Verbs.Clear();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue verbValue in valueList.GetValues()) {
                        if (!verbValue.TryGetValueAsProc(out var verb))
                            continue;


                        if (!verb.VerbId.HasValue)
                            VerbSystem?.RegisterVerb(verb);
                        if (appearance.Verbs.Contains(verb.VerbId!.Value))
                            continue;

                        appearance.Verbs.Add(verb.VerbId.Value);
                    }
                } else if (value.TryGetValueAsProc(out var verb)) {
                    if (!verb.VerbId.HasValue)
                        VerbSystem?.RegisterVerb(verb);

                    appearance.Verbs.Add(verb.VerbId!.Value);
                }

                break;
            case "maptext":
                if(value == DreamValue.Null)
                    appearance.Maptext = null;
                else
                    value.TryGetValueAsString(out appearance.Maptext);
                break;
            case "maptext_height":
                value.TryGetValueAsInteger(out appearance.MaptextSize.Y);
                break;
            case "maptext_width":
                value.TryGetValueAsInteger(out appearance.MaptextSize.X);
                break;
            case "maptext_x":
                value.TryGetValueAsInteger(out appearance.MaptextOffset.X);
                break;
            case "maptext_y":
                value.TryGetValueAsInteger(out appearance.MaptextOffset.Y);
                break;
            case "appearance":
                throw new Exception("Cannot assign the appearance var on an appearance");

            // These should be handled by the DreamObject if being accessed through that
            case "overlays":
            case "underlays":
                throw new Exception($"Cannot assign the {varName} var on an appearance");

            // TODO: filters
            //       It's handled separately by whatever is calling SetAppearanceVar currently
            default:
                throw new ArgumentException($"Invalid appearance var {varName}");
        }
    }

    //TODO THIS IS A SUPER NASTY HACK
    public DreamValue GetAppearanceVar(MutableAppearance appearance, string varName) {
        return GetAppearanceVar(new ImmutableAppearance(appearance, null), varName);
    }

    public DreamValue GetAppearanceVar(ImmutableAppearance appearance, string varName) {
        switch (varName) {
            case "name":
                return new(appearance.Name);
            case "desc":
                if (appearance.Desc == null)
                    return DreamValue.Null;
                return new(appearance.Desc);
            case "icon":
                if (appearance.Icon == null)
                    return DreamValue.Null;
                if (!_resourceManager.TryLoadResource(appearance.Icon.Value, out var iconResource))
                    return DreamValue.Null;

                return new(iconResource);
            case "icon_state":
                if (appearance.IconState == null)
                    return DreamValue.Null;

                return new(appearance.IconState);
            case "dir":
                return new((int) appearance.Direction);
            case "pixel_x":
                return new(appearance.PixelOffset.X);
            case "pixel_y":
                return new(appearance.PixelOffset.Y);
            case "pixel_w":
                return new(appearance.PixelOffset2.X);
            case "pixel_z":
                return new(appearance.PixelOffset2.Y);
            case "color":
                if(!appearance.ColorMatrix.Equals(ColorMatrix.Identity)) {
                    var matrixList = _objectTree.CreateList(20);
                    foreach (float entry in appearance.ColorMatrix.GetValues())
                        matrixList.AddValue(new DreamValue(entry));
                    return new DreamValue(matrixList);
                }

                if (appearance.Color == Color.White) {
                    return DreamValue.Null;
                }

                return new DreamValue(appearance.Color.ToHexNoAlpha().ToLower()); // BYOND quirk, does not return the alpha channel for some reason.
            case "layer":
                return new(appearance.Layer);
            case "invisibility":
                return new(appearance.Invisibility);
            case "opacity":
                return appearance.Opacity ? DreamValue.True : DreamValue.False;
            case "mouse_opacity":
                return new((int)appearance.MouseOpacity);
            case "plane":
                return new(appearance.Plane);
            case "blend_mode":
                return new((int) appearance.BlendMode);
            case "appearance_flags":
                return new((int) appearance.AppearanceFlags);
            case "alpha":
                return new(appearance.Alpha);
            case "glide_size":
                return new(appearance.GlideSize);
            case "render_source":
                return (appearance.RenderSource != null)
                    ? new DreamValue(appearance.RenderSource)
                    : DreamValue.Null;
            case "render_target":
                return (appearance.RenderTarget != null)
                    ? new DreamValue(appearance.RenderTarget)
                    : DreamValue.Null;
            case "transform":
                var transform = appearance.Transform;
                var matrix = DreamObjectMatrix.MakeMatrix(_objectTree,
                    transform[0], transform[2], transform[4],
                    transform[1], transform[3], transform[5]);

                return new(matrix);
            case "maptext":
                return (appearance.Maptext != null)
                    ? new DreamValue(appearance.Maptext)
                    : DreamValue.Null;
            case "maptext_height":
                return new(appearance.MaptextSize.Y);
            case "maptext_width":
                return new(appearance.MaptextSize.X);
            case "maptext_x":
                return new(appearance.MaptextOffset.X);
            case "maptext_y":
                return new(appearance.MaptextOffset.Y);
            case "appearance":
                MutableAppearance appearanceCopy = appearance.ToMutable(); // Return a copy
                return new(appearanceCopy);

            // These should be handled by an atom if referenced through one
            case "overlays":
            case "underlays":
                // In BYOND this just creates a new normal list
                var lays = varName == "overlays" ? appearance.Overlays : appearance.Underlays;
                var list = _objectTree.CreateList(lays.Length);

                if (_appearanceSystem != null) {
                    foreach (var lay in lays) {
                        list.AddValue(new(lay.ToMutable()));
                    }
                }

                return new(list);

            // TODO: filters
            //       It's handled separately by whatever is calling GetAppearanceVar currently
            default:
                throw new ArgumentException($"Invalid appearance var {varName}");
        }
    }

    /// <summary>
    /// Gets an atom's appearance. Will throw if the appearance system is not available.
    /// </summary>
    /// <param name="atom">The atom to find the appearance of.</param>
    public ImmutableAppearance MustGetAppearance(DreamObject atom) {
        return atom switch {
            DreamObjectArea area => area.Appearance,
            DreamObjectTurf turf => turf.Appearance,
            DreamObjectMovable movable => movable.SpriteComponent.Appearance!,
            DreamObjectImage image => image.IsMutableAppearance ? AppearanceSystem!.AddAppearance(image.MutableAppearance!, registerAppearance: false) : image.SpriteComponent!.Appearance!,
            _ => throw new Exception($"Cannot get appearance of {atom}")
        };
    }

    /// <summary>
    /// Optionally looks up for an appearance. Does not try to create a new one when one is not found for this atom.
    /// </summary>
    public bool TryGetAppearance(DreamObject atom, [NotNullWhen(true)] out ImmutableAppearance? appearance) {
        appearance = atom switch {
            DreamObjectArea area => area.Appearance,
            DreamObjectTurf turf => turf.Appearance,
            DreamObjectMovable { SpriteComponent.Appearance: { } movableAppearance } => movableAppearance,
            DreamObjectImage image => image.IsMutableAppearance
                ? AppearanceSystem!.AddAppearance(image.MutableAppearance!, registerAppearance: false)
                : image.SpriteComponent?.Appearance,
            _ => null
        };

        return appearance is not null;
    }

    public void UpdateAppearance(DreamObject atom, Action<MutableAppearance> update) {
        ImmutableAppearance immutableAppearance = MustGetAppearance(atom);
        using var appearance = immutableAppearance.ToMutable(); // Clone the appearance
        update(appearance);
        SetAtomAppearance(atom, appearance);
    }

    public void SetAtomAppearance(DreamObject atom, MutableAppearance appearance) {
        if (atom is DreamObjectImage image) {
            if(image.IsMutableAppearance)
                image.MutableAppearance = MutableAppearance.GetCopy(appearance); //this needs to be a copy
            else
                DMISpriteSystem?.SetSpriteAppearance(new(image.Entity, image.SpriteComponent!), appearance);
            return;
        }

        appearance.EnabledMouseEvents = GetEnabledMouseEvents(atom);

        if (atom is DreamObjectTurf turf) {
            _dreamMapManager.SetTurfAppearance(turf, appearance);
        } else if (atom is DreamObjectMovable movable) {
            DMISpriteSystem?.SetSpriteAppearance(new(movable.Entity, movable.SpriteComponent), appearance);
        } else if (atom is DreamObjectArea area) {
            _dreamMapManager.SetAreaAppearance(area, appearance);
        }
    }

    public void SetMovableScreenLoc(DreamObjectMovable movable, ScreenLocation screenLocation) {
        DMISpriteSystem?.SetSpriteScreenLocation(new(movable.Entity, movable.SpriteComponent), screenLocation);
    }

    public void SetSpriteAppearance(Entity<DMISpriteComponent> ent, MutableAppearance appearance) {
        DMISpriteSystem?.SetSpriteAppearance(ent, appearance);
    }

    public void AnimateAppearance(DreamObject atom, TimeSpan duration, AnimationEasing easing, int loop, AnimationFlags flags, int delay, bool chainAnim, Action<MutableAppearance> animate) {
        MutableAppearance appearance;
        EntityUid targetEntity;
        DMISpriteComponent? targetComponent = null;
        NetEntity ent = NetEntity.Invalid;
        uint? turfId = null;

        if (atom is DreamObjectMovable movable) {
            targetEntity = movable.Entity;
            targetComponent = movable.SpriteComponent;
            appearance = MustGetAppearance(atom).ToMutable();
        } else if (atom is DreamObjectImage { IsMutableAppearance: false } image) {
            targetEntity = image.Entity;
            targetComponent = image.SpriteComponent;
            appearance = MustGetAppearance(atom).ToMutable();
        } else if (atom is DreamObjectTurf turf) {
            targetEntity = EntityUid.Invalid;
            appearance = turf.Appearance.ToMutable();
        } else if (atom is DreamObjectArea area) {
            return;
            //TODO: animate area appearance
            //area appearance should be an overlay on turfs, so could maybe get away with animating that?
        } else if (atom is DreamObjectClient client) {
            return;
            //TODO: animate client appearance
        } else if (atom is DreamObjectFilter filter) {
            return;
            //TODO: animate filters
        } else
            throw new ArgumentException($"Cannot animate appearance of {atom}");

        animate(appearance);

        if(targetComponent is not null) {
            ent = _entityManager.GetNetEntity(targetEntity);
            // Don't send the updated appearance to clients, they will animate it
            DMISpriteSystem?.SetSpriteAppearance(new(targetEntity, targetComponent), appearance, dirty: false);
        } else if (atom is DreamObjectTurf turf) {
            //TODO: turf appearances are just set to the end appearance, they do not get properly animated
            _dreamMapManager.SetTurfAppearance(turf, appearance);
            turfId = turf.Appearance.MustGetId();
        } else if (atom is DreamObjectArea area) {
            //fuck knows, this will trigger a bunch of turf updates to? idek
        }

        AppearanceSystem?.Animate(ent, appearance, duration, easing, loop, flags, delay, chainAnim, turfId);
    }

    public bool TryCreateAppearanceFrom(DreamValue value, [NotNullWhen(true)] out MutableAppearance? appearance) {
        if (value.TryGetValueAsAppearance(out var copyFromAppearance)) {
            appearance = MutableAppearance.GetCopy(copyFromAppearance);
            return true;
        }

        if (value.TryGetValueAsDreamObject<DreamObjectImage>(out var copyFromImage)) {
            appearance = MustGetAppearance(copyFromImage).ToMutable();
            return true;
        }

        if (value.TryGetValueAsType(out var copyFromType)) {
            appearance = MutableAppearance.GetCopy(GetAppearanceFromDefinition(copyFromType.ObjectDefinition));
            return true;
        }

        if (value.TryGetValueAsDreamObject<DreamObjectAtom>(out var copyFromAtom)) {
            appearance = MustGetAppearance(copyFromAtom).ToMutable();
            return true;
        }

        if (_resourceManager.TryLoadIcon(value, out var iconResource)) {
            appearance = MutableAppearance.Get();
            appearance.Icon = iconResource.Id;

            return true;
        }

        appearance = null;
        return false;
    }

    public MutableAppearance GetAppearanceFromDefinition(DreamObjectDefinition def) {
        if (_definitionAppearanceCache.TryGetValue(def, out var appearance))
            return appearance;

        def.TryGetVariable("name", out var nameVar);
        def.TryGetVariable("desc", out var descVar);
        def.TryGetVariable("icon", out var iconVar);
        def.TryGetVariable("icon_state", out var stateVar);
        def.TryGetVariable("color", out var colorVar);
        def.TryGetVariable("alpha", out var alphaVar);
        def.TryGetVariable("glide_size", out var glideSizeVar);
        def.TryGetVariable("dir", out var dirVar);
        def.TryGetVariable("invisibility", out var invisibilityVar);
        def.TryGetVariable("opacity", out var opacityVar);
        def.TryGetVariable("mouse_opacity", out var mouseVar);
        def.TryGetVariable("pixel_x", out var xVar);
        def.TryGetVariable("pixel_y", out var yVar);
        def.TryGetVariable("layer", out var layerVar);
        def.TryGetVariable("plane", out var planeVar);
        def.TryGetVariable("render_source", out var renderSourceVar);
        def.TryGetVariable("render_target", out var renderTargetVar);
        def.TryGetVariable("blend_mode", out var blendModeVar);
        def.TryGetVariable("appearance_flags", out var appearanceFlagsVar);
        def.TryGetVariable("maptext", out var maptextVar);
        def.TryGetVariable("maptext_width", out var maptextWidthVar);
        def.TryGetVariable("maptext_height", out var maptextHeightVar);
        def.TryGetVariable("maptext_x", out var maptextXVar);
        def.TryGetVariable("maptext_y", out var maptextYVar);

        appearance = MutableAppearance.Get();
        SetAppearanceVar(appearance, "name", nameVar);
        SetAppearanceVar(appearance, "desc", descVar);
        SetAppearanceVar(appearance, "icon", iconVar);
        SetAppearanceVar(appearance, "icon_state", stateVar);
        SetAppearanceVar(appearance, "color", colorVar);
        SetAppearanceVar(appearance, "alpha", alphaVar);
        SetAppearanceVar(appearance, "glide_size", glideSizeVar);
        SetAppearanceVar(appearance, "dir", dirVar);
        SetAppearanceVar(appearance, "invisibility", invisibilityVar);
        SetAppearanceVar(appearance, "opacity", opacityVar);
        SetAppearanceVar(appearance, "mouse_opacity", mouseVar);
        SetAppearanceVar(appearance, "pixel_x", xVar);
        SetAppearanceVar(appearance, "pixel_y", yVar);
        SetAppearanceVar(appearance, "layer", layerVar);
        SetAppearanceVar(appearance, "plane", planeVar);
        SetAppearanceVar(appearance, "render_source", renderSourceVar);
        SetAppearanceVar(appearance, "render_target", renderTargetVar);
        SetAppearanceVar(appearance, "blend_mode", blendModeVar);
        SetAppearanceVar(appearance, "appearance_flags", appearanceFlagsVar);
        SetAppearanceVar(appearance, "maptext", maptextVar);
        SetAppearanceVar(appearance, "maptext_width", maptextWidthVar);
        SetAppearanceVar(appearance, "maptext_height", maptextHeightVar);
        SetAppearanceVar(appearance, "maptext_x", maptextXVar);
        SetAppearanceVar(appearance, "maptext_y", maptextYVar);

        if (def.TryGetVariable("transform", out var transformVar) && transformVar.TryGetValueAsDreamObject<DreamObjectMatrix>(out var transformMatrix)) {
            appearance.Transform = DreamObjectMatrix.MatrixToTransformFloatArray(transformMatrix);
        }

        if (def.Verbs != null) {
            foreach (var verb in def.Verbs) {
                var verbProc = _objectTree.Procs[verb];

                appearance.Verbs.Add(verbProc.VerbId!.Value);
            }
        }

        _definitionAppearanceCache.Add(def, appearance);
        return appearance;
    }

    public AtomMouseEvents GetEnabledMouseEvents(DreamObject atom) {
        var def = atom.ObjectDefinition;

        if (!_enabledMouseEvents.TryGetValue(def, out var mouseEvents)) {
            mouseEvents = 0;

            if (def.TryGetProc("MouseDown", out var mouseDownProc) && mouseDownProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Down;
            if (def.TryGetProc("MouseUp", out var mouseUpProc) && mouseUpProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Up;
            if (def.TryGetProc("MouseDrag", out var mouseDragProc) && mouseDragProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Drag;
            if (def.TryGetProc("MouseEntered", out var mouseEnterProc) && mouseEnterProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Enter;
            if (def.TryGetProc("MouseExited", out var mouseExitProc) && mouseExitProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Exit;
            if (def.TryGetProc("MouseMove", out var mouseMoveProc) && mouseMoveProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Move;
            if (def.TryGetProc("MouseWheel", out var mouseWheelProc) && mouseWheelProc is DMProc {Bytecode.Length: > 0})
                mouseEvents |= AtomMouseEvents.Wheel;

            _enabledMouseEvents.Add(atom.ObjectDefinition, mouseEvents);
        }

        return mouseEvents;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int X, int Y, int Z) GetAtomPosition(DreamObjectAtom atom) {
        return atom switch {
            DreamObjectMovable { Position: var pos, Z: var z } => (pos.X, pos.Y, z),
            DreamObjectTurf turf => (turf.X, turf.Y, turf.Z),
            DreamObjectArea area => (area.X, area.Y, area.Z),
            _ => ThrowCantGetPosition(atom)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (int X, int Y, int) ThrowCantGetPosition(DreamObjectAtom atom) {
        throw new Exception($"Cannot get the position of {atom}");
    }
}
