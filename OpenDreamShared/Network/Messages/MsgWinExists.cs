﻿using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgWinExists : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public int PromptId;
        public string ControlId = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            PromptId = buffer.ReadVariableInt32();
            ControlId = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.WriteVariableInt32(PromptId);
            buffer.Write(ControlId);
        }
    }
}
