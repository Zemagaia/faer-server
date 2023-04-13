using common;
using GameServer.networking.packets.outgoing;

namespace GameServer.realm.entities.player
{
    public partial class Player
    {
        /// <summary>
        /// Gets Exp Goal for current level
        /// <p><i><b>Note:</b> level 300 returns 0</i></p>
        /// </summary>
        public static int GetExpGoal(int level)
        {
            return 50 + (level - 1) * 502;
        }

        /// <summary>
        /// Gets Exp Goal for current level
        /// </summary>
        public static int GetExpGoal2(int level)
        {
            return 50 + (level - 1) * 502;
        }

        public static int GetLevelExp(int level)
        {
            if (level == 1) return 0;
            return 50 * (level - 1) + (level - 2) * (level - 1) * 251;
        }

        public static int GetFameGoal(int fame)
        {
            if (fame >= 2000) return 0;
            else if (fame >= 800) return 2000;
            else if (fame >= 400) return 800;
            else if (fame >= 150) return 400;
            else if (fame >= 20) return 150;
            else return 20;
        }

        public int GetStars()
        {
            int ret = 0;
            foreach (var i in FameCounter.ClassStats.AllKeys)
            {
                var entry = FameCounter.ClassStats[ushort.Parse(i)];
                if (entry.BestFame >= 2000) ret += 5;
                else if (entry.BestFame >= 800) ret += 4;
                else if (entry.BestFame >= 400) ret += 3;
                else if (entry.BestFame >= 150) ret += 2;
                else if (entry.BestFame >= 20) ret += 1;
            }

            return ret;
        }

        private static readonly Dictionary<string, Tuple<int, int, int>> QuestDat =
            new() //Priority, Min, Max
            {
                #region Wandering Quest Enemies

                //{ "Great White Shark", Tuple.Create(4, 5, 10) },

                #endregion

                #region Setpiece Bosses

                #endregion

                #region Events

                #endregion

                #region Dungeon Bosses

                // { "Evil Chicken God", Tuple.Create(15, 1, 300) },

                #endregion

                #region Special Events

                #endregion
            };

        Entity FindQuest(Position? destination = null)
        {
            Entity ret = null;
            double? bestScore = null;
            var pX = !destination.HasValue ? X : destination.Value.X;
            var pY = !destination.HasValue ? Y : destination.Value.Y;

            foreach (var i in Owner.Quests.Values
                .OrderBy(quest => MathsUtils.DistSqr(quest.X, quest.Y, pX, pY)))
            {
                if (i.ObjectDesc == null || !i.ObjectDesc.Quest) continue;

                Tuple<int, int, int> x;
                if (!QuestDat.TryGetValue(i.ObjectDesc.ObjectId, out x))
                    continue;

                if ((Level >= x.Item2 && Level <= x.Item3))
                {
                    var score = (300 - Math.Abs((i.ObjectDesc.Level ?? 0) - Level)) * x.Item1 - //priority * level diff
                                this.Dist(i) / 100; //minus 1 for every 100 tile distance
                    if (bestScore == null || score > bestScore)
                    {
                        bestScore = score;
                        ret = i;
                    }
                }
            }

            return ret;
        }

        Entity questEntity;
        public Entity Quest => questEntity;

        public void HandleQuest(RealmTime time, bool force = false, Position? destination = null)
        {
            if (force || time.TickCount % 500 == 0 || questEntity == null || questEntity.Owner == null)
            {
                var newQuest = FindQuest(destination);
                if (newQuest != null && newQuest != questEntity)
                {
                    Owner.Timers.Add(new WorldTimer(100, (w, t) =>
                    {
                        _client.SendPacket(new QuestObjId()
                        {
                            ObjectId = newQuest.Id
                        });
                    }));
                    questEntity = newQuest;
                }
            }
        }

        public void CalculateFame()
        {
            var newFame = (Experience < 200 * 1000) ? Experience / 1000 : 200 + (Experience - 200 * 1000) / 4000;

            if (newFame == Fame)
                return;

            var stats = FameCounter.ClassStats[ObjectType];
            var newGoal = GetFameGoal(stats.BestFame > newFame ? stats.BestFame : newFame);

            if (newGoal > FameGoal)
            {
                BroadcastSync(new Notification()
                {
                    ObjectId = Id,
                    Color = new ARGB(0xFF00FF00),
                    Message = "Class Quest Completed!"
                }, p => this.DistSqr(p) < RadiusSqr);
                Stars = GetStars();
            }
            else if (newFame != Fame)
            {
                BroadcastSync(new Notification()
                {
                    ObjectId = Id,
                    Color = new ARGB(0xFFE25F00),
                    Message = $"{(newFame - Fame > 0 ? "+" : "")}{newFame - Fame} Fame"
                }, p => this.DistSqr(p) < RadiusSqr);
            }

            Fame = newFame;
            FameGoal = newGoal;
        }

        bool CheckLevelUp()
        {
            var levelUps = 0;
            var level = Level;
            do
            {
                if (level < 20)
                    levelUps++;
                level++;
            } while (Experience - GetLevelExp(level) >= ExperienceGoal);

            string IdToStat(int id)
            {
                string ret = "";
                switch (id)
                {
                    case 0:
                        ret = "HP";
                        break;
                    case 1:
                        ret = "MP";
                        break;
                    case 2:
                        ret = "STR";
                        break;
                    case 4:
                        ret = "AGL";
                        break;
                    case 5:
                        ret = "DEX";
                        break;
                    case 6:
                        ret = "STA";
                        break;
                    case 7:
                        ret = "INT";
                        break;
                }

                return ret;
            }

            if (Experience - GetLevelExp(Level) >= ExperienceGoal && Level < 300)
            {
                Level = level;
                ExperienceGoal = GetExpGoal(Level);
                var statInfo = Manager.Resources.GameData.Classes[ObjectType].Stats;
                for (int lup = 0; lup < levelUps; lup++)
                {
                    string levelUpText = $"Level up!";
                    for (var i = 0; i < statInfo.Length; i++)
                    {
                        var min = statInfo[i].MinIncrease;
                        var max = statInfo[i].MaxIncrease + 1;
                        var increase = MathUtils.Next(min, max);

                        if (i != 3 && increase > 0 && Stats.Base[i] < statInfo[i].MaxValue)
                            levelUpText += $" +{increase} {IdToStat(i)}";

                        Stats.Base[i] += increase;
                        if (Stats.Base[i] >= statInfo[i].MaxValue)
                            Stats.Base[i] = statInfo[i].MaxValue;
                    }

                    SendInfo(levelUpText);
                }

                HP = Stats[0];
                MP = Stats[1];

                foreach (var i in Owner.Players.Values)
                {
                    if (Level % 50 == 0)
                        i.SendInfo($"{Name} achieved level {Level}");
                }

                // to get exp scaled to new exp goal
                InvokeStatChange(StatsType.Experience, Experience - GetLevelExp(Level), true);
                questEntity = null;

                return true;
            }

            CalculateFame();
            return false;
        }

        public bool EnemyKilled(Enemy enemy, int exp, bool killer)
        {
            if (enemy != null && enemy == questEntity)
                BroadcastSync(new Notification()
                {
                    ObjectId = Id,
                    Color = new ARGB(0xFF00FF00),
                    Message = "Quest Complete!"
                }, p => this.DistSqr(p) < RadiusSqr);
            if (exp != 0)
            {
                Experience += exp;
            }

            FameCounter.Killed(enemy, killer);
            return CheckLevelUp();
        }
    }
}