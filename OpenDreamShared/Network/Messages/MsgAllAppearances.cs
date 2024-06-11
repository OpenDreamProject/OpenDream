using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgAllAppearances(Dictionary<int, IconAppearance> allAppearances) : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    private enum Property : byte {
        Icon,
        IconState,
        Direction,
        DoesntInheritDirection,
        PixelOffset,
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

    public Dictionary<int, IconAppearance> AllAppearances = allAppearances;

    public MsgAllAppearances() : this(new()) { }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        var count = buffer.ReadInt32();
        var appearanceId = -1;

        AllAppearances = new(count);

        for (int i = 0; i < count; i++) {
            var appearance = new IconAppearance();
            var property = (Property)buffer.ReadByte();

            appearanceId++;

            while (property != Property.End) {
                switch (property) {
                    case Property.Id:
                        appearanceId = buffer.ReadVariableInt32();
                        break;
                    case Property.Icon:
                        appearance.Icon = buffer.ReadVariableInt32();
                        break;
                    case Property.IconState:
                        appearance.IconState = buffer.ReadString();
                        break;
                    case Property.Direction:
                        appearance.Direction = (AtomDirection)buffer.ReadByte();
                        break;
                    case Property.DoesntInheritDirection:
                        appearance.InheritsDirection = false;
                        break;
                    case Property.PixelOffset:
                        appearance.PixelOffset = (buffer.ReadVariableInt32(), buffer.ReadVariableInt32());
                        break;
                    case Property.Color:
                        appearance.Color = buffer.ReadColor();
                        break;
                    case Property.Alpha:
                        appearance.Alpha = buffer.ReadByte();
                        break;
                    case Property.GlideSize:
                        appearance.GlideSize = buffer.ReadFloat();
                        break;
                    case Property.Layer:
                        appearance.Layer = buffer.ReadFloat();
                        break;
                    case Property.Plane:
                        appearance.Plane = buffer.ReadVariableInt32();
                        break;
                    case Property.BlendMode:
                        appearance.BlendMode = (BlendMode)buffer.ReadByte();
                        break;
                    case Property.AppearanceFlags:
                        appearance.AppearanceFlags = (AppearanceFlags)buffer.ReadInt32();
                        break;
                    case Property.Invisibility:
                        appearance.Invisibility = buffer.ReadSByte();
                        break;
                    case Property.Opacity:
                        appearance.Opacity = buffer.ReadBoolean();
                        break;
                    case Property.Override:
                        appearance.Override = buffer.ReadBoolean();
                        break;
                    case Property.RenderSource:
                        appearance.RenderSource = buffer.ReadString();
                        break;
                    case Property.RenderTarget:
                        appearance.RenderTarget = buffer.ReadString();
                        break;
                    case Property.MouseOpacity:
                        appearance.MouseOpacity = (MouseOpacity)buffer.ReadByte();
                        break;
                    case Property.ColorMatrix:
                        appearance.ColorMatrix = new(
                            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(),
                            buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()
                        );

                        break;
                    case Property.Overlays: {
                        var overlaysCount = buffer.ReadVariableInt32();

                        appearance.Overlays.EnsureCapacity(overlaysCount);
                        for (int overlaysI = 0; overlaysI < overlaysCount; overlaysI++) {
                            appearance.Overlays.Add(buffer.ReadVariableInt32());
                        }

                        break;
                    }
                    case Property.Underlays: {
                        var underlaysCount = buffer.ReadVariableInt32();

                        appearance.Underlays.EnsureCapacity(underlaysCount);
                        for (int underlaysI = 0; underlaysI < underlaysCount; underlaysI++) {
                            appearance.Underlays.Add(buffer.ReadVariableInt32());
                        }

                        break;
                    }
                    case Property.VisContents: {
                        var visContentsCount = buffer.ReadVariableInt32();

                        appearance.VisContents.EnsureCapacity(visContentsCount);
                        for (int visContentsI = 0; visContentsI < visContentsCount; visContentsI++) {
                            appearance.VisContents.Add(buffer.ReadNetEntity());
                        }

                        break;
                    }
                    case Property.Filters: {
                        var filtersCount = buffer.ReadInt32();

                        appearance.Filters.EnsureCapacity(filtersCount);
                        for (int filtersI = 0; filtersI < filtersCount; filtersI++) {
                            var filterLength = buffer.ReadVariableInt32();
                            var filterData = buffer.ReadBytes(filterLength);
                            using var filterStream = new MemoryStream(filterData);
                            var filter = serializer.Deserialize<DreamFilter>(filterStream);

                            appearance.Filters.Add(filter);
                        }

                        break;
                    }
                    case Property.Verbs: {
                        var verbsCount = buffer.ReadVariableInt32();

                        appearance.Verbs.EnsureCapacity(verbsCount);
                        for (int verbsI = 0; verbsI < verbsCount; verbsI++) {
                            appearance.Verbs.Add(buffer.ReadVariableInt32());
                        }

                        break;
                    }
                    case Property.Transform: {
                        appearance.Transform = [
                            buffer.ReadSingle(), buffer.ReadSingle(),
                            buffer.ReadSingle(), buffer.ReadSingle(),
                            buffer.ReadSingle(), buffer.ReadSingle()
                        ];

                        break;
                    }
                    default:
                        throw new Exception($"Invalid property {property}");
                }

                property = (Property)buffer.ReadByte();
            }

            AllAppearances.Add(appearanceId, appearance);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        int lastId = -1;

        buffer.Write(AllAppearances.Count);
        foreach (var pair in AllAppearances) {
            var appearance = pair.Value;

            if (pair.Key != lastId + 1) {
                buffer.Write((byte)Property.Id);
                buffer.WriteVariableInt32(pair.Key);
            }

            lastId = pair.Key;

            if (appearance.Icon != null) {
                buffer.Write((byte)Property.Icon);
                buffer.WriteVariableInt32(appearance.Icon.Value);
            }

            if (appearance.IconState != null) {
                buffer.Write((byte)Property.IconState);
                buffer.Write(appearance.IconState);
            }

            if (appearance.Direction != IconAppearance.Default.Direction) {
                buffer.Write((byte)Property.Direction);
                buffer.Write((byte)appearance.Direction);
            }

            if (appearance.InheritsDirection != true) {
                buffer.Write((byte)Property.DoesntInheritDirection);
            }

            if (appearance.PixelOffset != IconAppearance.Default.PixelOffset) {
                buffer.Write((byte)Property.PixelOffset);
                buffer.WriteVariableInt32(appearance.PixelOffset.X);
                buffer.WriteVariableInt32(appearance.PixelOffset.Y);
            }

            if (appearance.Color != IconAppearance.Default.Color) {
                buffer.Write((byte)Property.Color);
                buffer.Write(appearance.Color);
            }

            if (appearance.Alpha != IconAppearance.Default.Alpha) {
                buffer.Write((byte)Property.Alpha);
                buffer.Write(appearance.Alpha);
            }

            if (!appearance.GlideSize.Equals(IconAppearance.Default.GlideSize)) {
                buffer.Write((byte)Property.GlideSize);
                buffer.Write(appearance.GlideSize);
            }

            if (!appearance.ColorMatrix.Equals(IconAppearance.Default.ColorMatrix)) {
                buffer.Write((byte)Property.ColorMatrix);

                foreach (var value in appearance.ColorMatrix.GetValues())
                    buffer.Write(value);
            }

            if (!appearance.Layer.Equals(IconAppearance.Default.Layer)) {
                buffer.Write((byte)Property.Layer);
                buffer.Write(appearance.Layer);
            }

            if (appearance.Plane != IconAppearance.Default.Plane) {
                buffer.Write((byte)Property.Plane);
                buffer.WriteVariableInt32(appearance.Plane);
            }

            if (appearance.BlendMode != IconAppearance.Default.BlendMode) {
                buffer.Write((byte)Property.BlendMode);
                buffer.Write((byte)appearance.BlendMode);
            }

            if (appearance.AppearanceFlags != IconAppearance.Default.AppearanceFlags) {
                buffer.Write((byte)Property.AppearanceFlags);
                buffer.Write((int)appearance.AppearanceFlags);
            }

            if (appearance.Invisibility != IconAppearance.Default.Invisibility) {
                buffer.Write((byte)Property.Invisibility);
                buffer.Write(appearance.Invisibility);
            }

            if (appearance.Opacity != IconAppearance.Default.Opacity) {
                buffer.Write((byte)Property.Opacity);
                buffer.Write(appearance.Opacity);
            }

            if (appearance.Override != IconAppearance.Default.Override) {
                buffer.Write((byte)Property.Override);
                buffer.Write(appearance.Override);
            }

            if (!string.IsNullOrWhiteSpace(appearance.RenderSource)) {
                buffer.Write((byte)Property.RenderSource);
                buffer.Write(appearance.RenderSource);
            }

            if (!string.IsNullOrWhiteSpace(appearance.RenderTarget)) {
                buffer.Write((byte)Property.RenderTarget);
                buffer.Write(appearance.RenderTarget);
            }

            if (appearance.MouseOpacity != IconAppearance.Default.MouseOpacity) {
                buffer.Write((byte)Property.MouseOpacity);
                buffer.Write((byte)appearance.MouseOpacity);
            }

            if (appearance.Overlays.Count != 0) {
                buffer.Write((byte)Property.Overlays);

                buffer.WriteVariableInt32(appearance.Overlays.Count);
                foreach (var overlay in appearance.Overlays) {
                    buffer.WriteVariableInt32(overlay);
                }
            }

            if (appearance.Underlays.Count != 0) {
                buffer.Write((byte)Property.Underlays);

                buffer.WriteVariableInt32(appearance.Underlays.Count);
                foreach (var underlay in appearance.Underlays) {
                    buffer.WriteVariableInt32(underlay);
                }
            }

            if (appearance.VisContents.Count != 0) {
                buffer.Write((byte)Property.VisContents);

                buffer.WriteVariableInt32(appearance.VisContents.Count);
                foreach (var item in appearance.VisContents) {
                    buffer.Write(item);
                }
            }

            if (appearance.Filters.Count != 0) {
                buffer.Write((byte)Property.Filters);

                buffer.Write(appearance.Filters.Count);
                foreach (var filter in appearance.Filters) {
                    using var filterStream = new MemoryStream();

                    serializer.Serialize(filterStream, filter);
                    buffer.WriteVariableInt32((int)filterStream.Length);
                    filterStream.TryGetBuffer(out var filterBuffer);
                    buffer.Write(filterBuffer);
                }
            }

            if (appearance.Verbs.Count != 0) {
                buffer.Write((byte)Property.Verbs);

                buffer.WriteVariableInt32(appearance.Verbs.Count);
                foreach (var verb in appearance.Verbs) {
                    buffer.WriteVariableInt32(verb);
                }
            }

            if (!appearance.Transform.SequenceEqual(IconAppearance.Default.Transform)) {
                buffer.Write((byte)Property.Transform);

                for (int i = 0; i < 6; i++) {
                    buffer.Write(appearance.Transform[i]);
                }
            }

            buffer.Write((byte)Property.End);
        }
    }
}
