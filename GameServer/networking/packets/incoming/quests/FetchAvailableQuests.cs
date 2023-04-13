using common;

namespace GameServer.networking.packets.incoming.quests
{
    public class FetchAvailableQuests : IncomingMessage
    {
        public override Packet CreateInstance() => new FetchAvailableQuests();

        public override PacketId ID => PacketId.FETCH_AVAILABLE_QUESTS;

        protected override void Read(NReader rdr)
        {
        }

        protected override void Write(NWriter wtr)
        {
        }
    }
}