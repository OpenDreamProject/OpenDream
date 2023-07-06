using System.Collections.Generic;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgUpdateStatPanels : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public readonly Dictionary<string, List<(string Name, string Value, string? AtomRef)>> StatPanels;

        public MsgUpdateStatPanels(Dictionary<string, List<(string, string, string?)>> statPanels) {
            StatPanels = statPanels;
        }

        [UsedImplicitly]
        public MsgUpdateStatPanels() {
            StatPanels = new();
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            var countTabs = buffer.ReadVariableInt32();
            StatPanels.EnsureCapacity(countTabs);

            for (var i = 0; i < countTabs; i++) {
                var title = buffer.ReadString();
                var countLines = buffer.ReadVariableInt32();
                var lines = new List<(string, string, string?)>(countLines);

                for (var l = 0; l < countLines; l++) {
                    var value = (buffer.ReadString(), buffer.ReadString(), buffer.ReadBoolean() ? buffer.ReadString() : null);

                    lines.Add(value);
                }

                StatPanels.Add(title, lines);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.WriteVariableInt32(StatPanels.Count);
            foreach (var (title, lines) in StatPanels) {
                buffer.Write(title);
                buffer.WriteVariableInt32(lines.Count);

                foreach (var line in lines) {
                    buffer.Write(line.Name);
                    buffer.Write(line.Value);

                    buffer.Write(line.AtomRef != null);
                    if (line.AtomRef != null)
                        buffer.Write(line.AtomRef);
                }
            }
        }
    }
}
