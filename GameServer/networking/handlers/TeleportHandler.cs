using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class TeleportHandler : PacketHandlerBase<Teleport>
    {
        public override PacketId ID => PacketId.TELEPORT;

        protected override void HandlePacket(Client client, Teleport packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet.ObjectId));
        }

        void Handle(Player player, RealmTime time, int objId)
        {
            if (player == null || player.Owner == null)
                return;

            player.Teleport(time, objId);
        }
    }
}