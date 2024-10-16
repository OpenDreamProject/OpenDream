using System.Diagnostics.Contracts;
using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using System.Linq;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;
using System;
using OpenDreamShared.Rendering;
using System.Collections.Generic;
using Robust.Shared.Player;

namespace OpenDreamShared.Dream;

/*
 * Woe, weary traveler, modifying this class is not for the faint of heart.
 * If you modify MutableIconAppearance, be sure to update the following places:
 * - All of the methods on ImmutableIconAppearance itself
 * - MutableIconAppearance
 * - MutableIconAppearance methods in AtomManager
 * - There may be others
 */

// TODO: Wow this is huge! Probably look into splitting this by most used/least used to reduce the size of these
[Serializable]
public sealed class ImmutableIconAppearance : IEquatable<ImmutableIconAppearance>{
    private bool registered = false;
    [ViewVariables] public readonly string Name = MutableIconAppearance.Default.Name;
    [ViewVariables] public readonly int? Icon = MutableIconAppearance.Default.Icon;
    [ViewVariables] public readonly string? IconState = MutableIconAppearance.Default.IconState;
    [ViewVariables] public readonly AtomDirection Direction = MutableIconAppearance.Default.Direction;
    [ViewVariables] public readonly bool InheritsDirection = MutableIconAppearance.Default.InheritsDirection; // Inherits direction when used as an overlay
    [ViewVariables] public readonly Vector2i PixelOffset = MutableIconAppearance.Default.PixelOffset;  // pixel_x and pixel_y
    [ViewVariables] public readonly Vector2i PixelOffset2 = MutableIconAppearance.Default.PixelOffset2; // pixel_w and pixel_z
    [ViewVariables] public readonly Color Color = MutableIconAppearance.Default.Color;
    [ViewVariables] public readonly byte Alpha = MutableIconAppearance.Default.Alpha;
    [ViewVariables] public readonly float GlideSize = MutableIconAppearance.Default.GlideSize;
    [ViewVariables] public readonly float Layer = MutableIconAppearance.Default.Layer;
    [ViewVariables] public int Plane = MutableIconAppearance.Default.Plane;
    [ViewVariables] public readonly BlendMode BlendMode = MutableIconAppearance.Default.BlendMode;
    [ViewVariables] public readonly AppearanceFlags AppearanceFlags = MutableIconAppearance.Default.AppearanceFlags;
    [ViewVariables] public readonly sbyte Invisibility = MutableIconAppearance.Default.Invisibility;
    [ViewVariables] public readonly bool Opacity = MutableIconAppearance.Default.Opacity;
    [ViewVariables] public readonly bool Override = MutableIconAppearance.Default.Override;
    [ViewVariables] public readonly string? RenderSource = MutableIconAppearance.Default.RenderSource;
    [ViewVariables] public readonly string? RenderTarget = MutableIconAppearance.Default.RenderTarget;
    [ViewVariables] public readonly MouseOpacity MouseOpacity = MutableIconAppearance.Default.MouseOpacity;
    [ViewVariables] public readonly ImmutableIconAppearance[] Overlays;
    [ViewVariables] public readonly ImmutableIconAppearance[] Underlays;
    [NonSerialized]
    private List<int>? _overlayIDs;
    [NonSerialized]
    private List<int>? _underlayIDs;
    [ViewVariables] public readonly Robust.Shared.GameObjects.NetEntity[] VisContents;
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
    private readonly SharedAppearanceSystem? appearanceSystem;

    public void MarkRegistered(){
        registered = true;
    }

    //this should only be called client-side, after network transfer
    public void ResolveOverlays(SharedAppearanceSystem appearanceSystem) {
        if(_overlayIDs is not null)
            for (int i = 0; i < _overlayIDs.Count; i++)
                Overlays[i] = appearanceSystem.MustGetAppearanceById(_overlayIDs[i]);

        if(_underlayIDs is not null)
            for (int i = 0; i < _underlayIDs.Count; i++)
                Underlays[i] = appearanceSystem.MustGetAppearanceById(_underlayIDs[i]);

        _overlayIDs = null;
        _underlayIDs = null;
    }


    public ImmutableIconAppearance(MutableIconAppearance appearance, SharedAppearanceSystem? serverAppearanceSystem) {
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

        Overlays = appearance.Overlays.ToArray();
        Underlays = appearance.Underlays.ToArray();

        VisContents = appearance.VisContents.ToArray();
        Filters = appearance.Filters.ToArray();
        Verbs = appearance.Verbs.ToArray();
        Override = appearance.Override;

        for (int i = 0; i < 6; i++) {
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
            hashCode.Add(overlay.GetHashCode());
        }

        foreach (ImmutableIconAppearance underlay in Underlays) {
            hashCode.Add(underlay.GetHashCode());
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

   public ImmutableIconAppearance(NetIncomingMessage buffer, IRobustSerializer serializer) {
        Overlays = [];
        Underlays = [];
        VisContents = [];
        Filters = [];
        Verbs =[];


        var property = (IconAppearanceProperty)buffer.ReadByte();
        while (property != IconAppearanceProperty.End) {
            switch (property) {
                case IconAppearanceProperty.Name:
                    Name = buffer.ReadString();
                    break;
                case IconAppearanceProperty.Id:
                    _storedHashCode = buffer.ReadVariableInt32();
                    break;
                case IconAppearanceProperty.Icon:
                    Icon = buffer.ReadVariableInt32();
                    break;
                case IconAppearanceProperty.IconState:
                    IconState = buffer.ReadString();
                    break;
                case IconAppearanceProperty.Direction:
                    Direction = (AtomDirection)buffer.ReadByte();
                    break;
                case IconAppearanceProperty.DoesntInheritDirection:
                    InheritsDirection = false;
                    break;
                case IconAppearanceProperty.PixelOffset:
                    PixelOffset = (buffer.ReadVariableInt32(), buffer.ReadVariableInt32());
                    break;
                case IconAppearanceProperty.PixelOffset2:
                    PixelOffset2 = (buffer.ReadVariableInt32(), buffer.ReadVariableInt32());
                    break;
                case IconAppearanceProperty.Color:
                    Color = buffer.ReadColor();
                    break;
                case IconAppearanceProperty.Alpha:
                    Alpha = buffer.ReadByte();
                    break;
                case IconAppearanceProperty.GlideSize:
                    GlideSize = buffer.ReadFloat();
                    break;
                case IconAppearanceProperty.Layer:
                    Layer = buffer.ReadFloat();
                    break;
                case IconAppearanceProperty.Plane:
                    Plane = buffer.ReadVariableInt32();
                    break;
                case IconAppearanceProperty.BlendMode:
                    BlendMode = (BlendMode)buffer.ReadByte();
                    break;
                case IconAppearanceProperty.AppearanceFlags:
                    AppearanceFlags = (AppearanceFlags)buffer.ReadInt32();
                    break;
                case IconAppearanceProperty.Invisibility:
                    Invisibility = buffer.ReadSByte();
                    break;
                case IconAppearanceProperty.Opacity:
                    Opacity = buffer.ReadBoolean();
                    break;
                case IconAppearanceProperty.Override:
                    Override = buffer.ReadBoolean();
                    break;
                case IconAppearanceProperty.RenderSource:
                    RenderSource = buffer.ReadString();
                    break;
                case IconAppearanceProperty.RenderTarget:
                    RenderTarget = buffer.ReadString();
                    break;
                case IconAppearanceProperty.MouseOpacity:
                    MouseOpacity = (MouseOpacity)buffer.ReadByte();
                    break;
                case IconAppearanceProperty.ColorMatrix:
                    ColorMatrix = new(
                        buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                        buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                        buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                        buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                        buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()
                    );

                    break;
                case IconAppearanceProperty.Overlays: {
                    var overlaysCount = buffer.ReadVariableInt32();

                    Overlays = new ImmutableIconAppearance[overlaysCount];
                    _overlayIDs = new(overlaysCount);
                    for (int overlaysI = 0; overlaysI < overlaysCount; overlaysI++) {
                        _overlayIDs.Add(buffer.ReadVariableInt32());
                    }

                    break;
                }
                case IconAppearanceProperty.Underlays: {
                    var underlaysCount = buffer.ReadVariableInt32();

                    Underlays = new ImmutableIconAppearance[underlaysCount];
                    _underlayIDs = new(underlaysCount);
                    for (int underlaysI = 0; underlaysI < underlaysCount; underlaysI++) {
                        _underlayIDs.Add(buffer.ReadVariableInt32());
                    }

                    break;
                }
                case IconAppearanceProperty.VisContents: {
                    var visContentsCount = buffer.ReadVariableInt32();

                    VisContents = new Robust.Shared.GameObjects.NetEntity[visContentsCount];
                    for (int visContentsI = 0; visContentsI < visContentsCount; visContentsI++) {
                        VisContents[visContentsI] = buffer.ReadNetEntity();
                    }

                    break;
                }
                case IconAppearanceProperty.Filters: {
                    var filtersCount = buffer.ReadInt32();

                    Filters = new DreamFilter[filtersCount];
                    for (int filtersI = 0; filtersI < filtersCount; filtersI++) {
                        var filterLength = buffer.ReadVariableInt32();
                        var filterData = buffer.ReadBytes(filterLength);
                        using var filterStream = new MemoryStream(filterData);
                        var filter = serializer.Deserialize<DreamFilter>(filterStream);

                        Filters[filtersI] = filter;
                    }

                    break;
                }
                case IconAppearanceProperty.Verbs: {
                    var verbsCount = buffer.ReadVariableInt32();

                    Verbs = new int[verbsCount];
                    for (int verbsI = 0; verbsI < verbsCount; verbsI++) {
                        Verbs[verbsI] = buffer.ReadVariableInt32();
                    }

                    break;
                }
                case IconAppearanceProperty.Transform: {
                    Transform = [
                        buffer.ReadSingle(), buffer.ReadSingle(),
                        buffer.ReadSingle(), buffer.ReadSingle(),
                        buffer.ReadSingle(), buffer.ReadSingle()
                    ];

                    break;
                }
                default:
                    throw new Exception($"Invalid property {property}");
            }

            property = (IconAppearanceProperty)buffer.ReadByte();
        }

        if(_storedHashCode is null)
            throw new Exception("No appearance ID found in buffer");
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
            result.Overlays.Add(overlay);

        foreach(ImmutableIconAppearance underlay in Underlays)
            result.Underlays.Add(underlay);

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
            appearanceSystem!.RemoveAppearance(this);
    }

}

