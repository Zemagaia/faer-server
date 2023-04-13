using common;

namespace GameServer.networking.packets.outgoing.quests
{
    public class DeliverItemsResult : OutgoingMessage
    {
        public override Packet CreateInstance() => new DeliverItemsResult();

        public override PacketId ID => PacketId.DELIVER_ITEMS_RESULT;

        public bool[] Results;

        protected override void Write(NWriter wtr)
        {
            wtr.Write((short)Results.Length);
            for (int i = 0; i < Results.Length; i++)
                wtr.Write(Results[i]);
        }
    }
}