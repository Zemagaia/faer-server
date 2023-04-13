using System.Xml.Linq;
using common;
using GameServer.realm;
using GameServer.realm.worlds.logic;

namespace GameServer.logic.behaviors
{
    class SendToCastle : Behavior
    {
        private int _delay;

        public SendToCastle(XElement e)
        {
            _delay = e.ParseInt("@delay", 15);
        }

        public SendToCastle(int delay = 15)
        {
            _delay = delay;
        }

        private Overseer _overseer;

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            var owner = host.Owner;
            if (!(owner is Realm)) return;
            _overseer = new Overseer(owner, "realm");

            owner.Timers.Add(new WorldTimer(_delay * 1000, (_, _) =>
            {
                if (owner is Realm)
                    _overseer.SendToCastle();
            }));
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
        }
    }
}