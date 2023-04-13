using common;

namespace GameServer.networking.packets.incoming.skillTree.abilities
{
    public class OffensiveAbility : IncomingMessage
    {
        public override Packet CreateInstance() => new OffensiveAbility();

        public override PacketId ID => PacketId.OFFENSIVE_ABILITY;

        public int Time;
        public Position UsePos;
        public float Angle;

        protected override void Read(NReader rdr)
        {
            Time = rdr.ReadInt32();
            UsePos = Position.Read(rdr);
            Angle = rdr.ReadSingle();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Time);
            UsePos.Write(wtr);
            wtr.Write(Angle);
        }
    }
}