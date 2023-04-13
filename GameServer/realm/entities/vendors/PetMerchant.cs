using common;
using GameServer.realm.entities.player;

namespace GameServer.realm.entities.vendors
{
    class PetMerchant : SellableObject
    {
        private readonly int[] _types = new int[] { 0x0451, 0x0452, 0x0453, 0x0454 };
        private readonly int[] _prices = new int[] { 1000, 2000, 5000, 10000 };
        private readonly int[] _levels = new int[] { 2, 3, 4, 5 };

        private readonly int _upgradeLevel;

        public PetMerchant(RealmManager manager, ushort objType) : base(manager, objType)
        {
            Currency = CurrencyType.Gold;
            Price = int.MaxValue; // just in case for some reason _type isn't found
            for (var i = 0; i < _types.Length; i++)
            {
                if (objType != _types[i])
                    continue;

                Price = _prices[i];
                _upgradeLevel = _levels[i];
            }
        }

        public override void Buy(Player player)
        {
            var account = player.Client.Account;
            
            if (account.Credits < Price)
            {
                player.Client.SendPacket(new networking.packets.outgoing.BuyResult
                {
                    ResultString = "Not enough gold",
                    Result = 9
                });
                return;
            }

            // change yard level
            if (!player.Manager.Database.ChangeYardLevel(account, _upgradeLevel))
            {
                player.SendError("Internal server error.");
                return;
            }

            player.Manager.Database.UpdateCredit(account, -Price);
            player.Client.SendPacket(new networking.packets.outgoing.BuyResult
            {
                ResultString = "Upgrade successful! Please leave the Pet Yard to have it upgraded.",
                Result = 0
            });
        }
    }
}