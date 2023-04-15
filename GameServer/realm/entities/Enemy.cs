using Shared;
using Shared.resources;
using Shared.terrain;
using GameServer.logic;
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
            if (stat || Owner == null) return 0;
            dmg = (int)StatsManager.GetDefenseDamage(this, dmg, damageType, from);
            var effDmg = dmg;
            if (effDmg > HP)
                effDmg = HP;
            if (!HasConditionEffect(ConditionEffects.Invulnerable))
                HP -= dmg;
            ApplyConditionEffect(effs);
            
            foreach (var plr in Owner.Players.Values)
                if (MathUtils.DistSqr(X, Y, plr.X, plr.Y) < 16 * 16)
                    plr.Client.SendDamage(Id, 0, (ushort)dmg, HP < 0, 0, from.Id);
            
            DamageCounter.HitBy(from, time, null, dmg);

            if (HP < 0 && Owner != null)
            {
                Death(time);
            }

            return effDmg;
        }

        public override bool HitByProjectile(Projectile projectile, RealmTime time)
        {
            if (stat || Owner == null ||
                projectile.ProjectileOwner is not Player p)
                return false;
            
            var dmg = (int)StatsManager.GetDefenseDamage(this, projectile.Damage, projectile.DamageType, p);
            if (!HasConditionEffect(ConditionEffects.Invulnerable))
                HP -= dmg;
            ApplyConditionEffect(projectile.ProjDesc.Effects);

            // Stheno's Kiss
            if (!HasConditionEffect(ConditionEffects.Invulnerable) &&
                p.Inventory[1].Power == "Stheno's Kiss" && !p.OnCooldown(0))
            {
                for (var i = 0; i < 5; i++)
                    Owner.Timers.Add(new WorldTimer(1000 * i, (_, t) => Damage(p, t, 200, false, true)));

                p.SetCooldown(0, 15);
            }
            
            foreach (var plr in Owner.Players.Values)
                if (MathUtils.DistSqr(X, Y, plr.X, plr.Y) < 16 * 16)
                    plr.Client.SendDamage(Id, projectile.ConditionEffects, (ushort)dmg, HP < 0, projectile.BulletId, projectile.ProjectileOwner.Self.Id);
            
            DamageCounter.HitBy(p, time, projectile, dmg);

            if (HP < 0 && Owner != null)
            {
                Death(time);
            }

            return true;
        }
        
        private float _bleeding;

        public override void Tick(RealmTime time)
        {
            pos ??= new Position {X = X, Y = Y};

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