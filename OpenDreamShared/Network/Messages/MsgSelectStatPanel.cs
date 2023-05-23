using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    //Client -> Server: Tell the server what stat panel the client is now looking at
    //Server -> Client: Tell the client to switch stat panels
    public sealed class MsgSelectStatPanel : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string StatPanel = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            StatPanel = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(StatPanel);
        }
    }
}
