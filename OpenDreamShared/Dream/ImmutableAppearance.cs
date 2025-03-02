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
using System.Diagnostics.CodeAnalysis;

namespace OpenDreamShared.Dream;

/*
 * Woe, weary traveler, modifying this class is not for the faint of heart.
 * If you modify MutableAppearance, be sure to update the following places:
 * - All of the methods on ImmutableAppearance itself
 * - MutableAppearance
 * - MutableAppearance methods in AtomManager
 * - There may be others
 */

// TODO: Wow this is huge! Probably look into splitting this by most used/least used to reduce the size of these
[Serializable, NetSerializable]
public sealed class ImmutableAppearance : IEquatable<ImmutableAppearance> {
    private uint? _registeredId;

    [ViewVariables] public readonly string Name = MutableAppearance.Default.Name;
    [ViewVariables] public readonly string? Desc = MutableAppearance.Default.Desc;
    [ViewVariables] public readonly int? Icon = MutableAppearance.Default.Icon;
    [ViewVariables] public readonly string? IconState = MutableAppearance.Default.IconState;
    [ViewVariables] public readonly AtomDirection Direction = MutableAppearance.Default.Direction;
    [ViewVariables] public readonly bool InheritsDirection = MutableAppearance.Default.InheritsDirection; // Inherits direction when used as an overlay
    [ViewVariables] public readonly Vector2i PixelOffset = MutableAppearance.Default.PixelOffset;  // pixel_x and pixel_y
    [ViewVariables] public readonly Vector2i PixelOffset2 = MutableAppearance.Default.PixelOffset2; // pixel_w and pixel_z
    [ViewVariables] public readonly Color Color = MutableAppearance.Default.Color;
    [ViewVariables] public readonly byte Alpha = MutableAppearance.Default.Alpha;
    [ViewVariables] public readonly float GlideSize = MutableAppearance.Default.GlideSize;
    [ViewVariables] public readonly float Layer = MutableAppearance.Default.Layer;
    [ViewVariables] public readonly int Plane = MutableAppearance.Default.Plane;
    [ViewVariables] public readonly BlendMode BlendMode = MutableAppearance.Default.BlendMode;
    [ViewVariables] public readonly AppearanceFlags AppearanceFlags = MutableAppearance.Default.AppearanceFlags;
    [ViewVariables] public readonly sbyte Invisibility = MutableAppearance.Default.Invisibility;
    [ViewVariables] public readonly bool Opacity = MutableAppearance.Default.Opacity;
    [ViewVariables] public readonly bool Override = MutableAppearance.Default.Override;
    [ViewVariables] public readonly string? RenderSource = MutableAppearance.Default.RenderSource;
    [ViewVariables] public readonly string? RenderTarget = MutableAppearance.Default.RenderTarget;
    [ViewVariables] public readonly MouseOpacity MouseOpacity = MutableAppearance.Default.MouseOpacity;
    [ViewVariables] public readonly ImmutableAppearance[] Overlays;
    [ViewVariables] public readonly ImmutableAppearance[] Underlays;
    [ViewVariables] public readonly Robust.Shared.GameObjects.NetEntity[] VisContents;
    [ViewVariables] public readonly DreamFilter[] Filters;
    [ViewVariables] public readonly int[] Verbs;
    [ViewVariables] public readonly ColorMatrix ColorMatrix = ColorMatrix.Identity;
    [ViewVariables] public Vector2i MaptextSize = MutableAppearance.Default.MaptextSize;
    [ViewVariables] public Vector2i MaptextOffset = MutableAppearance.Default.MaptextOffset;
    [ViewVariables] public string? Maptext = MutableAppearance.Default.Maptext;
    [ViewVariables] public AtomMouseEvents EnabledMouseEvents;

    /// <summary> The Transform property of this appearance, in [a,d,b,e,c,f] order</summary>
    [ViewVariables] public readonly float[] Transform = [
        1, 0,   // a d
        0, 1,   // b e
        0, 0    // c f
    ];

    // PixelOffset2 behaves the same as PixelOffset in top-down mode, so this is used
    public Vector2i TotalPixelOffset => PixelOffset + PixelOffset2;

    [NonSerialized] private readonly SharedAppearanceSystem? _appearanceSystem;
    [NonSerialized] private bool _needsFinalizer;
    [NonSerialized] private int? _storedHashCode;
    [NonSerialized] private List<uint>? _overlayIDs;
    [NonSerialized] private List<uint>? _underlayIDs;

    public ImmutableAppearance(MutableAppearance appearance, SharedAppearanceSystem? serverAppearanceSystem) {
        _appearanceSystem = serverAppearanceSystem;

        Name = appearance.Name;
        Desc = appearance.Desc;
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
        Maptext = appearance.Maptext;
        MaptextSize = appearance.MaptextSize;
        MaptextOffset = appearance.MaptextOffset;
        EnabledMouseEvents = appearance.EnabledMouseEvents;

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

    ~ImmutableAppearance() {
        if(_needsFinalizer && _registeredId is not null)
            _appearanceSystem!.RemoveAppearance(this);
    }

    public void MarkRegistered(uint registeredId){
        _registeredId = registeredId;
        _needsFinalizer = true;
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

    public override bool Equals(object? obj) => obj is ImmutableAppearance immutable && Equals(immutable);

    public bool Equals(ImmutableAppearance? immutableAppearance) {
        if (immutableAppearance == null) return false;

        if (immutableAppearance.Name != Name) return false;
        if (immutableAppearance.Desc != Desc) return false;
        if (immutableAppearance.Icon != Icon) return false;
        if (immutableAppearance.IconState != IconState) return false;
        if (immutableAppearance.Direction != Direction) return false;
        if (immutableAppearance.InheritsDirection != InheritsDirection) return false;
        if (immutableAppearance.PixelOffset != PixelOffset) return false;
        if (immutableAppearance.PixelOffset2 != PixelOffset2) return false;
        if (immutableAppearance.Color != Color) return false;
        if (immutableAppearance.Alpha != Alpha) return false;
        if (!immutableAppearance.GlideSize.Equals(GlideSize)) return false;
        if (!immutableAppearance.ColorMatrix.Equals(ColorMatrix)) return false;
        if (!immutableAppearance.Layer.Equals(Layer)) return false;
        if (immutableAppearance.Plane != Plane) return false;
        if (immutableAppearance.RenderSource != RenderSource) return false;
        if (immutableAppearance.RenderTarget != RenderTarget) return false;
        if (immutableAppearance.BlendMode != BlendMode) return false;
        if (immutableAppearance.AppearanceFlags != AppearanceFlags) return false;
        if (immutableAppearance.Invisibility != Invisibility) return false;
        if (immutableAppearance.Opacity != Opacity) return false;
        if (immutableAppearance.MouseOpacity != MouseOpacity) return false;
        if (immutableAppearance.Overlays.Length != Overlays.Length) return false;
        if (immutableAppearance.Underlays.Length != Underlays.Length) return false;
        if (immutableAppearance.VisContents.Length != VisContents.Length) return false;
        if (immutableAppearance.Filters.Length != Filters.Length) return false;
        if (immutableAppearance.Verbs.Length != Verbs.Length) return false;
        if (immutableAppearance.Override != Override) return false;
        if (immutableAppearance.Maptext != Maptext) return false;
        if (immutableAppearance.MaptextSize != MaptextSize) return false;
        if (immutableAppearance.MaptextOffset != MaptextOffset) return false;

        for (int i = 0; i < Filters.Length; i++) {
            if (immutableAppearance.Filters[i] != Filters[i]) return false;
        }

        for (int i = 0; i < Overlays.Length; i++) {
            if (!immutableAppearance.Overlays[i].Equals(Overlays[i])) return false;
        }

        for (int i = 0; i < Underlays.Length; i++) {
            if (!immutableAppearance.Underlays[i].Equals(Underlays[i])) return false;
        }

        for (int i = 0; i < VisContents.Length; i++) {
            if (immutableAppearance.VisContents[i] != VisContents[i]) return false;
        }

        for (int i = 0; i < Verbs.Length; i++) {
            if (immutableAppearance.Verbs[i] != Verbs[i]) return false;
        }

        for (int i = 0; i < 6; i++) {
            if (!immutableAppearance.Transform[i].Equals(Transform[i])) return false;
        }

        return true;
    }

    public uint MustGetId() {
        if(_registeredId is null)
            throw new InvalidDataException("GetID() was called on an appearance without an ID");
        return (uint)_registeredId;
    }

    public bool TryGetId([NotNullWhen(true)] out uint? id) {
        id = _registeredId;
        return _registeredId is not null;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() {
        if(_storedHashCode is not null) //because everything is readonly, this only needs to be done once
            return (int)_storedHashCode;

        HashCode hashCode = new HashCode();

        hashCode.Add(Name);
        hashCode.Add(Desc);
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
        hashCode.Add(Override);
        hashCode.Add(MouseOpacity);
        hashCode.Add(Alpha);
        hashCode.Add(GlideSize);
        hashCode.Add(Plane);
        hashCode.Add(RenderSource);
        hashCode.Add(RenderTarget);
        hashCode.Add(BlendMode);
        hashCode.Add(AppearanceFlags);
        hashCode.Add(Maptext);
        hashCode.Add(MaptextOffset);
        hashCode.Add(MaptextSize);

        foreach (ImmutableAppearance overlay in Overlays) {
            hashCode.Add(overlay.GetHashCode());
        }

        foreach (ImmutableAppearance underlay in Underlays) {
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

    public ImmutableAppearance(NetIncomingMessage buffer, IRobustSerializer serializer) {
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
                case IconAppearanceProperty.Desc:
                    Desc = buffer.ReadString();
                    break;
                case IconAppearanceProperty.Id:
                    _registeredId = buffer.ReadVariableUInt32();
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

                    Overlays = new ImmutableAppearance[overlaysCount];
                    _overlayIDs = new(overlaysCount);
                    for (int overlaysI = 0; overlaysI < overlaysCount; overlaysI++) {
                        _overlayIDs.Add(buffer.ReadVariableUInt32());
                    }

                    break;
                }
                case IconAppearanceProperty.Underlays: {
                    var underlaysCount = buffer.ReadVariableInt32();

                    Underlays = new ImmutableAppearance[underlaysCount];
                    _underlayIDs = new(underlaysCount);
                    for (int underlaysI = 0; underlaysI < underlaysCount; underlaysI++) {
                        _underlayIDs.Add(buffer.ReadVariableUInt32());
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
                case IconAppearanceProperty.Maptext: {
                    Maptext = buffer.ReadString();
                    break;
                }
                case IconAppearanceProperty.MaptextSize: {
                    MaptextSize = (buffer.ReadVariableInt32(), buffer.ReadVariableInt32());
                    break;
                }
                case IconAppearanceProperty.MaptextOffset: {
                    MaptextOffset = (buffer.ReadVariableInt32(), buffer.ReadVariableInt32());
                    break;
                }
                case IconAppearanceProperty.EnabledMouseEvents: {
                    EnabledMouseEvents = (AtomMouseEvents)buffer.ReadByte();
                    break;
                }
                default:
                    throw new Exception($"Invalid property {property}");
            }

            property = (IconAppearanceProperty)buffer.ReadByte();
        }

        if(_registeredId is null)
            throw new Exception("No appearance ID found in buffer");
    }

    //Creates an editable *copy* of this appearance, which must be added to the ServerAppearanceSystem to be used.
    [Pure]
    public MutableAppearance ToMutable() {
        MutableAppearance result = MutableAppearance.Get();

        result.Name = Name;
        result.Desc = Desc;
        result.Icon = Icon;
        result.IconState = IconState;
        result.Direction = Direction;
        result.InheritsDirection = InheritsDirection;
        result.PixelOffset = PixelOffset;
        result.PixelOffset2 = PixelOffset2;
        result.Color = Color;
        result.Alpha = Alpha;
        result.GlideSize = GlideSize;
        result.ColorMatrix = ColorMatrix;
        result.Layer = Layer;
        result.Plane = Plane;
        result.RenderSource = RenderSource;
        result.RenderTarget = RenderTarget;
        result.BlendMode = BlendMode;
        result.AppearanceFlags = AppearanceFlags;
        result.Invisibility = Invisibility;
        result.Opacity = Opacity;
        result.MouseOpacity = MouseOpacity;
        result.Override = Override;
        result.Maptext = Maptext;
        result.MaptextOffset = MaptextOffset;
        result.MaptextSize = MaptextSize;
        result.EnabledMouseEvents = EnabledMouseEvents;

        result.Overlays.EnsureCapacity(Overlays.Length);
        result.Underlays.EnsureCapacity(Underlays.Length);
        result.VisContents.EnsureCapacity(VisContents.Length);
        result.Filters.EnsureCapacity(Filters.Length);
        result.Verbs.EnsureCapacity(Verbs.Length);
        result.Overlays.AddRange(Overlays);
        result.Underlays.AddRange(Underlays);
        result.VisContents.AddRange(VisContents);
        result.Filters.AddRange(Filters);
        result.Verbs.AddRange(Verbs);
        Array.Copy(Transform, result.Transform, 6);

        return result;
    }

    public void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write((byte)IconAppearanceProperty.Id);
        buffer.WriteVariableUInt32(MustGetId());

        if (Name != MutableAppearance.Default.Name) {
            buffer.Write((byte)IconAppearanceProperty.Name);
            buffer.Write(Name);
        }

        if (Desc != MutableAppearance.Default.Desc) {
            buffer.Write((byte)IconAppearanceProperty.Desc);
            buffer.Write(Desc);
        }

        if (Icon != null) {
            buffer.Write((byte)IconAppearanceProperty.Icon);
            buffer.WriteVariableInt32(Icon.Value);
        }

        if (IconState != null) {
            buffer.Write((byte)IconAppearanceProperty.IconState);
            buffer.Write(IconState);
        }

        if (Direction != MutableAppearance.Default.Direction) {
            buffer.Write((byte)IconAppearanceProperty.Direction);
            buffer.Write((byte)Direction);
        }

        if (InheritsDirection != true) {
            buffer.Write((byte)IconAppearanceProperty.DoesntInheritDirection);
        }

        if (PixelOffset != MutableAppearance.Default.PixelOffset) {
            buffer.Write((byte)IconAppearanceProperty.PixelOffset);
            buffer.WriteVariableInt32(PixelOffset.X);
            buffer.WriteVariableInt32(PixelOffset.Y);
        }

        if (PixelOffset2 != MutableAppearance.Default.PixelOffset2) {
            buffer.Write((byte)IconAppearanceProperty.PixelOffset2);
            buffer.WriteVariableInt32(PixelOffset2.X);
            buffer.WriteVariableInt32(PixelOffset2.Y);
        }

        if (Color != MutableAppearance.Default.Color) {
            buffer.Write((byte)IconAppearanceProperty.Color);
            buffer.Write(Color);
        }

        if (Alpha != MutableAppearance.Default.Alpha) {
            buffer.Write((byte)IconAppearanceProperty.Alpha);
            buffer.Write(Alpha);
        }

        if (!GlideSize.Equals(MutableAppearance.Default.GlideSize)) {
            buffer.Write((byte)IconAppearanceProperty.GlideSize);
            buffer.Write(GlideSize);
        }

        if (!ColorMatrix.Equals(MutableAppearance.Default.ColorMatrix)) {
            buffer.Write((byte)IconAppearanceProperty.ColorMatrix);

            foreach (var value in ColorMatrix.GetValues())
                buffer.Write(value);
        }

        if (!Layer.Equals(MutableAppearance.Default.Layer)) {
            buffer.Write((byte)IconAppearanceProperty.Layer);
            buffer.Write(Layer);
        }

        if (Plane != MutableAppearance.Default.Plane) {
            buffer.Write((byte)IconAppearanceProperty.Plane);
            buffer.WriteVariableInt32(Plane);
        }

        if (BlendMode != MutableAppearance.Default.BlendMode) {
            buffer.Write((byte)IconAppearanceProperty.BlendMode);
            buffer.Write((byte)BlendMode);
        }

        if (AppearanceFlags != MutableAppearance.Default.AppearanceFlags) {
            buffer.Write((byte)IconAppearanceProperty.AppearanceFlags);
            buffer.Write((int)AppearanceFlags);
        }

        if (Invisibility != MutableAppearance.Default.Invisibility) {
            buffer.Write((byte)IconAppearanceProperty.Invisibility);
            buffer.Write(Invisibility);
        }

        if (Opacity != MutableAppearance.Default.Opacity) {
            buffer.Write((byte)IconAppearanceProperty.Opacity);
            buffer.Write(Opacity);
        }

        if (Override != MutableAppearance.Default.Override) {
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

        if (MouseOpacity != MutableAppearance.Default.MouseOpacity) {
            buffer.Write((byte)IconAppearanceProperty.MouseOpacity);
            buffer.Write((byte)MouseOpacity);
        }

        if (Overlays.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.Overlays);

            buffer.WriteVariableInt32(Overlays.Length);
            foreach (var overlay in Overlays) {
                buffer.WriteVariableUInt32(overlay.MustGetId());
            }
        }

        if (Underlays.Length != 0) {
            buffer.Write((byte)IconAppearanceProperty.Underlays);

            buffer.WriteVariableInt32(Underlays.Length);
            foreach (var underlay in Underlays) {
                buffer.WriteVariableUInt32(underlay.MustGetId());
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

        if (!Transform.SequenceEqual(MutableAppearance.Default.Transform)) {
            buffer.Write((byte)IconAppearanceProperty.Transform);

            for (int i = 0; i < 6; i++) {
                buffer.Write(Transform[i]);
            }
        }

        if(!string.IsNullOrEmpty(Maptext)){
            buffer.Write((byte) IconAppearanceProperty.Maptext);
            buffer.Write(Maptext);
        }

        if (MaptextOffset != MutableAppearance.Default.MaptextOffset) {
            buffer.Write((byte)IconAppearanceProperty.MaptextOffset);
            buffer.WriteVariableInt32(MaptextOffset.X);
            buffer.WriteVariableInt32(MaptextOffset.Y);
        }

        if (MaptextSize != MutableAppearance.Default.MaptextSize) {
            buffer.Write((byte)IconAppearanceProperty.MaptextSize);
            buffer.WriteVariableInt32(MaptextSize.X);
            buffer.WriteVariableInt32(MaptextSize.Y);
        }

        if (EnabledMouseEvents != MutableAppearance.Default.EnabledMouseEvents) {
            buffer.Write((byte)IconAppearanceProperty.EnabledMouseEvents);
            buffer.Write((byte)EnabledMouseEvents);
        }

        buffer.Write((byte)IconAppearanceProperty.End);
    }

    public int ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        throw new NotImplementedException();
    }
}

