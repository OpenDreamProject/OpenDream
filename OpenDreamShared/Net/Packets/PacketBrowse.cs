using System;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    class PacketBrowse : IPacket {
        public PacketID PacketID => PacketID.Browse;

        public string Window;
        public string HtmlSource;
        public Size Size;

        public PacketBrowse() { }

        public PacketBrowse(string window, string htmlSource) {
            Window = window;
            HtmlSource = htmlSource;
        }

        public void ReadFromStream(PacketStream stream) {
            Window = stream.ReadString();
            if (Window == String.Empty) Window = null;

            bool hasHtmlSource = stream.ReadBool();
            if (hasHtmlSource) HtmlSource = stream.ReadString();
            else HtmlSource = null;

            Size = new Size(stream.ReadUInt16(), stream.ReadUInt16());
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString((Window != null) ? Window : String.Empty);

            stream.WriteBool(HtmlSource != null);
            if (HtmlSource != null) stream.WriteString(HtmlSource);

            stream.WriteUInt16((UInt16)Size.Width);
            stream.WriteUInt16((UInt16)Size.Height);
        }
    }
}
