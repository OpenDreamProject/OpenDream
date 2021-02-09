using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    class IconAppearance {
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
            Transform
        }

        public static Dictionary<string, UInt32> Colors = new() {
            { "black", 0x000000FF },
            { "silver", 0xC0C0C0FF },
            { "gray", 0x808080FF },
            { "grey", 0x808080FF },
            { "white", 0xFFFFFFFF },
            { "maroon", 0x800000FF },
            { "red", 0xFF0000FF },
            { "purple", 0x800080FF },
            { "fuschia", 0xFF00FFFF },
            { "magenta", 0xFF00FFFF },
            { "green", 0x00C000FF },
            { "lime", 0x00FF00FF },
            { "olive", 0x808000FF },
            { "gold", 0x808000FF },
            { "yellow", 0xFFFF00FF },
            { "navy", 0x000080FF },
            { "blue", 0x0000FFFF },
            { "teal", 0x008080FF },
            { "aqua", 0x00FFFFFF },
            { "cyan", 0x00FFFFFF }
        };

        public string Icon;
        public string IconState;
        public AtomDirection Direction;
        public int PixelX, PixelY;
        public UInt32 Color = 0xFFFFFFFF;
        public float Layer;
        public int Invisibility;
        public List<int> Overlays = new();
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
            
            foreach (int overlay in appearance.Overlays) {
                Overlays.Add(overlay);
            }

            for (int i = 0; i < 6; i++) {
                Transform[i] = appearance.Transform[i];
            }
        }

        public override bool Equals(object obj) {
            IconAppearance appearance = obj as IconAppearance;
            if (appearance == null) return false;

            if (appearance.Icon != Icon) return false;
            if (appearance.IconState != IconState) return false;
            if (appearance.Direction != Direction) return false;
            if (appearance.PixelX != PixelX) return false;
            if (appearance.PixelY != PixelY) return false;
            if (appearance.Color != Color) return false;
            if (appearance.Layer != Layer) return false;
            if (appearance.Invisibility != Invisibility) return false;
            if (appearance.Overlays.Count != Overlays.Count) return false;

            for (int i = 0; i < Overlays.Count; i++) {
                if (appearance.Overlays[i] != Overlays[i]) return false;
            }

            for (int i = 0; i < 6; i++) {
                if (appearance.Transform[i] != Transform[i]) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            int hashCode = (Icon + IconState).GetHashCode();
            hashCode += Direction.GetHashCode();
            hashCode += PixelX.GetHashCode();
            hashCode += PixelY.GetHashCode();
            hashCode += Color.GetHashCode();
            hashCode += Layer.GetHashCode();
            hashCode += Invisibility.GetHashCode();

            foreach (int overlay in Overlays) {
                hashCode += overlay.GetHashCode();
            }

            for (int i = 0; i < 6; i++) {
                hashCode += Transform[i].GetHashCode();
            }

            return hashCode;
        }

        public void SetColor(string color) {
            if (color.StartsWith("#")) {
                color = color.Substring(1);

                if (color.Length == 3 || color.Length == 4) { //4-bit color; repeat each digit
                    string alphaComponent = (color.Length == 4) ? new string(color[3], 2) : "ff";

                    color = new string(color[0], 2) + new string(color[1], 2) + new string(color[2], 2) + alphaComponent;
                } else if (color.Length == 6) { //Missing alpha
                    color += "ff";
                }

                Color = Convert.ToUInt32(color, 16);
            } else if (!Colors.TryGetValue(color.ToLower(), out Color)) {
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

            if (Overlays.Count > 0) {
                packetStream.WriteByte((byte)AppearanceProperty.Overlays);
                packetStream.WriteByte((byte)Overlays.Count);

                foreach (int overlay in Overlays) {
                    packetStream.WriteUInt32((UInt32)overlay);
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
                    case AppearanceProperty.Overlays: {
                        int overlayCount = packetStream.ReadByte();

                        for (int i = 0; i < overlayCount; i++) {
                            appearance.Overlays.Add((int)packetStream.ReadUInt32());
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
