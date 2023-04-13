using common;
using common.resources;
using GameServer.realm.entities.player;

namespace GameServer.realm.logic.quests
{
    public partial class Quests
    {
        /// Give character a quest
        /// <param name="tier">Quest tier. Random rewards if tier is higher than -1. Use <b>overrideItems</b> for manual rewards (-1)</param>
        /// <param name="playerLevel">Player level. For handling enemies</param>
        /// <param name="difficulty">Quest difficulty. Difficult quests are more likely to have more tasks, rewards and higher maximum time</param>
        /// <param name="rewardWeight">Manual weight of rewards.</param>
        /// <param name="taskAmounts">Manual amount of tasks.</param>
        /// <param name="extraMins">Additional minutes to quest.</param>
        /// <param name="ignoreSameSlotType">Ignore items with same SlotType from pool</param>
        /// <param name="newItems">Items to add to pool (only works if tier is higher than -1)</param>
        /// <param name="items">Items to add to rewards (only works if tier is -1, ALL items are added to quest rewards)</param>
        /// <param name="hours">Fixed amount of hours for the quest</param>
        /// <param name="accountQuest">Add available quest or account quest</param>
        public void GiveQuest(int tier, int playerLevel, int difficulty, int rewardWeight = 0, int taskAmounts = 0,
            int extraMins = 0, bool ignoreSameSlotType = false, string[] newItems = null, string[] items = null,
            int hours = 0, bool accountQuest = false)
        {
            var questData = GenerateQuest(tier, playerLevel, difficulty, rewardWeight, taskAmounts, extraMins,
                ignoreSameSlotType, newItems, items, hours);

            if (questData == null) return;

            if (!accountQuest)
            {
                AddAvailableQuest(questData);
                return;
            }

            AddAccountQuest(questData, false);
        }

        public QuestData GenerateQuest(
            int tier, int playerLevel, int difficulty, int rewardWeight = 0, int taskAmounts = 0,
            int extraMins = 0, bool ignoreSameSlotType = false, string[] newItems = null, string[] items = null,
            int hours = 0)
        {
            var random = MathUtils.Random;

            // Shouldn't happen
            if (_questGiver == null)
            {
                Log.Error("Attempted to add quest while QuestGiver is null");
                return null;
            }

            int i;
            // Quest Id is set automatically!
            var questData = new QuestData
            {
                AddTime = DateTime.UtcNow.ToUnixTimestamp()
            };

            // Add dictionaries if they are non-existent and if we don't have manual loot
            if (_questGiver.TypeToUsableItems == null && tier > -1)
            {
                _questGiver.SetUsableItems(_player.ObjectType);
            }

            if (tier > -1)
            {
                _questGiver.SetEquippableTypes();
            }

            // Prefer item override (items), otherwise add items to item pool
            if (items == null)
            {
                if (newItems != null)
                {
                    _questGiver.AddToTypeDict(objectsId: newItems);
                }

                if (tier > -1)
                    switch (tier)
                    {
                        case 1:
                            foreach (var type2UsableItems in _questGiver.TypeToUsableItems)
                                if (type2UsableItems.Value.Tier > 4)
                                    _questGiver.RemoveFromTypeDict(type2UsableItems.Value.ObjectId);
                            // _questGiver.AddToTypeDict(objectsId: _questGiver.Content.TierOneLoot);
                            break;
                        case 2:
                        case 3:
                            foreach (var type2UsableItems in _questGiver.TypeToUsableItems)
                                if (type2UsableItems.Value.Tier > 6 || type2UsableItems.Value.Tier < tier)
                                    _questGiver.RemoveFromTypeDict(type2UsableItems.Value.ObjectId);
                            // _questGiver.AddToTypeDict(objectsId: _questGiver.Content.TierTwoLoot);
                            break;
                        case 4:
                        case 5:
                            foreach (var type2UsableItems in _questGiver.TypeToUsableItems)
                                if (type2UsableItems.Value.Tier > 7 || type2UsableItems.Value.Tier < tier)
                                    _questGiver.RemoveFromTypeDict(type2UsableItems.Value.ObjectId);
                            // _questGiver.AddToTypeDict(objectsId: QuestGiverContent.TierFourLoot);
                            break;
                        default:
                            foreach (var type2UsableItems in _questGiver.TypeToUsableItems)
                                if (type2UsableItems.Value.Tier > 8 || type2UsableItems.Value.Tier < tier)
                                    _questGiver.RemoveFromTypeDict(type2UsableItems.Value.ObjectId);
                            // _questGiver.AddToTypeDict(objectsId: _questGiver.Content.TierSixLoot);
                            break;
                    }

                var weightRoll = random.Next(Math.Max(1, difficulty / 3), difficulty);
                var rewards = new List<ushort>();
                var slotType = new List<int>();
                i = 0;
                while (i < (rewardWeight <= 0 ? weightRoll : rewardWeight))
                {
                    var rndId2EqObjType = _questGiver.IdToEquippableObjectTypes.RandomElement(random);
                    foreach (var type2UsableItems in _questGiver.TypeToUsableItems)
                    {
                        if (i >= (rewardWeight <= 0 ? weightRoll : rewardWeight))
                            break;

                        if (type2UsableItems.Value.ObjectType == rndId2EqObjType.Value &&
                            !slotType.Contains(type2UsableItems.Value.SlotType))
                        {
                            i++;
                            if (type2UsableItems.Value.Untiered)
                                i++;
                            rewards.Add(rndId2EqObjType.Value);
                            if (ignoreSameSlotType)
                                slotType.Add(type2UsableItems.Value.SlotType);
                        }

                        _questGiver.RemoveFromTypeDict(rndId2EqObjType.Key);
                        if (!ignoreSameSlotType)
                            break;
                    }
                }

                questData.Rewards = rewards.ToArray();
            }

            var gameData = _player.Manager.Resources.GameData;
            if (items != null)
            {
                questData.Rewards = new ushort[items.Length];
                for (i = 0; i < items.Length; i++)
                {
                    questData.Rewards[i] = gameData.IdToObjectType[items[i]];
                }
            }

            var difficultyRoll = random.Next(Math.Max(1, difficulty / 3), difficulty);
            var hasExpQuest = false;

            questData.Icon = 0;
            var slay = new List<ushort>();
            var dungeons = new List<string>();
            var deliveries = new List<ushort>();
            var deliveryDatas = new List<ItemData>();

            var questRng = new List<int>();

            i = 0;
            while (i < (taskAmounts <= 0 ? difficultyRoll : taskAmounts))
            {
                var rng = random.Next(hasExpQuest ? 1 : 0, 2);
                var rng2 = random.Next(0, 3);
                switch (rng)
                {
                    case 1: /*// Slay
                        questData.Icon = questData.Icon < 2 ? 1 : questData.Icon;
                        questRng.Add(rng2);
                        var enemy = gameData.IdToObjectType[_questGiver.GetEnemy(playerLevel)];
                        if (rng2 == 2)
                            enemy = gameData.IdToObjectType[_questGiver.GetQuestEnemy(playerLevel)];

                        if (!slay.Contains(enemy))
                        {
                            slay.Add(enemy);
                            i++;
                        }

                        break;
                    case 2: // Dungeon
                        questData.Icon = questData.Icon < 4 ? 3 : questData.Icon;
                        var dungeon = _questGiver.GetDungeon(playerLevel);
                        if (!dungeons.Contains(dungeon))
                        {
                            dungeons.Add(dungeon);
                            i++;
                        }

                        break;
                    case 3: // Delivery
                        questData.Icon = questData.Icon < 5 ? (byte)4 : questData.Icon;
                        var itemId = _questGiver.GetItem(playerLevel);
                        var objType = gameData.IdToObjectType[itemId];
                        if (!deliveries.Contains(objType))
                        {
                            deliveries.Add(objType);
                            deliveryDatas.Add(new ItemData()
                            {
                                MaxQuantity = random.Next(Math.Max(1, difficulty / 2), difficulty)
                            });
                            i++;
                        }

                        break;
                    default:*/ // Experience
                        questData.Experience = random.Next(Player.GetExpGoal2(playerLevel) / 3,
                            Player.GetExpGoal2(playerLevel));
                        if (playerLevel >= 5)
                        {
                            questData.Experience = Math.Max(1556, // level 4 goal
                                random.Next(Player.GetExpGoal2(playerLevel) / 7,
                                    Player.GetExpGoal2(playerLevel) / 3));
                        }

                        questData.ExpReward = random.Next((int)(questData.Experience * 0.4),
                            (int)(questData.Experience * 0.7));
                        hasExpQuest = true;
                        i++;
                        break;
                }
            }

            if (dungeons.Count > 0)
                questData.Dungeons = dungeons.ToArray();
            if (slay.Count > 0)
                questData.Slay = slay.ToArray();

            questData.Title = _questGiver.GetTitle(difficulty, questData.Icon);

            if (questData.Dungeons != null)
            {
                questData.DungeonAmounts = new int[questData.Dungeons.Length];
                for (i = 0; i < questData.Dungeons.Length; i++)
                    questData.DungeonAmounts[i] =
                        Math.Max(1, random.Next(difficulty / 3, difficulty + (tier <= -1 ? 5 : tier)));
            }

            if (questData.Slay != null)
            {
                questData.SlayAmounts = new int[questData.Slay.Length];
                for (i = 0; i < questData.Slay.Length; i++)
                {
                    questData.SlayAmounts[i] =
                        Math.Max(1, random.Next(difficulty / 2, difficulty + (tier <= -1 ? 5 : tier)));
                    if (questRng[i] != 2) 
                        questData.SlayAmounts[i] *= random.Next(difficulty / 2, difficulty);
                }
            }

            if (deliveries.Count > 0)
            {
                questData.Deliver = deliveries.ToArray();
                questData.DeliverDatas = deliveryDatas.ToArray();
            }

            questData.EndTime = DateTime.UtcNow
                .AddMinutes((tier <= -1 ? 30 : tier * 5) + random.Next(difficulty * 3, difficulty * 6) +
                            extraMins)
                .ToUnixTimestamp();
            // how daily quest is handled for now
            if (hours != 0)
            {
                questData.Experience *= 5;
                questData.ExpReward *= 5;
                questData.Id = int.Parse(DateTime.UtcNow.ToShortDateString().Replace("/", ""));
                questData.EndTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day)
                    .AddHours(hours).ToUnixTimestamp();
            }

            return questData;
        }
    }
}