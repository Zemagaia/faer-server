using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;

namespace GameServer.networking.handlers
{
    class CreateGuildHandler : PacketHandlerBase<CreateGuild>
    {
        public override PacketId ID => PacketId.CREATEGUILD;

        protected override void HandlePacket(Client client, CreateGuild packet)
        {
            //client.Manager.Logic.AddPendingAction(t => Handle(client, packet.Name));
            Handle(client, packet.Name);
        }

        private void Handle(Client client, string name)
        {
            if (client.Player == null || IsTest(client))
                return;

            var acc = client.Account;

            if (acc.Fame < 50000)
            {
                SendError(client, "Insufficient funds");
                return;
            }

            if (!acc.NameChosen)
            {
                SendError(client, "Must pick a character name\nbefore creating a guild");
                return;
            }

            if (acc.GuildId > 0)
            {
                SendError(client, "Already in a guild");
                return;
            }

            DbGuild guild;
            var guildResult = client.Manager.Database.CreateGuild(name, out guild);
            if (guildResult != GuildCreateStatus.OK)
            {
                SendError(client, guildResult.ToString());
                return;
            }

            var addResult = client.Manager.Database.AddGuildMember(guild, acc, true);
            if (addResult != AddGuildMemberStatus.OK)
            {
                SendError(client, addResult.ToString());
                return;
            }

            client.Manager.Database.UpdateFame(acc, -50000);
            client.Player.CurrentFame = acc.Fame;
            client.Player.Guild = guild.Name;
            client.Player.GuildRank = 40;
            SendSuccess(client);
        }

        private void SendSuccess(Client client)
        {
            client.SendPacket(new GuildResult()
            {
                Success = true,
                LineBuilderJSON = "Success!"
            });
        }

        private void SendError(Client client, string message = null)
        {
            client.SendPacket(new GuildResult()
            {
                Success = false,
                LineBuilderJSON = "Guild Creation Error: " + message
            });
        }
    }
}