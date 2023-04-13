using GameServer.networking.packets;
using GameServer.networking.packets.incoming;

namespace GameServer.networking.handlers
{
    class CheckCreditsHandler : PacketHandlerBase<CheckCredits>
    {
        public override PacketId ID => PacketId.CHECKCREDITS;

        protected override void HandlePacket(Client client, CheckCredits packet)
        {
            //client.Manager.Logic.AddPendingAction(t => Handle(client));
            Handle(client);
        }

        void Handle(Client client)
        {
            var player = client.Player;
            if (player == null || IsTest(client))
                return;

            player.Credits = player.Client.Account.Credits;
        }
    }
}