using common;
using common.resources;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using NLog;

namespace GameServer.logic.loot
{
    public struct LootDef
    {
        public LootDef(ItemData item, double probability)
        {
            Probability = probability;
            Item = item;
        }

        public readonly ItemData Item;
        public readonly double Probability;
    }

    public class Loot : List<ILootDef>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Loot(params ILootDef[] lootDefs)
        {
            //For independent loots(e.g. chests)
            AddRange(lootDefs);
        }

        private static readonly Random Rand = new();

        public IEnumerable<ItemData> GetLoots(RealmManager manager, int min, int max)
        {
            //For independent loots(e.g. chests)
            var consideration = new List<LootDef>();
            foreach (var i in this)
                i.Populate(manager, null, null, Rand, consideration);

            var retCount = Rand.Next(min, max);
            foreach (var i in consideration)
            {
                if (Rand.NextDouble() < i.Probability)
                {
                    yield return i.Item;
                    retCount--;
                }

                if (retCount == 0)
                    yield break;
            }
        }

        public static readonly ushort BrownBag = 0x0407;
        public static readonly ushort BlackBag = 0x0408;
        private static readonly ushort EggBasket = 0x0409;
        private static readonly ushort BlueBag = 0x040A;
        private static readonly ushort GreyBag = 0x040B;
        private static readonly ushort GoldenBag = 0x040C;
        private static readonly ushort RedBag = 0x040D;
        private static readonly ushort PinkBag = 0x040E;
        private static readonly ushort CyanBag = 0x040F;

        public void Handle(Enemy enemy)
        {
            if (enemy.Spawned) 
                return;

            var consideration = new List<LootDef>();
            var shared = new List<ItemData>();
            foreach (var i in this)
                i.Populate(enemy.Manager, enemy, null, Rand, consideration);

            var dats = enemy.DamageCounter.GetPlayerData();
            foreach (var i in consideration)
                if (Rand.NextDouble() < i.Probability && i.Item.Item?.Soulbound == false)
                    shared.Add(GetItemData(i, null));
            if (shared.Count > 0)
                AddBagToWorld(enemy, shared, new Player[0]);

            foreach (var dat in dats)
            {
                consideration.Clear();
                foreach (var i in this) 
                    i.Populate(enemy.Manager, enemy, dat, Rand, consideration);

                var lootDropBoost = dat.Item1.LDBoostTime > 0 ? 1.5 : 1;
                var luckStatBoost = 1 + dat.Item1.Stats[10] / 100.0;
                var globalLBoost = DateTime.UtcNow.ToUnixTimestamp() < Constants.EventEnds.ToUnixTimestamp()
                    ? Constants.GlobalLootBoost ?? 1
                    : 1;

                var playerLoot = new List<ItemData>();
                foreach (var i in consideration)
                    if (Rand.NextDouble() < i.Probability * lootDropBoost * luckStatBoost * globalLBoost)
                        playerLoot.Add(GetItemData(i, dat));
                if (playerLoot.Count > 0)
                    AddBagToWorld(enemy, playerLoot, new[] { dat.Item1 });
            }
        }

        private ItemData GetItemData(LootDef i, Tuple<Player, int> dat)
        {
            // public loot
            i.Item.ObjectType = i.Item.Item.ObjectType;
            i.Item.UIID = ItemData.MakeUIID(i.Item.ObjectType);
            // private loot since owner is not null
            /*if (dat is not null && dat.Item1 is not null)
            {
            }*/

            if (i.Item.Item.SlotType != 10 && i.Item.Item.SlotType != 26)
            {
                var quality = ItemData.MakeQuality(i.Item.Item);
                i.Item.Quality = quality;
                i.Item.Runes = ItemData.GetRuneSlots(quality);
            }

            return i.Item;
        }

        private static void AddBagToWorld(Enemy enemy, List<ItemData> items, Player[] owners)
        {
            var bag = BrownBag;
            var bagType = 0;
            var player = owners.Length > 0 ? owners[0] : null;
            for (var i = 0; i < items.Count; i++)
            {
                var type = items[i].Item?.BagType;
                if (type != null && type > bagType)
                    bagType = (int)type;
            }
            switch (bagType)
            {
                case 0:
                    bag = BrownBag;
                    break;
                case 1:
                    bag = BlackBag;
                    break;
                case 2:
                    bag = EggBasket;
                    break;
                case 3:
                    bag = BlueBag;
                    break;
                case 4:
                    bag = GreyBag;
                    break;
                case 5:
                    bag = GoldenBag;
                    player?.Client.SendPacket(new GlobalNotification()
                    {
                        Text = "legendaryLoot"
                    });
                    break;
                case 6:
                    bag = RedBag;
                    player?.Client.SendPacket(new GlobalNotification()
                    {
                        Text = "mythicLoot"
                    });
                    break;
                case 7:
                    bag = PinkBag;
                    player?.Client.SendPacket(new GlobalNotification()
                    {
                        Text = "unholyLoot"
                    });
                    break;
                case 8:
                    bag = CyanBag;
                    player?.Client.SendPacket(new GlobalNotification()
                    {
                        Text = "divineLoot"
                    });
                    break;
            }

            var container = new Container(enemy.Manager, bag, 1000 * 60, true);
            container.Inventory.SetItems(items);
            container.BagOwners = owners.Select(x => x.AccountId).ToArray();
            container.Move(
                enemy.X + (float)((Rand.NextDouble() * 2 - 1) * 0.5),
                enemy.Y + (float)((Rand.NextDouble() * 2 - 1) * 0.5));
            container.SetDefaultSize(bagType > 3 ? 110 : 80);
            enemy.Owner.EnterWorld(container);
            container.AlwaysTick = true;
        }
    }
}