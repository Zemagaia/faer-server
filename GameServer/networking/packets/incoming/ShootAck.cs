using common;

namespace GameServer.networking.packets.incoming
{
    public class ShootAck : IncomingMessage
    {
        public int Time;

        public override PacketId ID => PacketId.SHOOTACK;

        public override Packet CreateInstance()
        {
            return new ShootAck();
        }

        protected override void Read(NReader rdr)
        {
            Time = rdr.ReadInt32();
        }
    }
}