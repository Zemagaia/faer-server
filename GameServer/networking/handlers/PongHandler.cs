using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm;

namespace GameServer.networking.handlers
{
    class PongHandler : PacketHandlerBase<Pong>
    {
        public override PacketId ID => PacketId.PONG;

        protected override void HandlePacket(Client client, Pong packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client, packet, t));
        }

        private void Handle(Client client, Pong packet, RealmTime t)
        {
            client.Player?.Pong(t, packet);
        }
    }
}