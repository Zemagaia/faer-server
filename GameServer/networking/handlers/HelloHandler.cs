using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using NLog;

namespace GameServer.networking.handlers
{
    class HelloHandler : PacketHandlerBase<Hello>
    {
        private new static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override PacketId ID => PacketId.HELLO;

        protected override void HandlePacket(Client client, Hello packet)
        {
            //client.Manager.Logic.AddPendingAction(t => Handle(client, packet));
            Handle(client, packet);
        }

        private void Handle(Client client, Hello packet)
        {
            var reconnecting = client.State == ProtocolState.Reconnecting;
            if (!reconnecting)
            {
                // get acc info
                client.Manager.Database.Verify(packet.GUID, packet.Password, out var acc);
                if (acc == null)
                    return;

                client.Manager.Database.LogAccountByIp(client.IP, acc.AccountId);
                acc.IP = client.IP;
                acc.FlushAsync();
                client.Account = acc;
            }

            // log ip
            if (!VerifyConnection(client, packet, client.Account))
                return;

            client.Manager.ConMan.Add(new ConInfo(client, packet, reconnecting));
        }

        private bool VerifyConnection(Client client, Hello packet, DbAccount acc)
        {
            var version = client.Manager.Config.serverSettings.version;
            if (!version.Equals(packet.BuildVersion))
            {
                client.SendFailure(version, Failure.ClientUpdateNeeded);
                return false;
            }

            if (acc.Banned)
            {
                client.SendFailure("Account banned.", Failure.MessageWithDisconnect);
                Log.Info("{0} ({1}) tried to log in. Account Banned.",
                    acc.Name, client.IP);
                return false;
            }

            if (client.Manager.Database.IsIpBanned(client.IP))
            {
                client.SendFailure("IP banned.", Failure.MessageWithDisconnect);
                Log.Info("{0} ({1}) tried to log in. IP Banned.",
                    acc.Name, client.IP);
                return false;
            }

            if (!acc.Admin && client.Manager.Config.serverInfo.adminOnly)
            {
                client.SendFailureDialog("Admin Only Server",
                    $"Only admins can play on {client.Manager.Config.serverInfo.name}.");
                return false;
            }

            var minRank = client.Manager.Config.serverInfo.minRank;
            if (acc.Rank < minRank)
            {
                client.SendFailureDialog("Rank Required Server",
                    $"You need a minimum server rank of {minRank} to play on {client.Manager.Config.serverInfo.name}.");
                return false;
            }

            var lootBoost = Constants.GlobalLootBoost;
            var xpBoost = Constants.GlobalXpBoost;
            var allEventsActive = Constants.GlobalLootBoost > 1f && Constants.GlobalXpBoost > 1f;
            if (DateTime.UtcNow.ToUnixTimestamp() < Constants.EventEnds.ToUnixTimestamp())
            {
                client.SendFailure2(
                    $"There {(allEventsActive ? "are currently events of" : "is currently an event of")}" +
                    $" {(lootBoost > 1f ? lootBoost + "x Loot Boost" : "")}{(allEventsActive ? " and " : "")}{(xpBoost > 1f ? xpBoost + "x XP Boost" : "")}" +
                    $". Ends at {Constants.EventEnds} UTC", Failure.DefaultFailure2);
            }

            return true;
        }
    }
}