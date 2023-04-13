using common;
using common.resources;
using GameServer.realm.entities.player;
using NLog;

namespace GameServer.realm.logic.quests
{
    public partial class Quests
    {
        private Player _player;
        private int _elapsed;
        private QuestGiver _questGiver;
        private int _giverTimer;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Quests(Player player)
        {
            _player = player;
            _elapsed = 10000;
        }

        public void Tick(RealmTime time)
        {
            _elapsed += time.ElapsedMsDelta;
            if (_elapsed > 10000)
            {
                _elapsed = 0;
                int i;
                var count = 0;
                var quests = _player.CharacterQuests;
                if (quests != null && quests.Length > 0)
                    for (i = 0; i < quests.Length; i++)
                    {
                        if (quests[i].Goals[0] >= quests[i].Goals[1])
                        {
                            HandleQuestRewards(quests[i]);
                            continue;
                        }

                        HandleCharacterQuestRemoval(i, ref count);
                    }

                var availableQuests = _player.AvailableQuests;
                if (availableQuests != null && availableQuests.Length > 0)
                    for (i = 0; i < availableQuests.Length; i++)
                        HandleAvailableQuestRemoval(i, ref count);

                quests = _player.Client.Account.AccountQuests;
                if (quests != null && quests.Length > 0)
                    for (i = 0; i < quests.Length; i++)
                    {
                        if (quests[i].Goals[0] >= quests[i].Goals[1])
                        {
                            HandleQuestRewards(quests[i], true);
                            continue;
                        }

                        HandleAccountQuestRemoval(i, ref count);
                    }

                if (count > 0)
                    _player.SendInfo($"New mail{(count != 1 ? "s" : "")} ({count})");
            }
        }

        private void HandleAvailableQuestRemoval(int i, ref int count)
        {
            var quests = _player.AvailableQuests;
            if (i >= quests.Length)
            {
                return;
            }

            var quest = quests[i];
            if (AddQuestMail($"Available quest <b>\"{quest.Title}\"</b> (Id: {quest.Id}) has expired.", quest.EndTime,
                1, ref count))
            {
                RemoveAvailableQuest(quest.Id);
            }
        }

        private void HandleCharacterQuestRemoval(int i, ref int count)
        {
            var quests = _player.CharacterQuests;
            if (i >= quests.Length)
            {
                return;
            }

            var quest = quests[i];
            if (AddQuestMail($"Character quest <b>\"{quest.Title}\"</b> (Id: {quest.Id}) has expired.", quest.EndTime,
                1, ref count))
            {
                RemoveCharacterQuest(quest.Id);
            }
        }

        private void HandleAccountQuestRemoval(int i, ref int count)
        {
            var quests = _player.Client.Account.AccountQuests;
            if (i >= quests.Length)
            {
                return;
            }

            var quest = quests[i];
            if (AddQuestMail($"Account quest <b>\"{quest.Title}\"</b> (Id: {quest.Id}) has expired.", quest.EndTime, 3,
                ref count))
            {
                RemoveAccountQuest(quest.Id, quest.DailyQuest);
            }
        }

        private bool AddQuestMail(string mailContent, int endTime, int priority, ref int count)
        {
            if (DateTime.UtcNow.ToUnixTimestamp() > endTime)
            {
                count++;
                var mail = new AccountMail()
                {
                    AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                    CharacterId = -1,
                    Content = mailContent,
                    Priority = priority
                };
                _player.Mails.Add(mail);
                return true;
            }

            return false;
        }

        public void QuestGiverTick(RealmTime time)
        {
            _giverTimer += time.ElapsedMsDelta;
            // 10s tick
            if (_giverTimer < 10000)
                return;
            _giverTimer -= 10000;
            
            if (MathUtils.NextDouble() < 0.005)
            {
                GiveQuest(1, _player.Level, _player.Level > 50 ? 5 : 3, ignoreSameSlotType: true,
                    extraMins: MathUtils.Next(5, 30),
                    newItems: new[] { "XP Booster 20 min" });
            }
        }

        /// <summary>
        /// Give out rewards and remove the character quest if the player has completed it
        /// </summary>
        public void HandleQuestRewards(AcceptedQuestData questData, bool accQuest = false)
        {
            if (questData == null) return;

            if (questData.Goals[0] >= questData.Goals[1] && questData.EndTime > DateTime.UtcNow.ToUnixTimestamp())
            {
                GiveGifts(questData);

                if (questData.ExpReward > 0)
                    _player.EnemyKilled(null, questData.ExpReward, false);

                var mail = new AccountMail()
                {
                    AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                    CharacterId = _player.Client.Character.CharId,
                    Content =
                        $"{(accQuest ? "Account " : "")}Quest <b>\"{questData.Title}\"</b> ({questData.Id}) completed.\nRewards have been sent to your gift chest!",
                    Priority = 1
                };
                _player.Mails.Add(mail);
                _player.SendInfo($"New mail: {(accQuest ? "Account " : "")}Quest completed!");
                if (!accQuest)
                {
                    RemoveCharacterQuest(questData.Id);
                    return;
                }

                RemoveAccountQuest(questData.Id, questData.DailyQuest);
                //Log.Info($"{_player.Name} ({_player.AccountId}) finished quest \"{questsData.Title}\" ({questsData.Id}) at {DateTime.Now}");
            }
        }

        private void GiveGifts(AcceptedQuestData questData)
        {
            ushort objType;
            int i;
            for (i = 0; i < questData.Rewards.Length; i++)
            {
                objType = questData.Rewards[i];
                var item = _player.Manager.Resources.GameData.Items[objType];
                var itemData = ItemData.GenerateData(item);

                if (item.SlotType != 10 && item.SlotType != 26)
                {
                    var quality = ItemData.MakeQuality(item);
                    itemData.Quality = quality;
                    itemData.Runes = ItemData.GetRuneSlots(quality);
                }

                _player.Manager.Database.AddGift(_player.Client.Account, itemData);
            }
        }

        /// <summary>
        /// Give player a quest and increment character hash field <b>nextQuestId</b> on db
        /// <p>Note: <b>questId</b> is set automatically</p>
        /// </summary>
        public void AddAvailableQuest(QuestData questData)
        {
            var finalQuestsData = questData;
            finalQuestsData.Id =
                _player.Manager.Database.GetCharacterHashField(_player.Client.Character, "nextQuestId");
            var mail = new AccountMail()
            {
                AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                CharacterId = _player.Client.Character.CharId,
                Content = $"New Quest available! <b>\"{finalQuestsData.Title}\"</b> ({finalQuestsData.Id})",
                Priority = 1
            };
            _player.Mails.Add(mail);
            _player.SendInfo($"New mail: New quest available!");
            var quests = _player.AvailableQuests.ToList();
            quests.Add(finalQuestsData);
            _player.AvailableQuests = quests.ToArray();
            _player.Manager.Database.IncrementHashField(
                "char." + _player.AccountId + "." + _player.Client.Character.CharId, "nextQuestId");
        }

        /// <summary>
        /// Move quest from available quests to character quests
        /// </summary>
        public void AddCharacterQuest(QuestData questData)
        {
            var quests = _player.CharacterQuests.ToList();
            quests.Add(GetAcceptedQuest(questData, false));
            _player.CharacterQuests = quests.ToArray();
            RemoveAvailableQuest(questData.Id);
        }

        /// <summary>
        /// Give player a quest and increment character hash field <b>nextQuestId</b> on db
        /// <p>Note: <b>questId</b> is set automatically</p>
        /// </summary>
        public void AddAccountQuest(QuestData questData, bool dailyQuest)
        {
            var acceptedQuestData = GetAcceptedQuest(questData, dailyQuest);
            var content = $"New Account Quest! <b>\"{acceptedQuestData.Title}\"</b> ({acceptedQuestData.Id})";
            if (dailyQuest)
                content = $"Daily quest active! ({DateTime.UtcNow.ToString("MMMM dd yyyy")})";
            var mail = new AccountMail()
            {
                AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                CharacterId = -1,
                Content = content,
                Priority = 3
            };
            _player.Mails.Add(mail);
            var info = $"New mail: New account quest!";
            if (dailyQuest)
                info = $"New Daily Quest active! Go to the Quest Room to view it.";
            _player.SendInfo(info);
            AddAccountQuest(acceptedQuestData);
        }

        public AcceptedQuestData GetAcceptedQuest(QuestData questData, bool dailyQuest)
        {
            // Count goals needed to complete quest
            var slay = questData.Slay?.Length ?? 0;
            var dungeons = questData.Dungeons?.Length ?? 0;
            var deliver = questData.Deliver?.Length ?? 0;
            var xp = questData.Experience > 0 ? 1 : 0;
            var scout = questData.Scout != null ? 1 : 0;
            return new AcceptedQuestData
            {
                // Import stuff
                Id = questData.Id,
                AddTime = questData.AddTime,
                EndTime = questData.EndTime,
                Icon = dailyQuest ? (byte)5 : questData.Icon,
                Title = questData.Title,
                Description = questData.Description,
                Rewards = questData.Rewards,
                Slay = questData.Slay,
                SlayAmounts = questData.SlayAmounts,
                Dungeons = questData.Dungeons,
                DungeonAmounts = questData.DungeonAmounts,
                Experience = questData.Experience,
                ExpReward = questData.ExpReward,
                Deliver = questData.Deliver,
                DeliverDatas = questData.DeliverDatas,
                // Set accepted quest specifics
                SlainAmounts = questData.Slay != null ? new int[questData.Slay.Length] : null,
                DungeonsCompleted = questData.Dungeons != null ? new ushort[questData.Dungeons.Length] : null,
                Delivered = questData.Deliver != null ? new bool[questData.Deliver.Length] : null,
                // Set goals
                Goals = new byte[] {0, (byte)(slay + dungeons + xp + deliver + scout)},
                DailyQuest = dailyQuest
            };
        }

        /// <summary>
        /// Add an account quest and increment account hash field <b>nextQuestId</b> on db
        /// <p>Note: <b>id</b> is set automatically</p>
        /// </summary>
        /// <param name="quest">Quest to add</param>
        private void AddAccountQuest(AcceptedQuestData quest)
        {
            var client = _player.Client;
            var finalQuest = quest;
            if (quest.Id == 0)
                finalQuest.Id = client.Manager.Database.GetAccountHashField(client.Account, "nextQuestId");
            var accQuests = client.Account.AccountQuests.ToList();
            accQuests.Add(finalQuest);
            client.Account.AccountQuests = accQuests.ToArray();
            if (quest.Id == 0)
                client.Manager.Database.IncrementHashField("account." + client.Player.AccountId, "nextQuestId");
        }

        /// <summary>
        /// Check if player has the account quest by <b>id</b>
        /// </summary>
        public bool HasAccountQuest(int id)
        {
            var quests = _player.Client.Account.AccountQuests;
            for (var i = 0; i < quests.Length; i++)
                if (quests[i].Id == id)
                    return true;

            return false;
        }

        /// <summary>
        /// Check if player has the available quest by <b>id</b>
        /// </summary>
        public bool HasAvailableQuest(int id)
        {
            var quests = _player.AvailableQuests;
            for (var i = 0; i < quests.Length; i++)
                if (quests[i].Id == id)
                    return true;

            return false;
        }

        /// <summary>
        /// Check if player has the character quest by <b>id</b>
        /// </summary>
        public bool HasCharacterQuest(int id)
        {
            var quests = _player.CharacterQuests;
            for (var i = 0; i < quests.Length; i++)
                if (quests[i].Id == id)
                    return true;

            return false;
        }

        /// <summary>
        /// Remove an account mail by <b>id</b>
        /// </summary>
        public void RemoveAccountQuest(int id, bool dailyQuest = false)
        {
            var quests = _player.Client.Account.AccountQuests.ToList();
            for (var i = 0; i < quests.Count; i++)
            {
                if (quests[i].Id == id)
                {
                    if (dailyQuest)
                        if (quests[i].DailyQuest)
                        {
                            var completed = _player.Client.Account.DailyQuestsCompleted.ToList();
                            completed.Add(id);
                            _player.Client.Account.DailyQuestsCompleted = completed.ToArray();
                        }

                    quests.RemoveAt(i);
                    _player.Client.Account.AccountQuests = quests.ToArray();
                    break;
                }
            }
        }

        /// <summary>
        /// Remove character quest by <b>id</b>
        /// </summary>
        public void RemoveCharacterQuest(int id)
        {
            var quests = _player.CharacterQuests.ToList();
            for (var i = 0; i < quests.Count; i++)
                if (quests[i].Id == id)
                {
                    quests.RemoveAt(i);
                    break;
                }
            
            _player.CharacterQuests = quests.ToArray();
        }

        /// <summary>
        /// Remove available quest by <b>id</b>
        /// </summary>
        public void RemoveAvailableQuest(int id)
        {
            var quests = _player.AvailableQuests.ToList();
            for (var i = 0; i < quests.Count; i++)
                if (quests[i].Id == id)
                {
                    quests.RemoveAt(i);
                    break;
                }

            _player.AvailableQuests = quests.ToArray();
        }

        public void MakeQuestGiver(Player player)
        {
            _questGiver = new QuestGiver(player);
        }
    }
}