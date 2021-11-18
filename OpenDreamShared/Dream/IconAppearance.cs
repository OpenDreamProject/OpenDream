using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    public class IconAppearance : IEquatable<IconAppearance> {
        public enum AppearanceProperty {
            End,
            Icon,
            IconState,
            Direction,
            PixelX,
            PixelY,
            Color,
            Layer,
            Invisibility,
            Overlays,
            Underlays,
            Transform,
            MouseOpacity
        }

        public string Icon;
        public string IconState;
        public AtomDirection Direction;
        public int PixelX, PixelY;
        public UInt32 Color = 0xFFFFFFFF;
        public float Layer;
        public int Invisibility;
        public MouseOpacity MouseOpacity = MouseOpacity.PixelOpaque;
        public List<int> Overlays = new();
        public List<int> Underlays = new();
        public float[] Transform = new float[6] {   1, 0,
                                                    0, 1,
                                                    0, 0 };

        public IconAppearance() { }

        public IconAppearance(IconAppearance appearance) {
            Icon = appearance.Icon;
            IconState = appearance.IconState;
            Direction = appearance.Direction;
            PixelX = appearance.PixelX;
            PixelY = appearance.PixelY;
            Color = appearance.Color;
            Layer = appearance.Layer;
            Invisibility = appearance.Invisibility;
            MouseOpacity = appearance.MouseOpacity;
            Overlays = new List<int>(appearance.Overlays);
            Underlays = new List<int>(appearance.Underlays);

            for (int i = 0; i < 6; i++) {
                Transform[i] = appearance.Transform[i];
            }
        }

        public override bool Equals(object obj) => obj is IconAppearance appearance && Equals(appearance);

        public bool Equals(IconAppearance appearance) {
            if (appearance == null) return false;

            if (appearance.Icon != Icon) return false;
            if (appearance.IconState != IconState) return false;
            if (appearance.Direction != Direction) return false;
            if (appearance.PixelX != PixelX) return false;
            if (appearance.PixelY != PixelY) return false;
            if (appearance.Color != Color) return false;
            if (appearance.Layer != Layer) return false;
            if (appearance.Invisibility != Invisibility) return false;
            if (appearance.MouseOpacity != MouseOpacity) return false;
            if (appearance.Overlays.Count != Overlays.Count) return false;

            for (int i = 0; i < Overlays.Count; i++) {
                if (appearance.Overlays[i] != Overlays[i]) return false;
            }

            for (int i = 0; i < Underlays.Count; i++) {
                if (appearance.Underlays[i] != Underlays[i]) return false;
            }

            for (int i = 0; i < 6; i++) {
                if (appearance.Transform[i] != Transform[i]) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            int hashCode = (Icon + IconState).GetHashCode();
            hashCode += Direction.GetHashCode();
            hashCode += PixelX;
            hashCode += PixelY;
            hashCode += Color.GetHashCode();
            hashCode += Layer.GetHashCode();
            hashCode += Invisibility;
            hashCode += MouseOpacity.GetHashCode();

            foreach (int overlay in Overlays) {
                hashCode += overlay;
            }

            foreach (int underlay in Underlays) {
                hashCode += underlay;
            }

            for (int i = 0; i < 6; i++) {
                hashCode += Transform[i].GetHashCode();
            }

            return hashCode;
        }

        public void SetColor(string color) {
            if (color.StartsWith("#")) {
                color = ColorHelpers.ParseHexColor(color);

                Color = Convert.ToUInt32(color, 16);
            } else if (!ColorHelpers.Colors.TryGetValue(color.ToLower(), out Color)) {
                throw new ArgumentException("Invalid color '" + color + "'");
            }
        }

        public void WriteToPacket(PacketStream packetStream) {
            if (Icon != default) {
                packetStream.WriteByte((byte)AppearanceProperty.Icon);
                packetStream.WriteString(Icon);
            }

            if (IconState != default) {
                packetStream.WriteByte((byte)AppearanceProperty.IconState);
                packetStream.WriteString(IconState);
            }

            if (Direction != default) {
                packetStream.WriteByte((byte)AppearanceProperty.Direction);
                packetStream.WriteByte((byte)Direction);
            }

            if (PixelX != default) {
                packetStream.WriteByte((byte)AppearanceProperty.PixelX);
                packetStream.WriteInt16((Int16)PixelX);
            }

            if (PixelY != default) {
                packetStream.WriteByte((byte)AppearanceProperty.PixelY);
                packetStream.WriteInt16((Int16)PixelY);
            }

            if (Color != default) {
                packetStream.WriteByte((byte)AppearanceProperty.Color);
                packetStream.WriteUInt32(Color);
            }

            if (Layer != default) {
                packetStream.WriteByte((byte)AppearanceProperty.Layer);
                packetStream.WriteFloat(Layer);
            }

            if (Invisibility != default) {
                packetStream.WriteByte((byte)AppearanceProperty.Invisibility);
                packetStream.WriteByte((byte)Invisibility);
            }

            if (MouseOpacity != MouseOpacity.PixelOpaque) {
                packetStream.WriteByte((byte)AppearanceProperty.MouseOpacity);
                packetStream.WriteByte((byte)MouseOpacity);
            }

            if (Overlays.Count > 0) {
                packetStream.WriteByte((byte)AppearanceProperty.Overlays);
                packetStream.WriteByte((byte)Overlays.Count);

                foreach (int overlay in Overlays) {
                    packetStream.WriteUInt32((UInt32)overlay);
                }
            }

            if (Underlays.Count > 0) {
                packetStream.WriteByte((byte)AppearanceProperty.Underlays);
                packetStream.WriteByte((byte)Underlays.Count);

                foreach (int underlay in Underlays) {
                    packetStream.WriteUInt32((UInt32)underlay);
                }
            }

            if (!IsTransformIdentity()) {
                packetStream.WriteByte((byte)AppearanceProperty.Transform);
                for (int i = 0; i < 6; i++) {
                    packetStream.WriteFloat(Transform[i]);
                }
            }

            packetStream.WriteByte((byte)AppearanceProperty.End);
        }

        public static IconAppearance ReadFromPacket(PacketStream packetStream) {
            IconAppearance appearance = new IconAppearance();

            AppearanceProperty property = (AppearanceProperty)packetStream.ReadByte();
            while (property != AppearanceProperty.End) {
                switch (property) {
                    case AppearanceProperty.Icon: appearance.Icon = packetStream.ReadString(); break;
                    case AppearanceProperty.IconState: appearance.IconState = packetStream.ReadString(); break;
                    case AppearanceProperty.Direction: appearance.Direction = (AtomDirection)packetStream.ReadByte(); break;
                    case AppearanceProperty.PixelX: appearance.PixelX = packetStream.ReadInt16(); break;
                    case AppearanceProperty.PixelY: appearance.PixelY = packetStream.ReadInt16(); break;
                    case AppearanceProperty.Color: appearance.Color = packetStream.ReadUInt32(); break;
                    case AppearanceProperty.Layer: appearance.Layer = packetStream.ReadFloat(); break;
                    case AppearanceProperty.Invisibility: appearance.Invisibility = packetStream.ReadByte(); break;
                    case AppearanceProperty.MouseOpacity: appearance.MouseOpacity = (MouseOpacity)packetStream.ReadByte(); break;
                    case AppearanceProperty.Overlays: {
                        int overlayCount = packetStream.ReadByte();

                        for (int i = 0; i < overlayCount; i++) {
                            appearance.Overlays.Add((int)packetStream.ReadUInt32());
                        }

                        break;
                    }
                    case AppearanceProperty.Underlays: {
                        int underlayCount = packetStream.ReadByte();

                        for (int i = 0; i < underlayCount; i++) {
                            appearance.Underlays.Add((int)packetStream.ReadUInt32());
                        }

                        break;
                    }
                    case AppearanceProperty.Transform: {
                        for (int i = 0; i < 6; i++) {
                            appearance.Transform[i] = packetStream.ReadFloat();
                        }

                        break;
                    }
                    default: throw new Exception("Invalid appearnce property '" + property + "'");
                }

                property = (AppearanceProperty)packetStream.ReadByte();
            }

            return appearance;
        }

        private bool IsTransformIdentity() {
            if (Transform[0] != 1.0f) return false;
            if (Transform[1] != 0.0f) return false;
            if (Transform[2] != 0.0f) return false;
            if (Transform[3] != 1.0f) return false;
            if (Transform[4] != 0.0f) return false;
            if (Transform[5] != 0.0f) return false;

            return true;
        }
    }
}
