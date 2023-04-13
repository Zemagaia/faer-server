using common;

namespace GameServer.networking.packets.incoming
{
    public class UseItem : IncomingMessage
    {
        public int Time { get; set; }
        public ObjectSlot SlotObject { get; set; }
        public Position ItemUsePos { get; set; }
        public byte ActivateId { get; set; }

        public override PacketId ID => PacketId.USEITEM;

        public override Packet CreateInstance()
        {
            return new UseItem();
        }

        protected override void Read(NReader rdr)
        {
            Time = rdr.ReadInt32();
            SlotObject = ObjectSlot.Read(rdr);
            ItemUsePos = Position.Read(rdr);
            ActivateId = rdr.ReadByte();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Time);
            SlotObject.Write(wtr);
            ItemUsePos.Write(wtr);
            wtr.Write(ActivateId);
        }
    }
}