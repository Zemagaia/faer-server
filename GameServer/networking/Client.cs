using System.Net;
using System.Net.Sockets;
using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;
using GameServer.networking.server;
using GameServer.realm;
using GameServer.realm.entities.player;
using GameServer.realm.worlds.logic;
using NLog;
using Newtonsoft.Json;

namespace GameServer.networking
{
    public enum ProtocolState
    {
        Disconnected,
        Connected,
        Handshaked,
        Queued,
        Ready,
        Reconnecting
    }

    public partial class Client
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public RealmManager Manager { get; private set; }
        
        private readonly Server _server;
        private readonly CommHandler _handler;

        private volatile ProtocolState _state;

        public ProtocolState State
        {
            get { return _state; }
            internal set { _state = value; }
        }

        public int Id { get; internal set; }
        public DbAccount Account { get; internal set; }
        public DbChar Character { get; internal set; }
        public Player Player { get; internal set; }

        public WRandom Random { get; internal set; }

        //Temporary connection state
        internal int TargetWorld = -1;

        public Socket Skt { get; private set; }
        public string IP { get; private set; }
        public bool IsLagging { get; private set; }

        internal readonly object DcLock = new();

        public Client(Server server, RealmManager manager,
            SocketAsyncEventArgs send, SocketAsyncEventArgs receive)
        {
            _server = server;
            Manager = manager;

            _handler = new CommHandler(this, send, receive);
        }

        public void Reset()
        {
            Id = 0; // needed so that inbound packets that are currently queued are discarded.
            Account = null;
            Character = null;
            Player = null;

            // reset client ping/pong values
            _pingTime = -1;
            _pongTime = -1;

            _handler.Reset();
        }

        public void BeginHandling(Socket skt)
        {
            Skt = skt;

            try
            {
                IP = ((IPEndPoint)skt.RemoteEndPoint).Address.ToString();
            }
            catch (Exception)
            {
                IP = "";
            }

            Log.Info("Received client @ {0}.", IP);
            _handler.BeginHandling(Skt);
        }

        public void SendPacket(Packet pkt)
        {
            lock (DcLock)
            {
                if (State != ProtocolState.Disconnected)
                    _handler.SendPacket(pkt);
            }
        }

        public void SendPackets(IEnumerable<Packet> pkts)
        {
            lock (DcLock)
            {
                if (State != ProtocolState.Disconnected)
                    _handler.SendPackets(pkts);
            }
        }

        public bool IsReady()
        {
            if (State == ProtocolState.Disconnected)
                return false;

            if (State == ProtocolState.Ready && Player?.Owner == null)
                return false;

            return true;
        }

        public bool CheckForLag()
        {
            IsLagging = _handler.IsLagging();
            return IsLagging;
        }

        internal void ProcessPacket(Packet pkt)
        {
            lock (DcLock)
            {
                if (State == ProtocolState.Disconnected)
                    return;

                try
                {
                    //Log.Trace($"Handling packet '{pkt.ID}'...");

                    IPacketHandler handler;
                    if (!PacketHandlers.Handlers.TryGetValue(pkt.ID, out handler))
                        Log.Warn("Unhandled packet '{0}'.", pkt.ID);
                    else
                        handler.Handle(this, (IncomingMessage)pkt);
                }
                catch (Exception e)
                {
                    Log.Error($"Error when handling packet '{pkt.ToString()}'...", e);
                    Disconnect("Packet handling error.");
                }
            }
        }

        public void Reconnect(Reconnect pkt)
        {
            if (State == ProtocolState.Reconnecting) return;

            if (Account == null)
            {
                Disconnect("Tried to reconnect an client with a null account...");
                return;
            }

            Log.Info("Reconnecting client ({0}) @ {1} to {2}...", Account.Name, IP, pkt.Name);

            State = ProtocolState.Reconnecting;
            Save(false);

            Manager.ConMan.AddReconnect(Account.AccountId, pkt);
            SendPacket(pkt);

            if (Player.PrevSkin != Player.Skin)
            {
                Player.SetDefaultSkin(Player.PrevSkin);
                Player.SetDefaultSize(Player.PrevSize);
            }
        }

        public async void SendFailure(string text, int errorId = 0)
        {
            SendPacket(new Failure()
            {
                ErrorId = errorId,
                ErrorDescription = text
            });

            var t = Task.Delay(1000);
            await t;

            Disconnect();
        }

        public void SendFailure2(string text, int errorId = 0)
        {
            SendPacket(new Failure()
            {
                ErrorId = errorId,
                ErrorDescription = text
            });
        }

        public async void SendFailureDialog(string title, string description)
        {
            // Note: Client is programmed to check the build parameter
            // of the json object. If it doesn't match what the client
            // has, the error dialog will be an update client dialog
            // instead.

            var jsonMsg = new FailureJsonDialogMessage()
            {
                build = Manager.Config.serverSettings.version,
                title = title,
                description = description
            };
            SendPacket(new Failure()
            {
                ErrorId = Failure.JsonDialogDisconnect,
                ErrorDescription = JsonConvert.SerializeObject(jsonMsg)
            });

            var t = Task.Delay(1000);
            await t;

            Disconnect();
        }

        public void Disconnect(string reason = "")
        {
            lock (DcLock)
            {
                if (State == ProtocolState.Disconnected)
                    return;

                State = ProtocolState.Disconnected;

                if (!string.IsNullOrEmpty(reason))
                    Log.Info("Disconnecting client ({0}) @ {1}... {2}",
                        Account?.Name ?? " ", IP, reason);

                if (Account != null)
                    try
                    {
                        Save(true);
                    }
                    catch (Exception e)
                    {
                        var msg = $"{e.Message}\n{e.StackTrace}";
                        Log.Error(msg);
                    }

                Manager.Disconnect(this);
                _server.Disconnect(this);
            }
        }

        private void Save(bool unlock)
        {
            var acc = Account;

            if (Character == null || Player == null || Player.Owner is Test)
            {
                if (unlock)
                    Manager.Database.ReleaseLock(acc);
                return;
            }

            for (var i = 0; i < Player.PassiveCooldown.Length; i++)
            {
                Player.PassiveCooldown[i] = 0;
            }

/*#if DEBUG
            Console.WriteLine($"{Player.PrevSkin} prev skin, {Player.Skin} skin\n{Player.PrevSize} prev size, {Player.Client.Account.Size} size");
#endif*/

            if (Player.PrevSkin != Player.Skin)
            {
                Player.SetDefaultSkin(Player.PrevSkin);
                Player.SetDefaultSize(Player.PrevSize);
            }

            Player.SaveToCharacter();
            if (!acc.Hidden && acc.AccountIdOverrider == 0)
                acc.RefreshLastSeen();
            acc.FlushAsync();
            if (unlock)
                Manager.Database.ReleaseLock(acc);
            Manager.Database.SaveCharacter(acc, Character, Player.FameCounter.ClassStats, !unlock);
        }

        public void Dispose()
        {
            // nothing to do here
        }
    }
}