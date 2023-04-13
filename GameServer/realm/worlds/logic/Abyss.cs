using common.resources;
using GameServer.networking;

namespace GameServer.realm.worlds.logic
{
    public class Abyss : World
    {
        public Abyss(ProtoWorld proto, Client client = null) : base(proto)
        {
        }

        protected override void Init()
        {
            var template = DungeonTemplates.GetTemplate(Name);

            FromDungeonGen(Rand.Next(), template);
        }
    }
}