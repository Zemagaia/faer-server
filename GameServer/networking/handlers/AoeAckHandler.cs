using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class AoeAckHandler : PacketHandlerBase<AoeAck>
    {
        public override PacketId ID => PacketId.AOEACK;

        protected override void HandlePacket(Client client, AoeAck packet)
        {
            //TODO: implement something
        }
    }
}