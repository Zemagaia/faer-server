using common;

namespace GameServer.networking.packets.incoming
{
    public class EnemyHit : IncomingMessage
    {
        public int Time;
        public byte BulletId;
        public int TargetId;

        public override PacketId ID => PacketId.ENEMYHIT;

        public override Packet CreateInstance()
        {
            return new EnemyHit();
        }

        protected override void Read(NReader rdr)
        {
            Time = rdr.ReadInt32();
            BulletId = rdr.ReadByte();
            TargetId = rdr.ReadInt32();
        }
    }
}