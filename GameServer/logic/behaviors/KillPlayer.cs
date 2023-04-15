using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors
{
    class KillPlayer : Behavior
    {
        private Cooldown _coolDown;
        private readonly string _killMessage;
        private readonly bool _rekt;
        private readonly bool _killAll;
        
        public KillPlayer(XElement e)
        {
            _killMessage = e.ParseString("@killMessage");
            _coolDown = new Cooldown().Normalize(e.ParseInt("@coolDown", 1000));
            _rekt = e.ParseBool("@rekt");
            _killAll = e.ParseBool("@killAll");
        }

        public KillPlayer(string killMessage, Cooldown coolDown = new Cooldown(), bool rekt = true, bool killAll = false)
        {
            _coolDown = coolDown.Normalize();
            _killMessage = killMessage;
            _rekt = rekt;
            _killAll = killAll;
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            state = _coolDown.Next(Random);
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            if (host.AttackTarget == null || host.AttackTarget.Owner == null)
                return;
                
            var cool = (int)state;

            if (cool <= 0)
            {
                // death strike
                if (_killAll)
                    foreach (var plr in host.Owner.Players.Values
                        .Where(p => !p.HasConditionEffect(ConditionEffects.Hidden)))
                    {
                        Kill(host, plr);
                    }
                else
                    Kill(host, host.AttackTarget);

                // send kill message
                if (_killMessage != null)
                {
                    foreach (var p in host.Owner.Players.Values)
                        if (MathUtils.DistSqr(p.X, p.Y, host.X, host.Y) < 16 * 16)
                            p.Client.SendText("#" + (host.ObjectDesc.DisplayId ?? host.ObjectDesc.ObjectId), host.Id, 3, "", _killMessage, 0xAB1533, 0xAB1533);
                }

                cool = _coolDown.Next(Random);
            }
            else
                cool -= time.ElapsedMsDelta;

            state = cool;
        }

        private void Kill(Entity host, Player player)
        {
            foreach (var p in host.Owner.Players.Values)
                if (MathUtils.DistSqr(p.X, p.Y, host.X, host.Y) < 16 * 16)
                    p.Client.SendShowEffect(EffectType.Trail, host.Id, player.X, player.Y, 0, 0, 0xFFFFFFFF);
            

            // kill player
            player.Death(host.ObjectDesc.DisplayId, rekt: _rekt);
        }
    }
}
