using System;

namespace OpenDreamShared.Net.Packets {
    public class PacketAlert : IPacket {
        public PacketID PacketID => PacketID.Alert;

        public int PromptId;
        public string Title;
        public string Message;
        public string Button1, Button2, Button3;

        public PacketAlert() { }

        public PacketAlert(int promptId, String title, String message, String button1, String button2, String button3) {
            PromptId = promptId;
            Title = title;
            Message = message;
            Button1 = button1;
            Button2 = button2;
            Button3 = button3;
        }

        public void ReadFromStream(PacketStream stream) {
            PromptId = stream.ReadUInt16();
            Title = stream.ReadString();
            Message = stream.ReadString();
            Button1 = stream.ReadString();
            Button2 = stream.ReadString();
            Button3 = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt16((UInt16)PromptId);
            stream.WriteString(Title);
            stream.WriteString(Message);
            stream.WriteString(Button1);
            stream.WriteString(Button2);
            stream.WriteString(Button3);
        }
    }
}
