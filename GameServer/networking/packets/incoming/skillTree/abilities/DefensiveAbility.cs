using common;

namespace GameServer.networking.packets.incoming.skillTree.abilities
{
    public class DefensiveAbility : IncomingMessage
    {
        public override Packet CreateInstance() => new DefensiveAbility();

        public override PacketId ID => PacketId.DEFENSIVE_ABILITY;

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