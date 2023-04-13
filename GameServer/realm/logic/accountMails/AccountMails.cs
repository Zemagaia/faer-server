using common;
using GameServer.networking;
using GameServer.networking.packets.outgoing;

namespace GameServer.realm.logic.accountMails
{
    public class AccountMails
    {
        private Client _client;
        private int _elapsed;

        public AccountMails(Client client)
        {
            _client = client;
            _elapsed = 10000;
        }

        public void Tick(RealmTime time)
        {
            _elapsed += time.ElapsedMsDelta;
            if (_elapsed > 10000)
            {
                _elapsed = 0;
                if (_client.Account.AccountMails == null)
                {
                    return;
                }

                for (var i = 0; i < _client.Account.AccountMails.Count; i++)
                    if (DateTime.UtcNow.ToUnixTimestamp() > _client.Account.AccountMails[i].EndTime)
                    {
                        var accMails = _client.Account.AccountMails;
                        accMails.RemoveAt(i);
                        _client.Account.AccountMails = accMails;
                    }
            }
        }

        /// <summary>
        /// Add an account mail and increment account hash field <b>nextMailId</b> on db
        /// <p>Note: <b>id</b> is set automatically</p>
        /// </summary>
        /// <param name="mail">Mail to add</param>
        public void Add(AccountMail mail)
        {
            var finalMail = mail;
            if (mail.Id == 0)
                finalMail.Id = _client.Manager.Database.GetAccountHashField(_client.Account, "nextMailId");
            if (finalMail.EndTime == 0)
                finalMail.EndTime = DateTime.UtcNow.AddMinutes(finalMail.Priority * 10).ToUnixTimestamp();
            var accMails = _client.Account.AccountMails;
            accMails.Add(finalMail);
            _client.Account.AccountMails = accMails;
            var parsedMails = _client.Account.AccountMails
                .Where(m => m.CharacterId == -1 || m.CharacterId == _client.Character.CharId);
            _client.SendPacket(new GlobalNotification
            {
                Text = $"Mail ({parsedMails.Count()})"
            });
            if (mail.Id == 0)
                _client.Manager.Database.IncrementHashField("account." + _client.Player.AccountId, "nextMailId");
        }

        public bool HasMail(int id)
        {
            if (_client.Account.AccountMails.Exists(x => x.Id == id))
                return true;

            return false;
        }
    }
}