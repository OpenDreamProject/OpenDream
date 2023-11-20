using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgCommand : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Command = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            Command = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Command);
        }
    }

    public sealed class MsgCommandRepeatStart : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Command = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            Command = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Command);
        }
    }

    public sealed class MsgCommandRepeatStop : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Command = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            Command = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Command);
        }
    }
}
