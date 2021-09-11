using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgUpdateStatPanels : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public Dictionary<string, List<string>> StatPanels;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var countTabs = buffer.ReadVariableInt32();
            StatPanels = new Dictionary<string, List<string>>(countTabs);

            for (var i = 0; i < countTabs; i++)
            {
                var title = buffer.ReadString();
                var countLines = buffer.ReadVariableInt32();
                var lines = new List<string>(countLines);

                for (var l = 0; l < countLines; l++)
                {
                    lines.Add(buffer.ReadString());
                }

                StatPanels.Add(title, lines);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(StatPanels.Count);
            foreach (var (title, lines) in StatPanels)
            {
                buffer.Write(title);
                buffer.WriteVariableInt32(lines.Count);

                foreach (var line in lines)
                {
                    buffer.Write(line);
                }
            }
        }
    }
}
