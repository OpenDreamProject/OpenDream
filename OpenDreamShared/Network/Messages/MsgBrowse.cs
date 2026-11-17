using System;
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgBrowse : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public bool Display;
        public string? FileName;
        public string? Window;
        public Dictionary<string, string>? Options;
        public byte[]? Data;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            Display = buffer.ReadBoolean();
            var hasFileName = buffer.ReadBoolean();
            var hasWindow = buffer.ReadBoolean();
            var hasOptions = buffer.ReadBoolean();
            buffer.ReadPadBits();

            if (hasFileName)
                FileName = buffer.ReadString();
            if (hasWindow)
                Window = buffer.ReadString();
            if (hasOptions)
                Options = buffer.ReadString().Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('='))
                    .ToDictionary(split => split[0], split => split[1]);

            var dataLength = buffer.ReadVariableInt32();
            if(dataLength > -1) {
                Data = buffer.ReadBytes(dataLength);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Display);
            buffer.Write(FileName != null);
            buffer.Write(Window != null);
            buffer.Write(Options?.Count > 0);
            buffer.WritePadBits();

            if (FileName != null)
                buffer.Write(FileName);
            if (Window != null)
                buffer.Write(Window);
            if (Options?.Count > 0)
                buffer.Write(string.Join(';', Options.Select(keyval => $"{keyval.Key}={keyval.Value}")));

            buffer.WriteVariableInt32(Data?.Length ?? -1);
            if(Data is not null) {
                buffer.Write(Data);
            }
        }
    }
}
