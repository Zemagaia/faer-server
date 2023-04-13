using common;

namespace GameServer.networking.packets.outgoing.pets
{
    public class FetchPetsResult : OutgoingMessage
    {
        public override Packet CreateInstance() => new FetchPetsResult();

        public override PacketId ID => PacketId.FETCH_PETS_RESULT;

        public PetData[] PetDatas;
        public string Description;

        protected override void Write(NWriter wtr)
        {
            wtr.Write(PetDatas.ToBytes(false));
            wtr.WriteUTF(Description);
        }
    }
}