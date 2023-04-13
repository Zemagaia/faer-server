// this might be unnecessary, there are no special calls anymore

using common.resources;
using GameServer.networking;

namespace GameServer.realm.worlds.logic
{
    public class Marketplace : World
    {
        public Marketplace(ProtoWorld proto, Client client = null) : base(proto)
        {
        }
    }
}