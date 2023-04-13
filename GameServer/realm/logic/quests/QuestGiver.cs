using common.resources;
using GameServer.realm.entities.player;

namespace GameServer.realm.logic.quests
{
    public class QuestGiver
    {
        internal QuestGiverContent Content { get; private set; }
        public Dictionary<string, ushort> IdToEquippableObjectTypes { get; private set; }
        public Dictionary<ushort, Item> TypeToUsableItems { get; private set; }

        private Player _player;

        public QuestGiver(Player player)
        {
            _player = player;
            Content = new QuestGiverContent();
        }

        public void SetEquippableTypes()
        {
            var id2EqObjType = new Dictionary<string, ushort>();
            if (TypeToUsableItems == null) return;

            foreach (var @item in TypeToUsableItems.Values)
                if (@item.Tier > 0 && @item.SlotType != 10)
                    id2EqObjType.Add(@item.ObjectId, @item.ObjectType);

            IdToEquippableObjectTypes = new Dictionary<string, ushort>(id2EqObjType);
        }

        public void SetUsableItems(ushort objectType)
        {
            var type2UsableItems = new Dictionary<ushort, Item>();
            
            var gameData = _player.Manager.Resources.GameData;
            gameData.Classes.TryGetValue(objectType, out var @classValue);
            for (var i = 0; i < 6; i++)
                foreach (var @itemValue in gameData.Items.Values)
                    if (@itemValue.SlotType == @classValue.SlotTypes[i] || @itemValue.SlotType == 10 &&
                        !type2UsableItems.ContainsKey(@itemValue.ObjectType))
                        type2UsableItems.Add(@itemValue.ObjectType, @itemValue);

            TypeToUsableItems = new Dictionary<ushort, Item>(type2UsableItems);
        }

        ///<summary>
        /// Remove item from Usable Items Directory if it's available
        /// <p></p>
        /// <p><b>objectsId</b> is preferred over <b>objectId</b></p>
        ///</summary>
        public void RemoveFromTypeDict(string objectId = null, string[] objectsId = null)
        {
            // IdToEquippableObjectTypes has 32 items by default (8 tiers * 4 slottypes)
            if (objectId != null)
            {
                IdToEquippableObjectTypes.Remove(objectId);
            }
            else if (objectsId != null)
                for (var i = 0; i < objectsId.Length; i++)
                    IdToEquippableObjectTypes.Remove(objectsId[i]);
        }

        ///<summary>
        /// Add item from Usable Items Directory if it's available
        /// <p></p>
        /// <p><b>objectsId</b> is preferred over <b>objectId</b></p>
        ///</summary>
        public void AddToTypeDict(string objectId = null, string[] objectsId = null)
        {
            var gameData = _player.Manager.Resources.GameData;
            var id2ObjType = gameData.IdToObjectType;
            Item item;
            if (objectId != null)
            {
                if (!TypeToUsableItems.TryGetValue(id2ObjType[objectId], out item))
                {
                    return;
                }

                if (!IdToEquippableObjectTypes.ContainsKey(item.ObjectId))
                    IdToEquippableObjectTypes.Add(item.ObjectId, item.ObjectType);

                return;
            }

            if (objectsId != null)
            {
                for (var i = 0; i < objectsId.Length; i++)
                {
                    if (!TypeToUsableItems.TryGetValue(id2ObjType[objectsId[i]], out item))
                    {
                        continue;
                    }

                    if (!IdToEquippableObjectTypes.ContainsKey(item.ObjectId))
                        IdToEquippableObjectTypes.Add(item.ObjectId, item.ObjectType);
                }
            }
        }

        public string GetItem(int playerLevel)
        {
            var delivery = playerLevel switch
            {
                < 25 and > 10 => Content.MediumDeliveries[_player.Random.Next(Content.MediumDeliveries.Length)],
                > 25 => Content.HardDeliveries[_player.Random.Next(Content.HardDeliveries.Length)],
                _ => Content.EasyDeliveries[_player.Random.Next(Content.EasyDeliveries.Length)]
            };
            return delivery;
        }

        public string GetDungeon(int playerLevel)
        {
            var dungeon = playerLevel switch
            {
                < 25 and > 10 => Content.MediumDungeons[_player.Random.Next(Content.MediumDungeons.Length)],
                > 25 => Content.HardDungeons[_player.Random.Next(Content.HardDungeons.Length)],
                _ => Content.EasyDungeons[_player.Random.Next(Content.EasyDungeons.Length)]
            };
            return dungeon;
        }

        public string GetEnemy(int playerLevel)
        {
            var enemy = playerLevel switch
            {
                > 10 and < 15 => Content.EasyEnemies[_player.Random.Next(Content.EasyEnemies.Length)],
                > 15 and < 25 => Content.MediumEnemies[_player.Random.Next(Content.MediumEnemies.Length)],
                > 25 => Content.HardEnemies[_player.Random.Next(Content.HardEnemies.Length)],
                _ => Content.VeryEasyEnemies[_player.Random.Next(Content.VeryEasyEnemies.Length)]
            };
            return enemy;
        }

        public string GetQuestEnemy(int playerLevel)
        {
            var questEnemy = playerLevel switch
            {
                > 10 and < 15 => Content.EasyQuests[_player.Random.Next(Content.EasyQuests.Length)],
                > 15 and < 25 => Content.MediumQuests[_player.Random.Next(Content.MediumQuests.Length)],
                > 25 => Content.HardQuests[_player.Random.Next(Content.HardQuests.Length)],
                _ => Content.VeryEasyQuests[_player.Random.Next(Content.VeryEasyQuests.Length)]
            };
            return questEnemy;
        }

        public string GetTitle(int difficulty, int icon)
        {
            // WIP: implement icon (different titles for delivery quests [icon 4])
            var title = difficulty switch
            {
                >= 0 and < 3 => Content.EasyTitles[_player.Random.Next(Content.EasyTitles.Length)],
                >= 3 and < 6 => Content.MediumTitles[_player.Random.Next(Content.MediumTitles.Length)],
                >= 6 and < 10 => Content.HardTitles[_player.Random.Next(Content.HardTitles.Length)],
                _ => Content.ExtremeTitles[_player.Random.Next(Content.ExtremeTitles.Length)]
            };

            return title;
        }
    }
}