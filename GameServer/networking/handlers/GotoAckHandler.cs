using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class GotoAckHandler : PacketHandlerBase<GotoAck>
    {
        public override PacketId ID => PacketId.GOTOACK;

        protected override void HandlePacket(Client client, GotoAck packet)
        {
            client.Player.GotoAckReceived();
        }
    }
}