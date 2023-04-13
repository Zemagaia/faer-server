using System.Xml.Linq;
using common;
using GameServer.realm;

namespace GameServer.logic.behaviors
{
    class AddImmunity : Behavior
    {
        //State storage: none

        Immunity _id;
        bool _perm;
        int _duration;

        public AddImmunity(Immunity id, bool perm = false, int duration = -1)
        {
            _id = id;
            _perm = perm;
            _duration = duration;
        }

        public AddImmunity(XElement e)
        {
            _id = (Immunity)Enum.Parse(typeof(Immunity), e.ParseString("@id"), true);
            _perm = e.ParseBool("@perm");
            _duration = e.ParseInt("@duration", -1);
        }

        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            host.ApplyImmunity(_id, _duration);
        }

        protected override void OnStateExit(Entity host, RealmTime time, ref object state)
        {
            if (!_perm)
            {
                host.ApplyImmunity(_id, 0);
            }
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
        }
    }
}