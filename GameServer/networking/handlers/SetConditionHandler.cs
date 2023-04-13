using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class SetConditionHandler : PacketHandlerBase<SetCondition>
    {
        public override PacketId ID => PacketId.SETCONDITION;

        protected override void HandlePacket(Client client, SetCondition packet)
        {
            //TODO: implement something
        }
    }
}