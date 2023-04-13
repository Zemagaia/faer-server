using common;

namespace GameServer.networking.packets.incoming.mail
{
    public class FetchMail : IncomingMessage
    {
        public override Packet CreateInstance() => new FetchMail();

        public override PacketId ID => PacketId.FETCH_MAIL;

        protected override void Read(NReader rdr)
        {
        }

        protected override void Write(NWriter wtr)
        {
        }
    }
}