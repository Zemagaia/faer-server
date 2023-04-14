using common.resources;
using GameServer.networking;
using GameServer.networking.packets.outgoing;
using GameServer.realm.entities.player;

namespace GameServer.realm.worlds.logic
{
    class Davy : World
    {
        private bool _greenFound;
        private bool _redFound;
        private bool _yellowFound;
        private bool _purpleFound;

        public Davy(ProtoWorld proto, Client client = null) : base(proto)
        {
        }

        public override int EnterWorld(Entity entity)
        {
            var player = entity as Player;
            if (player != null)
            {
                var client = player.Client;
                
            }

            return base.EnterWorld(entity);
        }

        public override void LeaveWorld(Entity entity)
        {
            base.LeaveWorld(entity);
            
        }
    }
}