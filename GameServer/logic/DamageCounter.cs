using common;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic
{
    public class DamageCounter
    {
        private Enemy _enemy;

        public Enemy Host => _enemy;

        public Projectile LastProjectile { get; private set; }
        public Player LastHitter { get; private set; }

        public DamageCounter Corpse { get; set; }
        public DamageCounter Parent { get; set; }

        private WeakDictionary<Player, int> hitters = new();

        public DamageCounter(Enemy enemy)
        {
            _enemy = enemy;
        }

        public void HitBy(Player player, RealmTime time, Projectile projectile, int dmg)
        {
            int totalDmg;
            if (!hitters.TryGetValue(player, out totalDmg))
                totalDmg = 0;
            totalDmg += dmg;
            hitters[player] = totalDmg;

            if (_enemy.ObjectDesc.Quest && _enemy.HP > 0)
                player.DamageDealt = hitters[player];

            if (!_enemy.HasConditionEffect(ConditionEffects.Invulnerable))
            {
                var lifeSteal = player.Stats[15];
                var manaLeech = player.Stats[16];
                player.HP += lifeSteal;
                player.MP += manaLeech;
            }

            LastProjectile = projectile;
            LastHitter = player;

            player.FameCounter.Hit(projectile, _enemy);
        }

        public Tuple<Player, int>[] GetPlayerData()
        {
            if (Parent != null)
                return Parent.GetPlayerData();
            var dat = new List<Tuple<Player, int>>();
            foreach (var i in hitters)
            {
                if (i.Key.Owner == null) continue;
                dat.Add(new Tuple<Player, int>(i.Key, i.Value));
            }

            return dat.ToArray();
        }

        public void UpdateEnemy(Enemy enemy)
        {
            _enemy = enemy;
        }

        public void Death(RealmTime time)
        {
            if (Corpse != null)
            {
                Corpse.Parent = this;
                return;
            }

            var enemy = (Parent ?? this)._enemy;

            if (enemy.Spawned)
                return;
            
            if (LastHitter != null)
            {
                var lifeStealKill = LastHitter.Stats[17];
                var manaLeechKill = LastHitter.Stats[18];
                LastHitter.HP += lifeStealKill;
                LastHitter.MP += manaLeechKill;
            }

            if (enemy.ObjectDesc.Quest)
                foreach (var player in hitters.Keys)
                    player.DamageDealt = 0;

            if (enemy.Owner.Overseer != null)
                enemy.Owner.EnemyKilled(enemy, (Parent ?? this).LastHitter);

            var lvlUps = 0;
            foreach (var player in enemy.Owner.Players.Values
                .Where(p => enemy.Dist(p) < 25))
            {
                if (player.HasConditionEffect(ConditionEffects.Paused))
                    continue;

                double xp = enemy.GivesNoXp ? 0 : 1;
                xp *= enemy.ObjectDesc.MaxHP / 10f *
                      (enemy.ObjectDesc.ExpMultiplier ?? 1);

                if (enemy.ObjectDesc.God)
                    xp *= 2f; // 2x multiplier for god

                if (enemy.ObjectDesc.Level != null && player.Level >= enemy.ObjectDesc.Level && enemy.ObjectDesc.Quest)
                    xp *= 2f + Math.Round((float)enemy.ObjectDesc.Level / 50, 1); // (2 for quest + Quest level / 50)x
                else if (enemy.ObjectDesc.Level != null && player.Level < enemy.ObjectDesc.Level &&
                         enemy.ObjectDesc.Quest)
                    // less xp if player level is lower than quest level
                    xp *= 2f + Math.Round((float)enemy.ObjectDesc.Level / 100,
                        1); // (2 for quest + Quest level / 100)x

                var upperLimit = player.ExperienceGoal * (enemy.ObjectDesc.Quest ? 0.5f : 0.1f);
                if (player.Quest == enemy)
                {
                    xp *= 1.5f;
                    upperLimit = player.ExperienceGoal;
                }

                if (player.Level < 150)
                    upperLimit = player.ExperienceGoal;

                if (enemy.ObjectDesc.UncappedXP && player.Level >= 50)
                {
                    upperLimit = (float)xp;
                }
                else if (enemy.ObjectDesc.UncappedXP && player.Level < 50)
                {
                    for (var i = 0; i < 5; i++)
                        upperLimit += Player.GetExpGoal(player.Level + i);
                }

                var globalXpBoost = DateTime.UtcNow.ToUnixTimestamp() < Constants.EventEnds.ToUnixTimestamp()
                    ? Constants.GlobalXpBoost ?? 1
                    : 1;
                xp *= globalXpBoost;

                var playerXp = xp;
                if (upperLimit < playerXp)
                    playerXp = upperLimit;


                if (player.XpBoostItem > 0)
                {
                    xp *= 1f + (float)player.XpBoostItem / 100;
                    playerXp *= 1f + (float)player.XpBoostItem / 100;
                }

                if (player.XPBoostTime != 0 && player.Level < 300)
                {
                    xp *= 1.3f;
                    playerXp *= 1.3f;
                }

                var killer = (Parent ?? this).LastHitter == player;
                if (player.EnemyKilled(
                    enemy,
                    (int)playerXp,
                    killer) && !killer)
                    lvlUps++;
                if (hitters.ContainsKey(player))
                {
                    UpdateQuestKillGoals(killer, player, enemy);
                    UpdateQuestExperienceGoals(player, (int)xp);
                }
            }

            if ((Parent ?? this).LastHitter != null)
                (Parent ?? this).LastHitter.FameCounter.LevelUpAssist(lvlUps);
        }

        private void UpdateQuestKillGoals(bool killer, Player player, Enemy enemy)
        {
            if (!killer || hitters[player] < enemy.MaximumHP * 0.1)
            {
                return;
            }

            int i;
            int j;
            var quests = player.CharacterQuests;
            for (i = 0; i < quests.Length; i++)
            for (j = 0; j < quests[i].Slay.Length; j++)
            {
                if (quests[i].SlainAmounts[j] == quests[i].SlayAmounts[j] ||
                    quests[i].Slay[j] != enemy.ObjectType)
                {
                    continue;
                }

                quests[i].SlainAmounts[j]++;
                if (quests[i].SlainAmounts[j] == quests[i].SlayAmounts[j])
                {
                    quests[i].Goals[0]++;
                }
            }

            quests = player.Client.Account.AccountQuests;
            for (i = 0; i < quests.Length; i++)
            for (j = 0; j < quests[i].Slay.Length; j++)
            {
                if (quests[i].SlainAmounts[j] == quests[i].SlayAmounts[j] ||
                    quests[i].Slay[j] != enemy.ObjectType)
                {
                    continue;
                }

                quests[i].SlainAmounts[j]++;
                if (quests[i].SlainAmounts[j] == quests[i].SlayAmounts[j])
                {
                    quests[i].Goals[0]++;
                }
            }

            player.Client.Account.AccountQuests = quests;
        }

        private void UpdateQuestExperienceGoals(Player player, int xp)
        {
            int i;
            var quests = player.CharacterQuests;
            for (i = 0; i < quests.Length; i++)
            {
                if (quests[i].ExpGained == quests[i].Experience)
                {
                    continue;
                }

                quests[i].ExpGained =
                    Math.Min(quests[i].Experience, quests[i].ExpGained + xp);
                if (quests[i].ExpGained == quests[i].Experience)
                {
                    quests[i].Goals[0]++;
                }
            }

            quests = player.Client.Account.AccountQuests;
            for (i = 0; i < quests.Length; i++)
            {
                if (quests[i].ExpGained == quests[i].Experience)
                {
                    continue;
                }

                quests[i].ExpGained = Math.Min(quests[i].Experience, quests[i].ExpGained + xp);
                if (quests[i].ExpGained == quests[i].Experience)
                {
                    quests[i].Goals[0]++;
                }
            }

            player.Client.Account.AccountQuests = quests;
        }

        public void TransferData(DamageCounter dc)
        {
            dc.LastProjectile = LastProjectile;
            dc.LastHitter = LastHitter;

            int totalDmg;
            int totalExistingDmg;
            foreach (var plr in hitters.Keys)
            {
                if (!hitters.TryGetValue(plr, out totalDmg))
                    totalDmg = 0;
                if (!dc.hitters.TryGetValue(plr, out totalExistingDmg))
                    totalExistingDmg = 0;

                dc.hitters[plr] = totalDmg + totalExistingDmg;
            }
        }
    }
}