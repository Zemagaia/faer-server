using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class OtherHitHandler : PacketHandlerBase<OtherHit>
    {
        public override PacketId ID => PacketId.OTHERHIT;

        protected override void HandlePacket(Client client, OtherHit packet)
        {
        }
    }
}