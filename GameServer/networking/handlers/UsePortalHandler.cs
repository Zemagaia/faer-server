using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.worlds.logic;

namespace GameServer.networking.handlers
{
    class UsePortalHandler : PacketHandlerBase<UsePortal>
    {
        private readonly int[] _realmPortals = new int[] { 0x0100, 0x0101 };

        public override PacketId ID => PacketId.USEPORTAL;

        protected override void HandlePacket(Client client, UsePortal packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client, packet));
            //Handle(client, packet);
        }

        private void Handle(Client client, UsePortal packet)
        {
            var player = client.Player;
            if (player?.Owner == null || IsTest(client))
                return;

            var entity = player.Owner.GetEntity(packet.ObjectId);
            if (entity == null) return;

            if (entity is GuildHallPortal)
            {
                HandleGuildPortal(player, entity as GuildHallPortal);
                return;
            }

            HandlePortal(player, entity as Portal);
        }

        private void HandleGuildPortal(Player player, GuildHallPortal portal)
        {
            if (string.IsNullOrEmpty(player.Guild))
            {
                player.SendError("You are not in a guild.");
                return;
            }

            if (portal.ObjectType == 0x0439)
            {
                var proto = player.Manager.Resources.Worlds["GuildHall"];
                var world = player.Manager.GetWorld(proto.id);
                player.Reconnect(world.GetInstance(player.Client));
                return;
            }

            player.SendInfo("Portal not implemented.");
        }

        private void HandlePortal(Player player, Portal portal)
        {
            if (portal == null || !portal.Usable)
                return;

            lock (portal.CreateWorldLock)
            {
                var world = portal.WorldInstance;

                // special portal case lookup
                if (world == null && _realmPortals.Contains(portal.ObjectType))
                {
                    world = player.Manager.GetGameWorld(player.Client);
                    if (world == null)
                        return;
                }

                if (world is Realm && !player.Manager.Resources.GameData.ObjectTypeToId[portal.ObjectDesc.ObjectType]
                    .Contains("Cowardice"))
                {
                    player.FameCounter.CompleteDungeon(player.Owner.Name);
                    int i;
                    int j;
                    var quests = player.CharacterQuests;
                    for (i = 0; i < quests.Length; i++)
                    for (j = 0; j < quests[i].Dungeons.Length; j++)
                    {
                        if (quests[i].DungeonsCompleted[j] == quests[i].DungeonAmounts[j] ||
                            quests[i].Dungeons[j] != player.Owner.SBName)
                        {
                            continue;
                        }

                        quests[i].DungeonsCompleted[j]++;
                        if (quests[i].DungeonsCompleted[j] >= quests[i].DungeonAmounts[j])
                        {
                            quests[i].Goals[0]++;
                        }
                    }

                    quests = player.Client.Account.AccountQuests;
                    for (i = 0; i < quests.Length; i++)
                    for (j = 0; j < quests[i].Dungeons.Length; j++)
                    {
                        if (quests[i].DungeonsCompleted[j] == quests[i].DungeonAmounts[j] ||
                            quests[i].Dungeons[j] != player.Owner.SBName)
                        {
                            continue;
                        }

                        quests[i].DungeonsCompleted[j]++;
                        if (quests[i].DungeonsCompleted[j] >= quests[i].DungeonAmounts[j])
                        {
                            quests[i].Goals[0]++;
                        }
                    }

                    player.Client.Account.AccountQuests = quests;
                }

                if (world != null)
                {
                    player.Reconnect(world);

                    if (portal.WorldInstance?.Invites != null)
                    {
                        portal.WorldInstance.Invites.Remove(player.Name.ToLower());
                    }

                    if (portal.WorldInstance?.InviteDict != null)
                    {
                        portal.WorldInstance.InviteDict.Add(player.Name.ToLower(), null);
                    }

                    return;
                }

                // dynamic case lookup
                if (portal.CreateWorldTask == null || portal.CreateWorldTask.IsCompleted)
                    portal.CreateWorldTask = Task.Factory
                        .StartNew(() => portal.CreateWorld(player))
                        .ContinueWith(e =>
                                Log.Error(e.Exception.InnerException.ToString()),
                            TaskContinuationOptions.OnlyOnFaulted);

                portal.WorldInstanceSet += player.Reconnect;
            }
        }
    }
}