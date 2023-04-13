using GameServer.networking.packets;
using GameServer.networking.packets.incoming.skillTree.abilities;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers.skillTree.abilities
{
    class DefensiveAbilityHandler : PacketHandlerBase<DefensiveAbility>
    {
        public override PacketId ID => PacketId.DEFENSIVE_ABILITY;

        protected override void HandlePacket(Client client, DefensiveAbility packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet));
        }

        void Handle(Player player, RealmTime time, DefensiveAbility packet)
        {
            if (player?.Owner == null)
                return;

            player.UseDefensiveAbility(time, packet.Time, packet.UsePos, packet.Angle);
        }
    }
}