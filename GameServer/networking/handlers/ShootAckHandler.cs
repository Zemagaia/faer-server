using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class ShootAckHandler : PacketHandlerBase<ShootAck>
    {
        public override PacketId ID => PacketId.SHOOTACK;

        protected override void HandlePacket(Client client, ShootAck packet)
        {
            client.Manager.Logic.AddPendingAction(t => client.Player.DequeueShotSync(t, packet));
        }
    }
}