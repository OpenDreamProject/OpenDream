﻿using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Lidgren.Network;
using Robust.Shared.Network;
using System.IO;

namespace OpenDreamShared.Dream;

/*
 * Woe, weary traveler, modifying this class is not for the faint of heart.
 * If you modify MutableIconAppearance, be sure to update the following places:
 * - All of the methods on MutableIconAppearance itself
 * - ImmutableIconAppearance
 * - IconAppearance methods in AtomManager
 * - MsgAllAppearances
 * - IconDebugWindow
 * - IconAppearanceProperty enum
 * - There may be others
 */

// TODO: Wow this is huge! Probably look into splitting this by most used/least used to reduce the size of these
public sealed class MutableIconAppearance : IEquatable<MutableIconAppearance>, IBufferableAppearance {
    public static readonly MutableIconAppearance Default = new();

    [ViewVariables] public string Name = string.Empty;
    [ViewVariables] public int? Icon;
    [ViewVariables] public string? IconState;
    [ViewVariables] public AtomDirection Direction = AtomDirection.South;
    [ViewVariables] public bool InheritsDirection = true; // Inherits direction when used as an overlay
    [ViewVariables] public Vector2i PixelOffset;  // pixel_x and pixel_y
    [ViewVariables] public Vector2i PixelOffset2; // pixel_w and pixel_z
    [ViewVariables] public Color Color = Color.White;
    [ViewVariables] public byte Alpha = 255;
    [ViewVariables] public float GlideSize;
    [ViewVariables] public float Layer = -1f;
    [ViewVariables] public int Plane = -32767;
    [ViewVariables] public BlendMode BlendMode = BlendMode.Default;
    [ViewVariables] public AppearanceFlags AppearanceFlags = AppearanceFlags.None;
    [ViewVariables] public sbyte Invisibility;
    [ViewVariables] public bool Opacity;
    [ViewVariables] public bool Override;
    [ViewVariables] public string? RenderSource;
    [ViewVariables] public string? RenderTarget;
    [ViewVariables] public MouseOpacity MouseOpacity = MouseOpacity.PixelOpaque;
    [ViewVariables] public List<int> Overlays;
    [ViewVariables] public List<int> Underlays;
    [ViewVariables] public List<NetEntity> VisContents;
    [ViewVariables] public List<DreamFilter> Filters;
    [ViewVariables] public List<int> Verbs;

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
    [ViewVariables] public ColorMatrix ColorMatrix = ColorMatrix.Identity;

    /// <summary> The Transform property of this appearance, in [a,d,b,e,c,f] order</summary>
    [ViewVariables] public float[] Transform = [
        1, 0,   // a d
        0, 1,   // b e
        0, 0    // c f
    ];

    // PixelOffset2 behaves the same as PixelOffset in top-down mode, so this is used
    public Vector2i TotalPixelOffset => PixelOffset + PixelOffset2;

    public MutableIconAppearance() {
        Overlays = new();
        Underlays = new();
        VisContents = new();
        Filters = new();
        Verbs = new();
    }

    public MutableIconAppearance(MutableIconAppearance appearance) {
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
        Overlays = new(appearance.Overlays);
        Underlays = new(appearance.Underlays);
        VisContents = new(appearance.VisContents);
        Filters = new(appearance.Filters);
        Verbs = new(appearance.Verbs);
        Override = appearance.Override;

        for (int i = 0; i < 6; i++) {
            Transform[i] = appearance.Transform[i];
        }
    }

    public override bool Equals(object? obj) => obj is MutableIconAppearance appearance && Equals(appearance);

    public bool Equals(MutableIconAppearance? appearance) {
        if (appearance == null) return false;

        if (appearance.Name != Name) return false;
        if (appearance.Icon != Icon) return false;
        if (appearance.IconState != IconState) return false;
        if (appearance.Direction != Direction) return false;
        if (appearance.InheritsDirection != InheritsDirection) return false;
        if (appearance.PixelOffset != PixelOffset) return false;
        if (appearance.PixelOffset2 != PixelOffset2) return false;
        if (appearance.Color != Color) return false;
        if (appearance.Alpha != Alpha) return false;
        if (!appearance.GlideSize.Equals(GlideSize)) return false;
        if (!appearance.ColorMatrix.Equals(ColorMatrix)) return false;
        if (!appearance.Layer.Equals(Layer)) return false;
        if (appearance.Plane != Plane) return false;
        if (appearance.RenderSource != RenderSource) return false;
        if (appearance.RenderTarget != RenderTarget) return false;
        if (appearance.BlendMode != BlendMode) return false;
        if (appearance.AppearanceFlags != AppearanceFlags) return false;
        if (appearance.Invisibility != Invisibility) return false;
        if (appearance.Opacity != Opacity) return false;
        if (appearance.MouseOpacity != MouseOpacity) return false;
        if (appearance.Overlays.Count != Overlays.Count) return false;
        if (appearance.Underlays.Count != Underlays.Count) return false;
        if (appearance.VisContents.Count != VisContents.Count) return false;
        if (appearance.Filters.Count != Filters.Count) return false;
        if (appearance.Verbs.Count != Verbs.Count) return false;
        if (appearance.Override != Override) return false;

        for (int i = 0; i < Filters.Count; i++) {
            if (appearance.Filters[i] != Filters[i]) return false;
        }

        for (int i = 0; i < Overlays.Count; i++) {
            if (appearance.Overlays[i] != Overlays[i]) return false;
        }

        for (int i = 0; i < Underlays.Count; i++) {
            if (appearance.Underlays[i] != Underlays[i]) return false;
        }

        for (int i = 0; i < VisContents.Count; i++) {
            if (appearance.VisContents[i] != VisContents[i]) return false;
        }

        for (int i = 0; i < Verbs.Count; i++) {
            if (appearance.Verbs[i] != Verbs[i]) return false;
        }

        for (int i = 0; i < 6; i++) {
            if (!appearance.Transform[i].Equals(Transform[i])) return false;
        }

        return true;
    }

    /// <summary>
    /// This is a helper used for both optimization and parity. <br/>
    /// In BYOND, if a color matrix is representable as an RGBA color string, <br/>
    /// then it is coerced into one internally before being saved onto some appearance. <br/>
    /// This does the linear algebra madness necessary to determine whether this is the case or not.
    /// </summary>
    private static bool TryRepresentMatrixAsRgbaColor(in ColorMatrix matrix, [NotNullWhen(true)] out Color? maybeColor) {
        maybeColor = null;

        // The R G B A values need to be bounded [0,1] for a color conversion to work;
        // anything higher implies trying to render "superblue" or something.
        float diagonalSum = 0f;
        foreach (float diagonalValue in matrix.GetDiagonal()) {
            if (diagonalValue < 0 || diagonalValue > 1)
                return false;
            diagonalSum += diagonalValue;
        }

        // and then all of the other values need to be zero, including the offset vector.
        float sum = 0f;
        foreach (float value in matrix.GetValues()) {
            if (value < 0f) // To avoid situations like negatives and positives cancelling out this checksum.
                return false;
            sum += value;
        }

        if (sum - diagonalSum == 0) // PREEETTY sure I can trust the floating-point math here. Not 100% though
            maybeColor = new Color(matrix.c11, matrix.c22, matrix.c33, matrix.c44);
        return maybeColor is not null;
    }

    public override int GetHashCode() {
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

        foreach (int overlay in Overlays) {
            hashCode.Add(overlay);
        }

        foreach (int underlay in Underlays) {
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

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Parses the given colour string and sets this appearance to use it.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if color is not valid.</exception>
    public void SetColor(string color) {
        // TODO: the BYOND compiler enforces valid colors *unless* it's a map edit, in which case an empty string is allowed
        ColorMatrix = ColorMatrix.Identity; // reset our color matrix if we had one

        if (!ColorHelpers.TryParseColor(color, out Color)) {
            Color = Color.White;
        }
    }

    /// <summary>
    /// Sets the 'color' attribute to a color matrix, which will be used on the icon later on by a shader.
    /// </summary>
    public void SetColor(in ColorMatrix matrix) {
        if (TryRepresentMatrixAsRgbaColor(matrix, out var matrixColor)) {
            Color = matrixColor.Value;
            ColorMatrix = ColorMatrix.Identity;
            return;
        }

        Color = Color.White;
        ColorMatrix = matrix;
    }


    public int ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        int? appearanceId = null;
        var property = (IconAppearanceProperty)buffer.ReadByte();
        while (property != IconAppearanceProperty.End) {
            switch (property) {
                case IconAppearanceProperty.Name:
                    Name = buffer.ReadString();
                    break;
                case IconAppearanceProperty.Id:
                    appearanceId = buffer.ReadVariableInt32();
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

                    Overlays.EnsureCapacity(overlaysCount);
                    for (int overlaysI = 0; overlaysI < overlaysCount; overlaysI++) {
                        Overlays.Add(buffer.ReadVariableInt32());
                    }

                    break;
                }
                case IconAppearanceProperty.Underlays: {
                    var underlaysCount = buffer.ReadVariableInt32();

                    Underlays.EnsureCapacity(underlaysCount);
                    for (int underlaysI = 0; underlaysI < underlaysCount; underlaysI++) {
                        Underlays.Add(buffer.ReadVariableInt32());
                    }

                    break;
                }
                case IconAppearanceProperty.VisContents: {
                    var visContentsCount = buffer.ReadVariableInt32();

                    VisContents.EnsureCapacity(visContentsCount);
                    for (int visContentsI = 0; visContentsI < visContentsCount; visContentsI++) {
                        VisContents.Add(buffer.ReadNetEntity());
                    }

                    break;
                }
                case IconAppearanceProperty.Filters: {
                    var filtersCount = buffer.ReadInt32();

                    Filters.EnsureCapacity(filtersCount);
                    for (int filtersI = 0; filtersI < filtersCount; filtersI++) {
                        var filterLength = buffer.ReadVariableInt32();
                        var filterData = buffer.ReadBytes(filterLength);
                        using var filterStream = new MemoryStream(filterData);
                        var filter = serializer.Deserialize<DreamFilter>(filterStream);

                        Filters.Add(filter);
                    }

                    break;
                }
                case IconAppearanceProperty.Verbs: {
                    var verbsCount = buffer.ReadVariableInt32();

                    Verbs.EnsureCapacity(verbsCount);
                    for (int verbsI = 0; verbsI < verbsCount; verbsI++) {
                        Verbs.Add(buffer.ReadVariableInt32());
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
        if(appearanceId is not null)
            return appearanceId.Value;
        else
            throw new Exception("No appearance ID found in buffer");
    }

    public void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        throw new NotImplementedException();
    }
}

public enum BlendMode {
    Default,
    Overlay,
    Add,
    Subtract,
    Multiply,
    InsertOverlay
}

[Flags]
public enum AppearanceFlags {
    None = 0,
    LongGlide = 1,
    ResetColor = 2,
    ResetAlpha = 4,
    ResetTransform = 8,
    NoClientColor = 16,
    KeepTogether = 32,
    KeepApart = 64,
    PlaneMaster = 128,
    TileBound = 256,
    PixelScale = 512,
    PassMouse = 1024,
    TileMover = 2048
}

[Flags] //kinda, but only EASE_IN and EASE_OUT are used as bitflags, everything else is an enum
public enum AnimationEasing {
    Linear = 0,
    Sine = 1,
    Circular = 2,
    Cubic = 3,
    Bounce = 4,
    Elastic = 5,
    Back = 6,
    Quad = 7,
    Jump = 8,
    EaseIn = 64,
    EaseOut = 128,
}

[Flags]
public enum AnimationFlags {
    None = 0,
    AnimationEndNow = 1,
    AnimationLinearTransform = 2,
    AnimationParallel = 4,
    AnimationSlice = 8,
    AnimationRelative = 256,
    AnimationContinue = 512
}

//used for encoding for netmessages
public enum IconAppearanceProperty : byte {
        Name,
        Icon,
        IconState,
        Direction,
        DoesntInheritDirection,
        PixelOffset,
        PixelOffset2,
        Color,
        Alpha,
        GlideSize,
        ColorMatrix,
        Layer,
        Plane,
        BlendMode,
        AppearanceFlags,
        Invisibility,
        Opacity,
        Override,
        RenderSource,
        RenderTarget,
        MouseOpacity,
        Overlays,
        Underlays,
        VisContents,
        Filters,
        Verbs,
        Transform,

        Id,
        End
    }
