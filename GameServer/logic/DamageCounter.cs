using Shared;
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