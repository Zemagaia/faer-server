using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors
{
    class Duration : Behavior
    {

        Behavior child;
        int duration;
        
        public Duration(XElement e, IStateChildren[] behaviors)
        {
            foreach (var behavior in behaviors)
            {
                if (behavior is Behavior bh)
                {
                    child = bh;
                    break;
                }
            }
            duration = e.ParseInt("@duration");
        }
        
        public Duration(Behavior child, int duration)
        {
            this.child = child;
            this.duration = duration;
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
             child.OnStateEntry(host, time);
             state = 0;
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            int timeElapsed = (int)state;
            if (timeElapsed <= duration)
            {
                child.Tick(host, time);
                timeElapsed += time.ElapsedMsDelta;
            }
            state = timeElapsed;
        }
    }
}
