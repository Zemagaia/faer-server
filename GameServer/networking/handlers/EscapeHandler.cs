using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;

namespace GameServer.networking.handlers
{
    class EscapeHandler : PacketHandlerBase<Escape>
    {
        public override PacketId ID => PacketId.ESCAPE;

        protected override void HandlePacket(Client client, Escape packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(t, client));
        }

        private void Handle(RealmTime time, Client client)
        {
            if (client.Player == null || client.Player.Owner == null)
                return;

            var realm = client.Player.Owner as Realm;
            if (realm is null)
            {
                client.Reconnect(new Reconnect()
                {
                    Host = "",
                    Port = 2050,
                    GameId = World.Realm,
                    Name = "Realm",
                });
                return;
            }

            if (!client.Player.TPCooledDown())
            {
                client.Player.SendError("Teleport in cooldown");
                return;
            }
            
            var rand = new Random();
            var sPoints = realm.GetSpawnPoints();
            var pos = sPoints[rand.Next(sPoints.Length)].Key;
            client.Player.TeleportPosition(time, pos.X + 0.5f, pos.Y + 0.5f, removeNegative: true);
            client.Player.SendInfo("Teleporting to spawn...");
        }
    }
}