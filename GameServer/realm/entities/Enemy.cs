using common;
using common.resources;
using common.terrain;
using GameServer.logic;
using GameServer.networking.packets.outgoing;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;

namespace GameServer.realm.entities
{
    public class Enemy : Character
    {
        private readonly bool stat;
        public Enemy ParentEntity;

        public Enemy(RealmManager manager, ushort objType)
            : base(manager, objType)
        {
            stat = ObjectDesc.MaxHP == 0;
            DamageCounter = new DamageCounter(this);
        }

        public DamageCounter DamageCounter { get; private set; }

        public TileRegion Region { get; set; }

        private Position? pos;
        public Position SpawnPoint => pos ?? new Position { X = X, Y = Y };

        public override void Init(World owner)
        {
            base.Init(owner);
            // todo: immunity
            if (ObjectDesc.StasisImmune)
                return;
        }

        public void SetDamageCounter(DamageCounter counter, Enemy enemy)
        {
            DamageCounter = counter;
            DamageCounter.UpdateEnemy(enemy);
        }

        public event EventHandler<BehaviorEventArgs> OnDeath;

        public void Death(RealmTime time)
        {
            DamageCounter.Death(time);
            CurrentState?.OnDeath(new BehaviorEventArgs(this, time));
            OnDeath?.Invoke(this, new BehaviorEventArgs(this, time));
            Owner.LeaveWorld(this);
        }

        public int Damage(Player from, RealmTime time, int dmg, bool noDef, bool dmgTypeDelayed = false,
            DamageTypes damageType = DamageTypes.Magical, params ConditionEffect[] effs)
        {
            if (stat || Owner == null || HasConditionEffect(ConditionEffects.Invincible) ||
                HasConditionEffect(ConditionEffects.Paused) || HasConditionEffect(ConditionEffects.Stasis)) return 0;
            dmg = (int)StatsManager.GetDefenseDamage(this, dmg, damageType, from);
            var effDmg = dmg;
            if (effDmg > HP)
                effDmg = HP;
            if (!HasConditionEffect(ConditionEffects.Invulnerable))
                HP -= dmg;
            ApplyConditionEffect(effs);

            Owner?.BroadcastPacketNearby(new Damage()
            {
                TargetId = Id,
                Effects = 0,
                DamageAmount = (ushort)dmg,
                Kill = HP < 0,
                BulletId = 0,
                ObjectId = from.Id,
                DTp = dmgTypeDelayed
            }, this);

            DamageCounter.HitBy(from, time, null, dmg);

            if (HP < 0 && Owner != null)
            {
                Death(time);
            }

            return effDmg;
        }

        public override bool HitByProjectile(Projectile projectile, RealmTime time)
        {
            if (stat || Owner == null || HasConditionEffect(ConditionEffects.Invincible) ||
                HasConditionEffect(ConditionEffects.Paused) || HasConditionEffect(ConditionEffects.Stasis) ||
                projectile.ProjectileOwner is not Player p)
                return false;

            int[] fDamages = null;
            var inv = p.Inventory;
            for (var i = 0; i < 6; i++)
            {
                if (inv[i].Item is null || inv[i].DamageBoosts is null) continue;
                if (fDamages is null)
                {
                    fDamages = inv[i].DamageBoosts;
                    continue;
                }

                fDamages = StatsManager.DamageUtils.Add(fDamages, inv[i].DamageBoosts);
            }

            var dmg = GetDamage(projectile, fDamages, p);
            if (!HasConditionEffect(ConditionEffects.Invulnerable))
                HP -= dmg;
            ApplyConditionEffect(projectile.ProjDesc.Effects);

            // Stheno's Kiss
            if (!p.HasConditionEffect(ConditionEffects.Suppressed) &&
                !HasConditionEffect(ConditionEffects.Invulnerable) &&
                p.Inventory[1].Item.Power == "Stheno's Kiss" && !p.OnCooldown(0))
            {
                for (var i = 0; i < 5; i++)
                    Owner.Timers.Add(new WorldTimer(1000 * i, (_, t) => Damage(p, t, 200, false, true)));

                p.SetCooldown(0, 15);
            }
            
            Owner.BroadcastPacketNearby(new Damage
            {
                TargetId = Id,
                Effects = projectile.ConditionEffects,
                DamageAmount = (ushort)dmg,
                Kill = HP < 0,
                BulletId = projectile.BulletId,
                ObjectId = projectile.ProjectileOwner.Self.Id
            }, this, p);

            DamageCounter.HitBy(p, time, projectile, dmg);

            if (HP < 0 && Owner != null)
            {
                Death(time);
            }

            return true;
        }

        private int GetDamage(Projectile projectile, int[] fDamages, Player player)
        {
            if (fDamages is null)
                return (int)StatsManager.GetDefenseDamage(this, projectile.Damage, projectile.DamageType, player);

            return (int)StatsManager.GetDefenseDamage(this,
                new[]
                {
                    projectile.Damage, fDamages[0], fDamages[1], fDamages[2],
                    fDamages[3], fDamages[4], fDamages[5], fDamages[6], fDamages[7]
                },
                new[]
                {
                    projectile.DamageType, DamageTypes.Physical, DamageTypes.Magical, DamageTypes.Earth,
                    DamageTypes.Air, DamageTypes.Profane, DamageTypes.Fire, DamageTypes.Water, DamageTypes.Holy
                }, player);
        }

        private float _bleeding;

        public override void Tick(RealmTime time)
        {
            if (pos == null)
                pos = new Position() { X = X, Y = Y };

            if (!stat && HasConditionEffect(ConditionEffects.Bleeding))
            {
                if (_bleeding > 1)
                {
                    HP -= (int)_bleeding;
                    _bleeding -= (int)_bleeding;
                }

                _bleeding += 28 * (time.ElapsedMsDelta / 1000f);
            }

            base.Tick(time);
        }
    }
}