using System.Xml.Linq;
using common;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors
{
    class EnemyAoe : CycleBehavior
    {
        //State storage: cooldown timer

        private readonly float _radius;
        private readonly int _minDamage;
        private readonly int _maxDamage;
        private readonly bool _noDef;
        private readonly bool _players;
        private readonly ARGB _color;
        private readonly ConditionEffectIndex _effect;
        private readonly int _effectDuration;
        private Cooldown _coolDown;
        private int _damage;

        public EnemyAoe(XElement e)
        {
            _radius = e.ParseFloat("@radius");
            _minDamage = e.ParseInt("@minDamage");
            _maxDamage = e.ParseInt("@maxDamage");
            _noDef = e.ParseBool("@noDef");
            _effect = e.ParseConditionEffect("@effect");
            _effectDuration = e.ParseInt("@effectDuration");
            _players = e.ParseBool("@players");
            _color = new ARGB(e.ParseUInt("@color", undefined: 0xffff0000));
            _coolDown = new Cooldown().Normalize(e.ParseInt("@coolDown"));
        }
        
        public EnemyAoe(
            double radius,
            int minDamage,
            int maxDamage,
            bool noDef = false,
            ConditionEffectIndex effect = 0,
            int effectDuration = 0,
            bool players = true,
            uint color = 0xff0000,
            Cooldown coolDown = new())
        {
            _radius = (float)radius;
            _minDamage = minDamage;
            _maxDamage = maxDamage;
            _noDef = noDef;
            _effect = effect;
            _effectDuration = effectDuration;
            _players = players;
            _color = new ARGB(color);
            _coolDown = coolDown.Normalize();
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            state = 0;
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            int cool = (int)state;

            if (cool <= 0)
            {
                _damage = Random.Next(_minDamage, _maxDamage);
                if (host.HasConditionEffect(ConditionEffects.Stunned))
                    return;

                var player = host.AttackTarget ?? host.GetNearestEntity(_radius * 2, _players);

                if (host.TauntedPlayerNearby(_radius * 2) && _players)
                    player = host.GetNearestTauntedPlayer(_radius * 2);

                if (player != null)
                {
                    var target = new Position()
                    {
                        X = host.X,
                        Y = host.Y
                    };

                    var entities = new List<Entity>();
                    host.Owner.AOE(target, _radius, _players, en => { entities.Add(en); });

                    if (_players)
                        host.Owner.BroadcastPacketNearby(new Aoe()
                        {
                            Pos = target,
                            Radius = _radius,
                            Damage = (ushort)_damage,
                            Duration = 0,
                            Effect = 0,
                            Color = _color,
                            OrigType = host.ObjectType
                        }, host, null);
                    else
                        host.Owner.BroadcastPacketNearby(new ShowEffect()
                        {
                            EffectType = EffectType.AreaBlast,
                            TargetObjectId = host.Id,
                            Color = _color,
                            Pos1 = new Position() { X = _radius }
                        }, host, exclusive: host.GetPlayerOwner());

                    foreach (var entity in entities)
                    {
                        if (entity is Player p && _players)
                        {
                            var tenacity = Constants.NegativeEffsIdx.Contains(_effect)
                                ? 1d - (double)p.Stats[13] / 100
                                : 1d;
                            ((IPlayer)p).Damage(_damage, host, _noDef);
                            if (!p.HasConditionEffect(ConditionEffects.Invincible) &&
                                !p.HasConditionEffect(ConditionEffects.Stasis))
                                p.ApplyConditionEffect(_effect, (int)(Math.Max(1, _effectDuration * tenacity)));
                        }
                        else if (entity is Enemy e && !_players)
                        {
                            e.Damage(host.GetPlayerOwner(), time, _damage, _noDef);
                            if (!e.HasConditionEffect(ConditionEffects.Invincible) &&
                                !e.HasConditionEffect(ConditionEffects.Stasis))
                                e.ApplyConditionEffect(_effect, _effectDuration);
                        }
                    }

                    cool = _coolDown.Next(Random);
                }
            }
            else
                cool -= time.ElapsedMsDelta;

            state = cool;
        }
    }
}