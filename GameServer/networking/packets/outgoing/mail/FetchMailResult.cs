using common;

namespace GameServer.networking.packets.outgoing.mail
{
    public class FetchMailResult : OutgoingMessage
    {
        public override Packet CreateInstance() => new FetchMailResult();

        public override PacketId ID => PacketId.FETCH_MAIL_RESULT;

        public AccountMail[] Results;
        public string Description;

        protected override void Read(NReader rdr)
        {
            Results = new AccountMail[rdr.ReadInt16()];
            for (var i = 0; i < Results.Length; i++)
            {
                Results[i] = AccountMail.Read(rdr);
            }

            Description = rdr.ReadUTF();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write((short)Results.Length);
            for (int i = 0; i < Results.Length; i++)
            {
                Results[i].Write(wtr);
            }

            wtr.WriteUTF(Description);
        }
    }
}