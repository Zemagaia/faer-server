using common;

namespace GameServer.networking.packets.incoming.pets
{
    public class FetchPets : IncomingMessage
    {
        public override Packet CreateInstance() => new FetchPets();

        public override PacketId ID => PacketId.FETCH_PETS;

        protected override void Read(NReader rdr)
        {
        }

        protected override void Write(NWriter wtr)
        {
        }
    }
}