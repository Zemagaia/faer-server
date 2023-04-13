using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class UseItemHandler : PacketHandlerBase<UseItem>
    {
        public override PacketId ID => PacketId.USEITEM;

        protected override void HandlePacket(Client client, UseItem packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet));
        }

        void Handle(Player player, RealmTime time, UseItem packet)
        {
            if (player?.Owner == null)
                return;

            player.UseItem(time, packet.SlotObject.ObjectId, packet.SlotObject.SlotId, packet.ItemUsePos,
                packet.ActivateId, packet.Time);
        }
    }
}