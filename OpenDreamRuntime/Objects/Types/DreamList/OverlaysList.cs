using System.Linq;
using System.Runtime.CompilerServices;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
namespace OpenDreamRuntime.Objects.Types;

// atom.overlays or atom.underlays list
// Operates on an object's appearance
public sealed class DreamOverlaysList(DreamObjectDefinition listDef, DreamObject owner, ServerAppearanceSystem? appearanceSystem, bool isUnderlays) : DreamListAbstract(listDef) {
    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        var appearance = AtomManager.MustGetAppearance(owner);
        if (appearanceSystem == null)
            yield break;

        foreach (var overlay in GetOverlaysArray(appearance)) {
            yield return new(overlay.ToMutable());
        }
    }

    public override void Cut(int start = 1, int end = 0) {
        AtomManager.UpdateAppearance(owner, appearance => {
            var overlaysList = GetOverlaysList(appearance);
            int count = overlaysList.Count + 1;
            if (end == 0 || end > count) end = count;
            overlaysList.RemoveRange(start - 1, end - start);
        });
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var overlayIndex) || overlayIndex < 1)
            throw new DMException($"Invalid index into {(isUnderlays ? "underlays" : "overlays")} list: {key}");

        ImmutableAppearance appearance = AtomManager.MustGetAppearance(owner);
        var overlaysList = GetOverlaysArray(appearance);
        if (overlayIndex > overlaysList.Length)
            throw new DMException($"Atom only has {overlaysList.Length} {(isUnderlays ? "underlay" : "overlay")}(s), cannot index {overlayIndex}");

        if (appearanceSystem == null)
            return DreamValue.Null;

        var overlayAppearance = overlaysList[overlayIndex - 1].ToMutable();
        return new DreamValue(overlayAppearance);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException($"Cannot write to an index of an {(isUnderlays ? "underlays" : "overlays")} list");
    }

    public override void AddKey(DreamValue value) {
        if (appearanceSystem == null)
            return;

        var overlayAppearance = CreateOverlayAppearance(AtomManager, value, AtomManager.MustGetAppearance(owner).Icon);
        var immutableOverlay = appearanceSystem.AddAppearance(overlayAppearance ?? MutableAppearance.Default);
        overlayAppearance?.Dispose();

        //after UpdateAppearance is done, the atom is set with a new immutable appearance containing a hard ref to the overlay
        //only /mutable_appearance handles it differently, and that's done in DreamObjectImage
        AtomManager.UpdateAppearance(owner, appearance => {
            GetOverlaysList(appearance).Add(immutableOverlay);
        });
    }

    public override void RemoveKey(DreamValue value) {
        if (appearanceSystem == null)
            return;

        MutableAppearance? overlayAppearance = CreateOverlayAppearance(AtomManager, value, AtomManager.MustGetAppearance(owner).Icon);
        if (overlayAppearance == null)
            return;

        AtomManager.UpdateAppearance(owner, appearance => {
            GetOverlaysList(appearance).Remove(appearanceSystem.AddAppearance(overlayAppearance, registerAppearance:false));
            overlayAppearance.Dispose();
        });
    }

    public override int GetLength() {
        return GetOverlaysArray(AtomManager.MustGetAppearance(owner)).Length;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<ImmutableAppearance> GetOverlaysList(MutableAppearance appearance) =>
        isUnderlays ? appearance.Underlays : appearance.Overlays;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ImmutableAppearance[] GetOverlaysArray(ImmutableAppearance appearance) =>
        isUnderlays ? appearance.Underlays : appearance.Overlays;

    public static MutableAppearance? CreateOverlayAppearance(AtomManager atomManager, DreamValue value, int? defaultIcon) {
        MutableAppearance overlay;

        if (value.TryGetValueAsString(out var iconState)) {
            overlay = MutableAppearance.Get();
            overlay.IconState = iconState;
            overlay.Icon ??= defaultIcon;
        } else if (atomManager.TryCreateAppearanceFrom(value, out var overlayAppearance)) {
            overlay = overlayAppearance;
        } else {
            return null; // Not a valid overlay
        }

        return overlay;
    }
}
