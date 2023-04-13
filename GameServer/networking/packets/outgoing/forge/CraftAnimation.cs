using common;

namespace GameServer.networking.packets.outgoing.forge
{
    public class CraftAnimation : OutgoingMessage
    {
        public override Packet CreateInstance() => new CraftAnimation();

        public override PacketId ID => PacketId.CRAFT_ANIMATION;

        public int ObjectType;
        public int[] Active;

        protected override void Read(NReader rdr)
        {
            ObjectType = rdr.ReadInt32();
            Active = new int[rdr.ReadInt16()];
            for (var i = 0; i < Active.Length; i++)
            {
                Active[i] = rdr.ReadInt32();
            }
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(ObjectType);
            wtr.Write((short)Active.Length);
            for (var i = 0; i < Active.Length; i++)
            {
                wtr.Write(Active[i]);
            }
        }
    }
}