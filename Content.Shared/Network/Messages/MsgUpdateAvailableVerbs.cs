using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    public sealed class MsgUpdateAvailableVerbs : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string[] AvailableVerbs;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var count = buffer.ReadVariableInt32();
            var verbs = new string[count];

            for (var i = 0; i < count; i++)
            {
                verbs[i] = buffer.ReadString();
            }

            AvailableVerbs = verbs;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(AvailableVerbs.Length);

            foreach (var verb in AvailableVerbs)
            {
                buffer.Write(verb);
            }
        }
    }
}
