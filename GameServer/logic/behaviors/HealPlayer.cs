using System.Xml.Linq;
using common;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors
{
    class HealPlayer : Behavior
    {
        private double _range;
        private Cooldown _coolDown;
        private int _healAmount;

        public HealPlayer(XElement e)
        {
            _range = e.ParseFloat("@range");
            _healAmount = e.ParseInt("@amount", 100);            
            _coolDown = new Cooldown().Normalize(e.ParseInt("@coolDown", 1000));
        }
        
        public HealPlayer(double range, Cooldown coolDown = new Cooldown(), int healAmount = 100)
        {
            _range = range;
            _coolDown = coolDown.Normalize();
            _healAmount = healAmount;
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
                foreach (var entity in host.GetNearestEntities(_range, null, true).OfType<Player>())
                {
                    if (entity.Owner == null)
                        continue;

                    if ((host.AttackTarget != null && host.AttackTarget != entity) || entity.HasConditionEffect(ConditionEffects.Sick))
                        continue;
                    int maxHp = entity.Stats[0];
                    int newHp = Math.Min(entity.HP + _healAmount, maxHp);

                    if (newHp != entity.HP)
                    {
                        int n = newHp - entity.HP;
                        entity.HP = newHp;
                        entity.Owner.BroadcastPacketNearby(new ShowEffect()
                        {
                            EffectType = EffectType.Potion,
                            TargetObjectId = entity.Id,
                            Color = new ARGB(0xffffffff)
                        }, entity, null);
                        entity.Owner.BroadcastPacketNearby(new ShowEffect()
                        {
                            EffectType = EffectType.Trail,
                            TargetObjectId = host.Id,
                            Pos1 = new Position { X = entity.X, Y = entity.Y },
                            Color = new ARGB(0xffffffff)
                        }, host, null);
                        entity.Owner.BroadcastPacketNearby(new Notification()
                        {
                            ObjectId = entity.Id,
                            Message = "+" + n,
                            Color = new ARGB(0xff00ff00)
                        }, entity, null);
                    }
                }
                cool = _coolDown.Next(Random);
            }
            else
                cool -= time.ElapsedMsDelta;

            state = cool;
        }
    }
}
