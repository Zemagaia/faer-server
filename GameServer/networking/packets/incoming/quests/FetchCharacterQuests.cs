using common;

namespace GameServer.networking.packets.incoming.quests
{
    public class FetchCharacterQuests : IncomingMessage
    {
        public override Packet CreateInstance() => new FetchCharacterQuests();

        public override PacketId ID => PacketId.FETCH_CHARACTER_QUESTS;

        protected override void Read(NReader rdr)
        {
        }

        protected override void Write(NWriter wtr)
        {
        }
    }
}