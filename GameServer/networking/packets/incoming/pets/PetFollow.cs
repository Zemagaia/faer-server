using common;

namespace GameServer.networking.packets.incoming.pets
{
    public class PetFollow : IncomingMessage
    {
        public PetData PetData;
        
        public override Packet CreateInstance() => new PetFollow();

        public override PacketId ID => PacketId.PET_FOLLOW;

        protected override void Read(NReader rdr)
        {
            PetData = this.PetData.Read(rdr);
        }
    }
}