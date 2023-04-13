using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class UpdateAckHandler : PacketHandlerBase<UpdateAck>
    {
        public override PacketId ID => PacketId.UPDATEACK;

        protected override void HandlePacket(Client client, UpdateAck packet)
        {
            if (client.State == ProtocolState.Reconnecting)
                return;

            client.Player.UpdateAckReceived();
        }
    }
}