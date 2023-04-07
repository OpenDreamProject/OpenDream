using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    /// <summary>
    /// Sent server -> client to tell the client to load the interface after connecting, before going in-game.
    /// </summary>
    public sealed class MsgLoadInterface : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        public string InterfaceText = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            InterfaceText = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(InterfaceText);
        }
    }
}
