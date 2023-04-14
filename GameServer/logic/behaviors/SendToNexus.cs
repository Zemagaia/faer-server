using System.Xml.Linq;
using common;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.worlds;

namespace GameServer.logic.behaviors
{
    class SendToNexus : Behavior
    {
        private double _delay;

        public SendToNexus(XElement e)
        {
            _delay = e.ParseFloat("@delay", 5);
        }

        public SendToNexus(double delay = 5)
        {
            _delay = delay;
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            var owner = host.Owner;


            foreach (var player in owner.Players.Values)
            {
                player.Client.SendShowEffect(EffectType.Earthquake, 0, 0, 0, 0, 0, 0);
               
            }

            owner.Timers.Add(new WorldTimer((int)(_delay * 1000), (world, t) =>
            {
                foreach (var player in owner.Players.Values)
                {
                    player.Client.Reconnect(new Reconnect()
                    {
                        Host = "",
                        Port = 2050,
                        GameId = World.Realm,
                        Name = "Nexus"
                    });
                }
            }));
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
        }
    }
}