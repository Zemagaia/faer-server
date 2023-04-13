using common;

namespace GameServer.networking.packets.incoming
{
    public class CancelTrade : IncomingMessage
    {
        public override PacketId ID => PacketId.CANCELTRADE;

        public override Packet CreateInstance()
        {
            return new CancelTrade();
        }

        protected override void Read(NReader rdr)
        {
        }

        protected override void Write(NWriter wtr)
        {
        }
    }
}