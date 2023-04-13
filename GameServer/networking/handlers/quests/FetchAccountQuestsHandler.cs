using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.quests;
using GameServer.networking.packets.outgoing.quests;

namespace GameServer.networking.handlers.quests
{
    class FetchAccountQuestsHandler : PacketHandlerBase<FetchAccountQuests>
    {
        public override PacketId ID => PacketId.FETCH_ACCOUNT_QUESTS;

        protected override void HandlePacket(Client client, FetchAccountQuests packet)
        {
            client.Manager.Logic.AddPendingAction(t =>
            {
                if (client.Player == null || IsTest(client))
                {
                    return;
                }

                if (client.Account.AccountQuests.Length == 0)
                {
                    client.SendPacket(new FetchAccountQuestsResult
                    {
                        Results = new AcceptedQuestData[0],
                        Description = $"You do not have any account quests"
                    });
                    return;
                }

                client.SendPacket(new FetchAccountQuestsResult
                {
                    Results = client.Account.AccountQuests.ToArray(),
                    Description = ""
                });
            });
        }
    }
}