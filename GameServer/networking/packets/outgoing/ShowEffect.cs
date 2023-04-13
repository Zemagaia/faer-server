using common;

namespace GameServer.networking.packets.outgoing
{
    public class ShowEffect : OutgoingMessage
    {
        public EffectType EffectType { get; set; }
        public int TargetObjectId { get; set; }
        public Position Pos1 { get; set; }
        public Position Pos2 { get; set; }
        public ARGB Color { get; set; }
        public double Duration { get; set; }

        public override PacketId ID => PacketId.SHOWEFFECT;

        public override Packet CreateInstance()
        {
            return new ShowEffect();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write((byte)EffectType);
            wtr.Write(TargetObjectId);
            Pos1.Write(wtr);
            Pos2.Write(wtr);
            Color.Write(wtr);
            wtr.Write(Duration);
        }
    }
}