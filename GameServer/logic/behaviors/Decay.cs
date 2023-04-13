using System.Xml.Linq;
using common;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors
{
    class Decay : Behavior
    {
        //State storage: timer

        int time;
        
        public Decay(XElement e)
        {
            time = e.ParseInt("@time", 10000);
        }

        public Decay(int time = 10000)
        {
            this.time = time;
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            state = this.time;
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            int cool = (int)state;

            if (cool <= 0)
                if (!(host is Enemy))
                    host.Owner.LeaveWorld(host);
                else
                    (host as Enemy).Death(time);
            else
                cool -= time.ElapsedMsDelta;

            state = cool;
        }
    }
}
