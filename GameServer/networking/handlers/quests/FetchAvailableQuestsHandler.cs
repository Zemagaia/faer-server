using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.quests;
using GameServer.networking.packets.outgoing.quests;
using GameServer.realm.worlds;

namespace GameServer.networking.handlers.quests
{
    class FetchQuestsHandler : PacketHandlerBase<FetchAvailableQuests>
    {
        public override PacketId ID => PacketId.FETCH_AVAILABLE_QUESTS;

        protected override void HandlePacket(Client client, FetchAvailableQuests packet)
        {
            client.Manager.Logic.AddPendingAction(t =>
            {
                var player = client.Player;
                if (player == null || IsTest(client))
                {
                    return;
                }

                // no quests or world is not quest world (useless?... but this is only for fetching, why would you need it?)
                if (player.AvailableQuests.Length == 0 || player.Owner.Id != World.Tinker && player.Rank < 100)
                {
                    client.SendPacket(new FetchAvailableQuestsResult
                    {
                        Results = new QuestData[0],
                        Description = $"You do not have any available quests"
                    });
                    return;
                }

                client.SendPacket(new FetchAvailableQuestsResult
                {
                    Results = player.AvailableQuests.ToArray(),
                    Description = ""
                });
            });
        }
    }
}