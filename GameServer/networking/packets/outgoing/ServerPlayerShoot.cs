using common;

namespace GameServer.networking.packets.outgoing
{
    public class ServerPlayerShoot : OutgoingMessage
    {
        public int OwnerId;
        public int ContainerType;
        public byte[] BulletIds;
        public short[] Damages;
        public DamageTypes[] DamageTypes;
        public Position StartingPos;

        public override PacketId ID => PacketId.SERVERPLAYERSHOOT;

        public override Packet CreateInstance()
        {
            return new ServerPlayerShoot();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(OwnerId);
            wtr.Write(ContainerType);
            int i;
            wtr.Write((short)BulletIds.Length);
            for (i = 0; i < BulletIds.Length; i++)
                wtr.Write(BulletIds[i]);
            wtr.Write((short)Damages.Length);
            for (i = 0; i < Damages.Length; i++)
                wtr.Write(Damages[i]);
            wtr.Write((short)DamageTypes.Length);
            for (i = 0; i < DamageTypes.Length; i++)
                wtr.Write((byte)DamageTypes[i]);
            StartingPos.Write(wtr);
        }
    }
}