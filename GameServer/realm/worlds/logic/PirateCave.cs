using common.resources;
using GameServer.networking;

namespace GameServer.realm.worlds.logic
{
    public class PirateCave : World
    {
        public PirateCave(ProtoWorld proto, Client client = null) : base(proto)
        {
        }

        protected override void Init()
        {
            var template = DungeonTemplates.GetTemplate(Name);

            FromDungeonGen(Rand.Next(), template);
        }
    }
}