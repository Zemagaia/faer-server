using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class SquareHitHandler : PacketHandlerBase<SquareHit>
    {
        public override PacketId ID => PacketId.SQUAREHIT;

        protected override void HandlePacket(Client client, SquareHit packet)
        {
        }
    }
}