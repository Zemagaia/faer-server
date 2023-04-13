using System.Xml.Linq;
using common;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors
{
    class HealSelf : Behavior
    {
        //State storage: cooldown timer

        private Cooldown _coolDown;
        readonly int? _amount;

        public HealSelf(XElement e)
        {
            _amount = e.ParseNInt("@amount");    
            _coolDown = new Cooldown().Normalize(e.ParseInt("@coolDown", 1000));
        }
        
        public HealSelf(Cooldown coolDown = new Cooldown(), int? amount = null)
        {
            _coolDown = coolDown.Normalize();
            _amount = amount;
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            state = 0;
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            var cool = (int)state;

            if (cool <= 0)
            {
                if (host.HasConditionEffect(ConditionEffects.Stunned)) 
                    return;

                var entity = host as Character;

                if (entity == null)
                    return;

                int newHp = entity.ObjectDesc.MaxHP;
                if (_amount != null)
                {
                    var newHealth = (int)_amount + entity.HP;
                    if (newHp > newHealth)
                        newHp = newHealth;
                }
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
                        Pos1 = new Position() { X = entity.X, Y = entity.Y },
                        Color = new ARGB(0xffffffff)
                    }, host, null);
                    entity.Owner.BroadcastPacketNearby(new Notification()
                    {
                        ObjectId = entity.Id,
                        Message = "+" + n,
                        Color = new ARGB(0xff00ff00)
                    }, entity, null);
                }
                
                cool = _coolDown.Next(Random);
            }
            else
                cool -= time.ElapsedMsDelta;

            state = cool;
        }
    }
}
