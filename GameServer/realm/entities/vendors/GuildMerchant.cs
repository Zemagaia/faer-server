using common;
using GameServer.realm.entities.player;

namespace GameServer.realm.entities.vendors
{
    class GuildMerchant : SellableObject
    {
        private readonly int[] _hallTypes = new int[] { 0x043A, 0x043B, 0x043C };
        private readonly int[] _hallPrices = new int[] { 10000, 100000, 250000 };
        private readonly int[] _hallLevels = new int[] { 1, 2, 3 };

        private readonly int _upgradeLevel;

        public GuildMerchant(RealmManager manager, ushort objType) : base(manager, objType)
        {
            Currency = CurrencyType.Fame;
            Price = Int32.MaxValue; // just in case for some reason _hallType isn't found
            for (int i = 0; i < _hallTypes.Length; i++)
            {
                if (objType != _hallTypes[i])
                    continue;

                Price = _hallPrices[i];
                _upgradeLevel = _hallLevels[i];
            }
        }

        public override void Buy(Player player)
        {
            var account = player.Manager.Database.GetAccount(player.AccountId);
            var guild = player.Manager.Database.GetGuild(account.GuildId);


            if (guild.IsNull || account.GuildRank < 30)
            {
                player.SendError("Verification failed.");
                return;
            }

            if (guild.Fame < Price)
            {
                player.Client.SendBuyResult(9, $"Not enough Guild Fame!");
                return;
            }

            // change guild level
            if (!player.Manager.Database.ChangeGuildLevel(guild, _upgradeLevel))
            {
                player.SendError("Internal server error.");
                return;
            }

            player.Manager.Database.UpdateGuildFame(guild, -Price);
            player.Client.SendBuyResult(0, $"Upgrade successful! Please leave the Guild Hall to have it upgraded.");
        }
    }
}