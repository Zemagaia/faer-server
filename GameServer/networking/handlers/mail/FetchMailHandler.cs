using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.mail;
using GameServer.networking.packets.outgoing.mail;

namespace GameServer.networking.handlers.mail
{
    class FetchMailHandler : PacketHandlerBase<FetchMail>
    {
        public override PacketId ID => PacketId.FETCH_MAIL;

        protected override void HandlePacket(Client client, FetchMail packet)
        {
            client.Manager.Logic.AddPendingAction(t =>
            {
                if (client.Player == null || IsTest(client))
                {
                    return;
                }

                var mails = client.Account.AccountMails.ToArray();
                for (var i = 0; i < mails.Length; i++)
                    if (mails[i].CharacterId != -1 && mails[i].CharacterId != client.Character.CharId)
                        mails[i] = new AccountMail();

                if (mails.Length == 0)
                {
                    client.SendPacket(new FetchMailResult
                    {
                        Results = new AccountMail[0],
                        Description = $"No Mail"
                    });
                    return;
                }

                client.SendPacket(new FetchMailResult
                {
                    Results = mails,
                    Description = ""
                });
            });
        }
    }
}