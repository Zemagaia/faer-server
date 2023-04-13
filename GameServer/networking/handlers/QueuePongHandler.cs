using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm;

namespace GameServer.networking.handlers
{
    class QueuePongHandler : PacketHandlerBase<QueuePong>
    {
        public override PacketId ID => PacketId.QUEUE_PONG;

        protected override void HandlePacket(Client client, QueuePong packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client, packet, t));
        }

        private void Handle(Client client, QueuePong packet, RealmTime t)
        {
            client.Pong(t, packet);
        }
    }
}