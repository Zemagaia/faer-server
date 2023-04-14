using System.Xml.Linq;
using common;
using common.resources;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using NLog;
using Newtonsoft.Json;

namespace GameServer.logic.loot
{
    public interface ILootDef
    {
        void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat,
            Random rand, IList<LootDef> lootDefs);
    }

    internal class MostDamagers : ILootDef
    {
        private readonly ILootDef[] _loots;
        private readonly int _amount;

        public MostDamagers(XElement e, ILootDef[] loots)
        {
            _amount = e.ParseInt("@amount");
            _loots = loots;
        }
        
        public MostDamagers(int amount, params ILootDef[] loots)
        {
            _amount = amount;
            _loots = loots;
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat, Random rand,
            IList<LootDef> lootDefs)
        {
            var data = enemy.DamageCounter.GetPlayerData();
            var mostDamage = GetMostDamage(data);
            foreach (var loot in mostDamage.Where(pl => pl.Equals(playerDat)).SelectMany(_ => _loots))
            {
                if (loot is QuestLoot || loot is GoldDrop)
                {
                    loot.Populate(manager, enemy, playerDat, rand, lootDefs);
                    continue;
                }

                loot.Populate(manager, enemy, null, rand, lootDefs);
            }
        }

        private IEnumerable<Tuple<Player, int>> GetMostDamage(IEnumerable<Tuple<Player, int>> data)
        {
            var enumerable = data.ToList();
            var damages = enumerable.Select(_ => _.Item2).ToList();
            var len = damages.Count < _amount ? damages.Count : _amount;
            for (var i = 0; i < len; i++)
            {
                var val = damages.Max();
                yield return enumerable.FirstOrDefault(_ => _.Item2 == val);
                damages.Remove(val);
            }
        }
    }

    public class OnlyOne : ILootDef
    {
        private readonly ILootDef[] _loots;

        public OnlyOne(XElement e, ILootDef[] loots)
        {
            _loots = loots;
        }
        
        public OnlyOne(params ILootDef[] loots)
        {
            _loots = loots;
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat, Random rand,
            IList<LootDef> lootDefs)
        {
            _loots[rand.Next(0, _loots.Length)].Populate(manager, enemy, playerDat, rand, lootDefs);
        }
    }

    public class Threshold : ILootDef
    {
        private readonly double _threshold;
        private readonly ILootDef[] _children;

        
        public Threshold(XElement e, ILootDef[] loots)
        {
            _threshold = e.ParseFloat("@amount");
            _children = loots;
        }
        
        public Threshold(double threshold, params ILootDef[] children)
        {
            _threshold = threshold;
            _children = children;
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat,
            Random rand, IList<LootDef> lootDefs)
        {
            if (playerDat == null || playerDat.Item2 / (double)enemy.MaximumHP <= _threshold)
            {
                return;
            }

            foreach (var i in _children)
            {
                if (i is QuestLoot || i is GoldDrop)
                {
                    i.Populate(manager, enemy, playerDat, rand, lootDefs);
                    continue;
                }

                i.Populate(manager, enemy, null, rand, lootDefs);
            }
        }
    }

    public enum LItemType
    {
        Weapon,
        Ability,
        Armor,
        Ring,
        Potion
    }

    public class TierLoot : ILootDef
    {
        public static readonly int[] WeaponT = { 1, 2, 3, 8, 17, 24 };

        // old: 20 trap, 21 orb, 22 prism
        public static readonly int[] AbilityT = { 4, 5, 11, 12, 13, 15, 16, 18, 19, 23, 27, 28, 29, 30 };
        public static readonly int[] ArmorT = { 6, 7, 14 };
        public static readonly int[] RingT = { 9 };
        public static readonly int[] PotionT = { 10 };
        public static readonly int[] ArtifactT = { 31 };
        public static readonly int[] CharmT = { 32 };

        private readonly byte _tier;
        private readonly int[] _types;
        private readonly double _probability;

        public TierLoot(XElement e)
        {
            _tier = (byte)e.ParseInt("@tier");
            switch (e.ParseString("@type"))
            {
                case "Weapon":
                    _types = WeaponT;
                    break;
                case "Ability":
                    _types = AbilityT;
                    break;
                case "Armor":
                    _types = ArmorT;
                    break;
                case "Ring":
                    _types = RingT;
                    break;
                case "Potion":
                    _types = PotionT;
                    break;
                default:
                    throw new NotSupportedException(e.ParseString("@type"));
            }
            
            _probability = e.ParseFloat("@probability");
        }
        
        public TierLoot(byte tier, ItemType type, double probability)
        {
            _tier = tier;
            switch (type)
            {
                case ItemType.Weapon:
                    _types = WeaponT;
                    break;
                case ItemType.Ability:
                    _types = AbilityT;
                    break;
                case ItemType.Armor:
                    _types = ArmorT;
                    break;
                case ItemType.Ring:
                    _types = RingT;
                    break;
                case ItemType.Potion:
                    _types = PotionT;
                    break;
                default:
                    throw new NotSupportedException(type.ToString());
            }

            _probability = probability;
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat,
            Random rand, IList<LootDef> lootDefs)
        {
            if (playerDat != null) return;
            var candidates = manager.Resources.GameData.Items
                .Where(item => Array.IndexOf(_types, item.Value.SlotType) != -1 
                               && item.Value.Tier == _tier)
                .Select(item => item.Value)
                .ToArray();
            foreach (var i in candidates)
                lootDefs.Add(new LootDef(ItemData.GenerateData(i), _probability / candidates.Length));
        }
    }

    public class ItemLoot : ILootDef
    {
        protected static readonly Logger Log = LogManager.GetLogger("ItemLoot");
        private readonly string _item;
        private readonly double _probability;
        private readonly string _itemData;

        public ItemLoot(XElement e)
        {
            _item = e.ParseString("@item");
            _probability = e.ParseFloat("@probability");
            _itemData = e.ParseString("@itemData");
        }
        
        public ItemLoot(string item, double probability, string itemData = null)
        {
            _item = item;
            _probability = probability;
            _itemData = itemData;

            // Send items for the gameserver to log any non-existent items on BehaviorDb startup.
            BehaviorDb.SendItem(item);
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat,
            Random rand, IList<LootDef> lootDefs)
        {
            if (playerDat != null) return;
            var dat = manager.Resources.GameData;
            ItemData fData;
            try
            {
                fData = JsonConvert.DeserializeObject<ItemData>(_itemData);
            }
            catch
            {
                fData = new ItemData();
            }

            var objType = dat.IdToObjectType[_item];
            if (dat.IdToObjectType.ContainsKey(_item)
                && dat.Items.ContainsKey(objType))
            {
                fData.Item = dat.Items[objType];
                fData.ObjectType = objType;
                fData.UIID = ItemData.MakeUIID(objType);
                lootDefs.Add(new LootDef(fData, _probability));
            }
        }
    }

    public class QuestLoot : ILootDef
    {
        protected static readonly Logger Log = LogManager.GetLogger("QuestLoot");
        private readonly string _item;
        private readonly double _probability;

        public QuestLoot(XElement e)
        {
            _item = e.ParseString("@item");
            _probability = e.ParseFloat("@probability");
        }
        
        public QuestLoot(string item, double probability)
        {
            _item = item;
            _probability = probability;

            // Send items for the gameserver to log any non-existent items on BehaviorDb startup.
            BehaviorDb.SendItem(item);
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat,
            Random rand, IList<LootDef> lootDefs)
        {
            if (playerDat == null)
            {
                return;
            }

            var dat = manager.Resources.GameData;
            if (!dat.IdToObjectType.ContainsKey(_item) || !dat.Items.ContainsKey(dat.IdToObjectType[_item]))
            {
                return;
            }

            var fData = new ItemData();
            var player = playerDat.Item1;
            int i;
            int j;

            var quests = player.Client.Account.AccountQuests;
            for (i = 0; i < quests.Length; i++)
            for (j = 0; j < quests[i].DeliverDatas.Length; j++)
            {
                // make sure that the items haven't already been delivered
                if (quests[i].Delivered[j])
                {
                    continue;
                }

                var data = quests[i].DeliverDatas[j];
                if (data.MaxQuantity == 0)
                {
                    continue;
                }

                fData.Quantity = 1;
                fData.MaxQuantity = data.MaxQuantity;
                var objType = dat.IdToObjectType[_item];
                fData.Item = dat.Items[objType];
                fData.ObjectType = objType;
                fData.UIID = ItemData.MakeUIID(objType);
                // if account quest has already assigned a quantity
                // add loot to defs and don't check char quests
                lootDefs.Add(new LootDef(fData, _probability));
                return;
            }

            quests = player.CharacterQuests;
            for (i = 0; i < quests.Length; i++)
            for (j = 0; j < quests[i].DeliverDatas.Length; j++)
            {
                // make sure that the items haven't already been delivered
                if (quests[i].Delivered[j])
                {
                    continue;
                }

                var data = quests[i].DeliverDatas[j];
                if (data.MaxQuantity == 0)
                {
                    continue;
                }

                fData.Quantity = 1;
                fData.MaxQuantity = data.MaxQuantity;
                var objType = dat.IdToObjectType[_item];
                fData.Item = dat.Items[objType];
                fData.ObjectType = objType;
                fData.UIID = ItemData.MakeUIID(objType);
                lootDefs.Add(new LootDef(fData, _probability));
                return;
            }
        }
    }
    public class GoldDrop : ILootDef
    {
        private readonly int _min;
        private readonly int _max;
        private readonly double _probability;

        public GoldDrop(XElement e)
        {
            _min = e.ParseInt("@min");
            var max = e.ParseInt("@max");
            _max = max <= _min ? _min : max;
            _probability = e.ParseFloat("@probability");
        }
        
        public GoldDrop(int min, int max = 0, double probability = 1)
        {
            _min = min;
            _max = max <= 0 ? min : max;
            _probability = probability;
        }

        public void Populate(RealmManager manager, Enemy enemy, Tuple<Player, int> playerDat,
            Random rand, IList<LootDef> lootDefs)
        {
            if (playerDat == null)
                return;
            if (_probability < MathUtils.NextDouble())
                return;
            var player = playerDat.Item1;
            player.Client.Account.Credits = player.Credits += MathUtils.Next(_min, _max);
            player.Client.SendShowEffect(EffectType.Flow, player.Id, enemy.X, enemy.Y, enemy.X, enemy.Y, 0xFFFFFF00);
        }
    }

    public static class LootTemplates
    {
        public static ILootDef StatPots()
        {
            return
                new OnlyOne(
                    new ItemLoot("Potion of Armor", 1),
                    new ItemLoot("Potion of Strength", 1),
                    new ItemLoot("Potion of Agility", 1),
                    new ItemLoot("Potion of Stamina", 1),
                    new ItemLoot("Potion of Intelligence", 1),
                    new ItemLoot("Potion of Dexterity", 1)
                );
        }

        public static ILootDef GreaterPots()
        {
            return
                new OnlyOne(
                    new ItemLoot("Greater Potion of Armor", 1),
                    new ItemLoot("Greater Potion of Strength", 1),
                    new ItemLoot("Greater Potion of Agility", 1),
                    new ItemLoot("Greater Potion of Stamina", 1),
                    new ItemLoot("Greater Potion of Intelligence", 1),
                    new ItemLoot("Greater Potion of Dexterity", 1),
                    new ItemLoot("Greater Potion of Life", 1),
                    new ItemLoot("Greater Potion of Mana", 1)
                );
        }

        public static ILootDef AllPots()
        {
            return
                new OnlyOne(
                    new ItemLoot("Potion of Armor", 1),
                    new ItemLoot("Potion of Strength", 1),
                    new ItemLoot("Potion of Agility", 1),
                    new ItemLoot("Potion of Stamina", 1),
                    new ItemLoot("Potion of Intelligence", 1),
                    new ItemLoot("Potion of Dexterity", 1),
                    new ItemLoot("Potion of Life", 1),
                    new ItemLoot("Potion of Mana", 1),
                    new ItemLoot("Greater Potion of Armor", 1),
                    new ItemLoot("Greater Potion of Strength", 1),
                    new ItemLoot("Greater Potion of Agility", 1),
                    new ItemLoot("Greater Potion of Stamina", 1),
                    new ItemLoot("Greater Potion of Intelligence", 1),
                    new ItemLoot("Greater Potion of Dexterity", 1),
                    new ItemLoot("Greater Potion of Life", 1),
                    new ItemLoot("Greater Potion of Mana", 1)
                );
        }
    }
    
    public static class Thresholds
    {
        public const double Legendary = 0.02;
        public const double Mythic = 0.05;
        public const double Special = 0.08; // Divine and Unholy
    }
}