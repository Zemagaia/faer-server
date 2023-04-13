using common;

namespace GameServer.networking.packets.incoming.quests
{
    public class FetchAccountQuests : IncomingMessage
    {
        public override Packet CreateInstance() => new FetchAccountQuests();

        public override PacketId ID => PacketId.FETCH_ACCOUNT_QUESTS;

        protected override void Read(NReader rdr)
        {
        }

        protected override void Write(NWriter wtr)
        {
        }
    }
}