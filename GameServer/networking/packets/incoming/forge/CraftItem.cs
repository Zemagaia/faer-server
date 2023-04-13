using common;

namespace GameServer.networking.packets.incoming.forge
{
    public class CraftItem : IncomingMessage
    {
        public override Packet CreateInstance() => new CraftItem();

        public override PacketId ID => PacketId.CRAFT_ITEM;

        public int Id;
        public int ItemSlot;
        public int RuneSlot;
        public byte[] Slots;

        protected override void Read(NReader rdr)
        {
            Id = rdr.ReadInt32();
            ItemSlot = rdr.ReadInt32();
            RuneSlot = rdr.ReadInt32();
            Slots = new byte[rdr.ReadByte()];
            for (var i = 0; i < Slots.Length; i++)
            {
                Slots[i] = rdr.ReadByte();
            }
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Id);
            wtr.Write(ItemSlot);
            wtr.Write(RuneSlot);
            wtr.Write((byte)Slots.Length);
            for (var i = 0; i < Slots.Length; i++)
            {
                wtr.Write(Slots[i]);
            }
        }
    }
}