using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Serialization;
using System.Linq;

namespace OpenDreamRuntime.Rendering;

/*
 * Woe, weary traveler, modifying this class is not for the faint of heart.
 * If you modify MutableIconAppearance, be sure to update the following places:
 * - All of the methods on ImmutableIconAppearance itself
 * - MutableIconAppearance
 * - MutableIconAppearance methods in AtomManager
 * - There may be others
 */

// TODO: Wow this is huge! Probably look into splitting this by most used/least used to reduce the size of these

public sealed class ImmutableIconAppearance : IEquatable<ImmutableIconAppearance>, IBufferableAppearance {
    private bool registered = false;
    [ViewVariables] public readonly string Name;
    [ViewVariables] public readonly int? Icon;
    [ViewVariables] public readonly string? IconState;
    [ViewVariables] public readonly AtomDirection Direction;
    [ViewVariables] public readonly bool InheritsDirection; // Inherits direction when used as an overlay
    [ViewVariables] public readonly Vector2i PixelOffset;  // pixel_x and pixel_y
    [ViewVariables] public readonly Vector2i PixelOffset2; // pixel_w and pixel_z
    [ViewVariables] public readonly Color Color;
    [ViewVariables] public readonly byte Alpha;
    [ViewVariables] public readonly float GlideSize;
    [ViewVariables] public readonly float Layer;
    [ViewVariables] public int Plane;
    [ViewVariables] public readonly BlendMode BlendMode;
    [ViewVariables] public readonly AppearanceFlags AppearanceFlags;
    [ViewVariables] public readonly sbyte Invisibility;
    [ViewVariables] public readonly bool Opacity;
    [ViewVariables] public readonly bool Override;
    [ViewVariables] public readonly string? RenderSource;
    [ViewVariables] public readonly string? RenderTarget;
    [ViewVariables] public readonly MouseOpacity MouseOpacity;
    [ViewVariables] public readonly ImmutableIconAppearance[] Overlays;
    [ViewVariables] public readonly ImmutableIconAppearance[] Underlays;
    [ViewVariables] public readonly NetEntity[] VisContents;
    [ViewVariables] public readonly DreamFilter[] Filters;
    [ViewVariables] public readonly int[] Verbs;

    /// <summary>
    /// An appearance can gain a color matrix filter by two possible forces: <br/>
    /// 1. the /atom.color var is modified. <br/>
    /// 2. the /atom.filters var gets a new filter of type "color". <br/>
    /// DM crashes in some circumstances of this but we, as an extension :^), should try not to. <br/>
    /// So, this exists as a way for the appearance to remember whether it's coloured by .color, specifically.
    /// </summary>
    /// <remarks>
    /// The reason we don't just take the slow path and always use this filter is not just for optimization,<br/>
    /// it's also for parity! See <see cref="TryRepresentMatrixAsRgbaColor"/> for more.
    /// </remarks>
    [ViewVariables] public readonly ColorMatrix ColorMatrix = ColorMatrix.Identity;

    /// <summary> The Transform property of this appearance, in [a,d,b,e,c,f] order</summary>
    [ViewVariables] public readonly float[] Transform = [
        1, 0,   // a d
        0, 1,   // b e
        0, 0    // c f
    ];

    // PixelOffset2 behaves the same as PixelOffset in top-down mode, so this is used
    public Vector2i TotalPixelOffset => PixelOffset + PixelOffset2;

    private int? _storedHashCode;
    private readonly ServerAppearanceSystem appearanceSystem;

    public void MarkRegistered(){
        registered = true;
    }

    public ImmutableIconAppearance(MutableIconAppearance appearance, ServerAppearanceSystem serverAppearanceSystem) {
        appearanceSystem = serverAppearanceSystem;

        Name = appearance.Name;
        Icon = appearance.Icon;
        IconState = appearance.IconState;
        Direction = appearance.Direction;
        InheritsDirection = appearance.InheritsDirection;
        PixelOffset = appearance.PixelOffset;
        PixelOffset2 = appearance.PixelOffset2;
        Color = appearance.Color;
        Alpha = appearance.Alpha;
        GlideSize = appearance.GlideSize;
        ColorMatrix = appearance.ColorMatrix;
        Layer = appearance.Layer;
        Plane = appearance.Plane;
        RenderSource = appearance.RenderSource;
        RenderTarget = appearance.RenderTarget;
        BlendMode = appearance.BlendMode;
        AppearanceFlags = appearance.AppearanceFlags;
        Invisibility = appearance.Invisibility;
        Opacity = appearance.Opacity;
        MouseOpacity = appearance.MouseOpacity;

        int i = 0;
        Overlays = new ImmutableIconAppearance[appearance.Overlays.Count];
        foreach(int overlayId in appearance.Overlays)
            Overlays[i++] = serverAppearanceSystem.MustGetAppearanceById(overlayId);

        i = 0;
        Underlays = new ImmutableIconAppearance[appearance.Underlays.Count];
        foreach(int underlayId in appearance.Underlays)
            Underlays[i++] = serverAppearanceSystem.MustGetAppearanceById(underlayId);

        VisContents = appearance.VisContents.ToArray();
        Filters = appearance.Filters.ToArray();
        Verbs = appearance.Verbs.ToArray();
        Override = appearance.Override;

        for (i = 0; i < 6; i++) {
            Transform[i] = appearance.Transform[i];
        }
    }

    public override bool Equals(object? obj) => obj is ImmutableIconAppearance immutable && Equals(immutable);

    public bool Equals(ImmutableIconAppearance? immutableIconAppearance) {
        if (immutableIconAppearance == null) return false;

        if (immutableIconAppearance.Name != Name) return false;
        if (immutableIconAppearance.Icon != Icon) return false;
        if (immutableIconAppearance.IconState != IconState) return false;
        if (immutableIconAppearance.Direction != Direction) return false;
        if (immutableIconAppearance.InheritsDirection != InheritsDirection) return false;
        if (immutableIconAppearance.PixelOffset != PixelOffset) return false;
        if (immutableIconAppearance.PixelOffset2 != PixelOffset2) return false;
        if (immutableIconAppearance.Color != Color) return false;
        if (immutableIconAppearance.Alpha != Alpha) return false;
        if (immutableIconAppearance.GlideSize != GlideSize) return false;
        if (!immutableIconAppearance.ColorMatrix.Equals(ColorMatrix)) return false;
        if (immutableIconAppearance.Layer != Layer) return false;
        if (immutableIconAppearance.Plane != Plane) return false;
        if (immutableIconAppearance.RenderSource != RenderSource) return false;
        if (immutableIconAppearance.RenderTarget != RenderTarget) return false;
        if (immutableIconAppearance.BlendMode != BlendMode) return false;
        if (immutableIconAppearance.AppearanceFlags != AppearanceFlags) return false;
        if (immutableIconAppearance.Invisibility != Invisibility) return false;
        if (immutableIconAppearance.Opacity != Opacity) return false;
        if (immutableIconAppearance.MouseOpacity != MouseOpacity) return false;
        if (immutableIconAppearance.Overlays.Length != Overlays.Length) return false;
        if (immutableIconAppearance.Underlays.Length != Underlays.Length) return false;
        if (immutableIconAppearance.VisContents.Length != VisContents.Length) return false;
        if (immutableIconAppearance.Filters.Length != Filters.Length) return false;
        if (immutableIconAppearance.Verbs.Length != Verbs.Length) return false;
        if (immutableIconAppearance.Override != Override) return false;

        for (int i = 0; i < Filters.Length; i++) {
            if (immutableIconAppearance.Filters[i] != Filters[i]) return false;
        }

        for (int i = 0; i < Overlays.Length; i++) {
            if (!immutableIconAppearance.Overlays[i].Equals(Overlays[i])) return false;
        }

        for (int i = 0; i < Underlays.Length; i++) {
            if (!immutableIconAppearance.Underlays[i].Equals(Underlays[i])) return false;
        }

        for (int i = 0; i < VisContents.Length; i++) {
            if (immutableIconAppearance.VisContents[i] != VisContents[i]) return false;
        }

        for (int i = 0; i < Verbs.Length; i++) {
            if (immutableIconAppearance.Verbs[i] != Verbs[i]) return false;
        }

        for (int i = 0; i < 6; i++) {
            if (!immutableIconAppearance.Transform[i].Equals(Transform[i])) return false;
        }

        return true;
    }

    public override int GetHashCode() {
        if(_storedHashCode is not null) //because everything is readonly, this only needs to be done once
            return (int)_storedHashCode;

        HashCode hashCode = new HashCode();

        hashCode.Add(Name);
        hashCode.Add(Icon);
        hashCode.Add(IconState);
        hashCode.Add(Direction);
        hashCode.Add(InheritsDirection);
        hashCode.Add(PixelOffset);
        hashCode.Add(PixelOffset2);
        hashCode.Add(Color);
        hashCode.Add(ColorMatrix);
        hashCode.Add(Layer);
        hashCode.Add(Invisibility);
        hashCode.Add(Opacity);
        hashCode.Add(MouseOpacity);
        hashCode.Add(Alpha);
        hashCode.Add(GlideSize);
        hashCode.Add(Plane);
        hashCode.Add(RenderSource);
        hashCode.Add(RenderTarget);
        hashCode.Add(BlendMode);
        hashCode.Add(AppearanceFlags);

        foreach (ImmutableIconAppearance overlay in Overlays) {
            hashCode.Add(overlay);
        }

        foreach (ImmutableIconAppearance underlay in Underlays) {
            hashCode.Add(underlay);
        }

        foreach (int visContent in VisContents) {
            hashCode.Add(visContent);
        }

        foreach (DreamFilter filter in Filters) {
            hashCode.Add(filter);
        }

        foreach (int verb in Verbs) {
            hashCode.Add(verb);
        }

        for (int i = 0; i < 6; i++) {
            hashCode.Add(Transform[i]);
        }

        _storedHashCode = hashCode.ToHashCode();
        return (int)_storedHashCode;
    }

    //Creates an editable *copy* of this appearance, which must be added to the ServerAppearanceSystem to be used.
    [Pure]
    public MutableIconAppearance ToMutable() {
        MutableIconAppearance result = new MutableIconAppearance() {
            Name = Name,
            Icon = Icon,
            IconState = IconState,
            Direction = Direction,
            InheritsDirection = InheritsDirection,
            PixelOffset = PixelOffset,
            PixelOffset2 = PixelOffset2,
            Color = Color,
            Alpha = Alpha,
            GlideSize = GlideSize,
            ColorMatrix = ColorMatrix,
            Layer = Layer,
            Plane = Plane,
            RenderSource = RenderSource,
            RenderTarget = RenderTarget,
            BlendMode = BlendMode,
            AppearanceFlags = AppearanceFlags,
            Invisibility = Invisibility,
            Opacity = Opacity,
            MouseOpacity = MouseOpacity,
            Overlays = new(Overlays.Length),
            Underlays = new(Underlays.Length),
            VisContents = new(VisContents),
            Filters = new(Filters),
            Verbs = new(Verbs),
            Override = Override,
        };

        foreach(ImmutableIconAppearance overlay in Overlays)
            result.Overlays.Add(overlay.GetHashCode());

        foreach(ImmutableIconAppearance underlay in Underlays)
            result.Underlays.Add(underlay.GetHashCode());

        for (int i = 0; i < 6; i++) {
            result.Transform[i] = Transform[i];
        }

        return result;
    }

    public void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write((byte)IconAppearanceProperty.Id);
        buffer.WriteVariableInt32(GetHashCode());

        if (Name != MutableIconAppearance.Default.Name) {
            buffer.Write((byte)IconAppearanceProperty.Name);
            buffer.Write(Name);
        }

        if (Icon != null) {
            buffer.Write((byte)IconAppearanceProperty.Icon);
            buffer.WriteVariableInt32(Icon.Value);
        }

        if (IconState != null) {
            buffer.Write((byte)IconAppearanceProperty.IconState);
            buffer.Write(IconState);
        }

        if (Direction != MutableIconAppearance.Default.Direction) {
            buffer.Write((byte)IconAppearanceProperty.Direction);
            buffer.Write((byte)Direction);
        }

        if (InheritsDirection != true) {
            buffer.Write((byte)IconAppearanceProperty.DoesntInheritDirection);
        }

        if (PixelOffset != MutableIconAppearance.Default.PixelOffset) {
            buffer.Write((byte)IconAppearanceProperty.PixelOffset);
            buffer.WriteVariableInt32(PixelOffset.X);
            buffer.WriteVariableInt32(PixelOffset.Y);
        }

        if (PixelOffset2 != MutableIconAppearance.Default.PixelOffset2) {
            buffer.Write((byte)IconAppearanceProperty.PixelOffset2);
            buffer.WriteVariableInt32(PixelOffset2.X);
            buffer.WriteVariableInt32(PixelOffset2.Y);
        }

        if (Color != MutableIconAppearance.Default.Color) {
            buffer.Write((byte)IconAppearanceProperty.Color);
            buffer.Write(Color);
        }

        if (Alpha != MutableIconAppearance.Default.Alpha) {
            buffer.Write((byte)IconAppearanceProperty.Alpha);
            buffer.Write(Alpha);
        }

        if (!GlideSize.Equals(MutableIconAppearance.Default.GlideSize)) {
            buffer.Write((byte)IconAppearanceProperty.GlideSize);
            buffer.Write(GlideSize);
        }

        if (!ColorMatrix.Equals(MutableIconAppearance.Default.ColorMatrix)) {
            buffer.Write((byte)IconAppearanceProperty.ColorMatrix);

            foreach (var value in ColorMatrix.GetValues())
                buffer.Write(value);
        }

        if (!Layer.Equals(MutableIconAppearance.Default.Layer)) {
            buffer.Write((byte)IconAppearanceProperty.Layer);
            buffer.Write(Layer);
        }

        if (Plane != MutableIconAppearance.Default.Plane) {
            buffer.Write((byte)IconAppearanceProperty.Plane);
            buffer.WriteVariableInt32(Plane);
        }

        if (BlendMode != MutableIconAppearance.Default.BlendMode) {
            buffer.Write((byte)IconAppearanceProperty.BlendMode);
            buffer.Write((byte)BlendMode);
        }

        if (AppearanceFlags != MutableIconAppearance.Default.AppearanceFlags) {
            buffer.Write((byte)IconAppearanceProperty.AppearanceFlags);
            buffer.Write((int)AppearanceFlags);
        }

        if (Invisibility != MutableIconAppearance.Default.Invisibility) {
            buffer.Write((byte)IconAppearanceProperty.Invisibility);
            buffer.Write(Invisibility);
        }

        if (Opacity != MutableIconAppearance.Default.Opacity) {
            buffer.Write((byte)IconAppearanceProperty.Opacity);
            buffer.Write(Opacity);
        }

        if (Override != MutableIconAppearance.Default.Override) {
            buffer.Write((byte)IconAppearanceProperty.Override);
            buffer.Write(Override);
        }

        if (!string.IsNullOrWhiteSpace(RenderSource)) {
            buffer.Write((byte)IconAppearanceProperty.RenderSource);
            buffer.Write(RenderSource);
        }

        if (!string.IsNullOrWhiteSpace(RenderTarget)) {
            buffer.Write((byte)IconAppearanceProperty.RenderTarget);
            buffer.Write(RenderTarget);
        }

        if (MouseOpacity != MutableIconAppearance.Default.MouseOpacity) {
            buffer.Write((byte)IconAppearanceProperty.MouseOpacity);
            buffer.Write((byte)MouseOpacity);
        }

        if (Overlays.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.Overlays);

            buffer.WriteVariableInt32(Overlays.Length);
            foreach (var overlay in Overlays) {
                buffer.WriteVariableInt32(overlay.GetHashCode());
            }
        }

        if (Underlays.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.Underlays);

            buffer.WriteVariableInt32(Underlays.Length);
            foreach (var underlay in Underlays) {
                buffer.WriteVariableInt32(underlay.GetHashCode());
            }
        }

        if (VisContents.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.VisContents);

            buffer.WriteVariableInt32(VisContents.Length);
            foreach (var item in VisContents) {
                buffer.Write(item);
            }
        }

        if (Filters.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.Filters);

            buffer.Write(Filters.Length);
            foreach (var filter in Filters) {
                using var filterStream = new MemoryStream();

                serializer.Serialize(filterStream, filter);
                buffer.WriteVariableInt32((int)filterStream.Length);
                filterStream.TryGetBuffer(out var filterBuffer);
                buffer.Write(filterBuffer);
            }
        }

        if (Verbs.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.Verbs);

            buffer.WriteVariableInt32(Verbs.Length);
            foreach (var verb in Verbs) {
                buffer.WriteVariableInt32(verb);
            }
        }

        if (!Transform.SequenceEqual(MutableIconAppearance.Default.Transform)) {
            buffer.Write((byte)IconAppearanceProperty.Transform);

            for (int i = 0; i < 6; i++) {
                buffer.Write(Transform[i]);
            }
        }

        buffer.Write((byte)IconAppearanceProperty.End);
    }

    public int ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        throw new NotImplementedException();
    }

    ~ImmutableIconAppearance() {
        if(registered)
            appearanceSystem.RemoveAppearance(this);
    }

}

