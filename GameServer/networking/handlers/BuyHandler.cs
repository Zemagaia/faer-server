using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm.entities.player;
using GameServer.realm.entities.vendors;

namespace GameServer.networking.handlers
{
    class BuyHandler : PacketHandlerBase<Buy>
    {
        public override PacketId ID => PacketId.BUY;

        protected override void HandlePacket(Client client, Buy packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, packet.ObjectId));
            //Handle(client.Player, packet.ObjectId);
        }

        void Handle(Player player, int objId)
        {
            if (player?.Owner == null)
                return;

            var obj = player.Owner.GetEntity(objId) as SellableObject;
            obj?.Buy(player);
        }
    }
}