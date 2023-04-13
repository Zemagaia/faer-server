using common;

namespace GameServer.networking.packets.outgoing
{
    public class CurrentTime : OutgoingMessage
    {
        public int Hour { get; set; }

        public override PacketId ID => PacketId.CURRENT_TIME;

        public override Packet CreateInstance()
        {
            return new CurrentTime();
        }

        protected override void Read(NReader rdr)
        {
            Hour = rdr.ReadInt32();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Hour);
        }
    }
}