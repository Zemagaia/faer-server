using System.Collections.Concurrent;

namespace GameServer.realm.entities.player
{
    public partial class Player
    {
        private const int PingPeriod = 3000;
        public const int DcThresold = 12000;

        private long _pingTime = -1;
        private long _pongTime = -1;

        private int _cnt;

        private long _sum;
        public long TimeMap { get; private set; }

        private long _latSum;
        public int Latency { get; private set; }

        public int LastClientTime = -1;
        public long LastServerTime = -1;

        private readonly List<KeyValuePair<long, byte[]>> _shoot = new();
        private readonly ConcurrentQueue<long> _updateAckTimeout = new();
        private readonly ConcurrentQueue<long> _gotoAckTimeout = new();
        private readonly ConcurrentQueue<int> _move = new();
        private readonly ConcurrentQueue<int> _clientTimeLog = new();
        private readonly ConcurrentQueue<int> _serverTimeLog = new();

        bool KeepAlive(RealmTime time) {
            /*
            if (_pingTime == -1)
            {
                _pingTime = time.TotalElapsedMs - PingPeriod;
                _pongTime = time.TotalElapsedMs;
            }

            // check for disconnect timeout
            if (time.TotalElapsedMs - _pongTime > DcThresold)
            {
                _client.Disconnect("Connection timeout. (KeepAlive)");
                return false;
            }

            // check for shootack timeout
            var shoot = _shoot;
            if (shoot.Count > 0)
            {
                if (time.TotalElapsedMs > shoot[0].Key)
                {
                    _client.Disconnect("Connection timeout. (ShootAck)");
                    return false;
                }
            }

            long timeout;
            // check for updateack timeout
            if (_updateAckTimeout.TryPeek(out timeout))
            {
                if (time.TotalElapsedMs > timeout)
                {
                    _client.Disconnect("Connection timeout. (UpdateAck)");
                    return false;
                }
            }

            // check for gotoack timeout
            if (_gotoAckTimeout.TryPeek(out timeout))
            {
                if (time.TotalElapsedMs > timeout)
                {
                    _client.Disconnect("Connection timeout. (GotoAck)");
                    return false;
                }
            }

            if (time.TotalElapsedMs - _pingTime < PingPeriod)
                return true;

            // send ping
            _pingTime = time.TotalElapsedMs;
            _client.SendPacket(new Ping()
            {
                Serial = (int)time.TotalElapsedMs
            });*/
            return UpdateOnPing();
        }

        public void Pong(RealmTime time, int pongTime, int serial)
        {
            _cnt++;

            _sum += time.TotalElapsedMs - pongTime;
            TimeMap = _sum / _cnt;

            _latSum += (time.TotalElapsedMs - serial) / 2;
            Latency = (int)_latSum / _cnt;

            _pongTime = time.TotalElapsedMs;
        }

        private bool UpdateOnPing()
        {
            // renew account lock
            try
            {
                if (!Manager.Database.RenewLock(_client.Account))
                    _client.Disconnect("RenewLock failed. (Pong)");
            }
            catch
            {
                _client.Disconnect("RenewLock failed. (Timeout)");
                return false;
            }

            return true;
        }

        public long C2STime(int clientTime)
        {
            return clientTime + TimeMap;
        }

        public long S2CTime(int serverTime)
        {
            return serverTime - TimeMap;
        }

        public void AwaitShootAck(long serverTime, byte[] shots)
        {
            _shoot.Add(new KeyValuePair<long, byte[]>(serverTime + DcThresold, shots));
        }

        public void ShootAckReceived()
        {
            if (_shoot.Count == 0)
            {
                _client.Disconnect("One too many ShootAcks");
                return;
            }
            
            _shoot.RemoveAt(0);
        }

        public void AwaitUpdateAck(long serverTime)
        {
            _updateAckTimeout.Enqueue(serverTime + DcThresold);
        }

        public void UpdateAckReceived()
        {
            long ignored;
            if (!_updateAckTimeout.TryDequeue(out ignored))
            {
                _client.Disconnect("One too many UpdateAcks");
            }
        }

        public void AwaitGotoAck(long serverTime)
        {
            _gotoAckTimeout.Enqueue(serverTime + DcThresold);
        }

        public void GotoAckReceived()
        {
            long ignored;
            if (!_gotoAckTimeout.TryDequeue(out ignored))
            {
                _client.Disconnect("One too many GotoAcks");
            }
        }

        public void AwaitMove(int tickId)
        {
            _move.Enqueue(tickId);
        }

        public void MoveReceived(RealmTime time, int clientTickId, int clientTime)
        {
            /*if (!_move.TryDequeue(out var tickId))
            {
                _client.Disconnect("One too many MovePackets");
                return;
            }

            if (tickId != clientTickId)
            {
                _client.Disconnect("[NewTick -> Move] TickIds don't match");
                return;
            }

            if (clientTickId > TickId)
            {
                _client.Disconnect("[NewTick -> Move] Invalid tickId");
                return;
            }

            var lastClientTime = LastClientTime;
            var lastServerTime = LastServerTime;
            LastClientTime = clientTime;
            LastServerTime = time.TotalElapsedMs;

            if (lastClientTime == -1)
                return;

            _clientTimeLog.Enqueue(pkt.Time - lastClientTime);
            _serverTimeLog.Enqueue((int)(time.TotalElapsedMs - lastServerTime));

            if (_clientTimeLog.Count < 30)
                return;

            if (_clientTimeLog.Count > 30)
            {
                int ignore;
                _clientTimeLog.TryDequeue(out ignore);
                _serverTimeLog.TryDequeue(out ignore);
            }

            // calculate average
            var clientDeltaAvg = _clientTimeLog.Sum() / _clientTimeLog.Count;
            var serverDeltaAvg = _serverTimeLog.Sum() / _serverTimeLog.Count;
            var dx = clientDeltaAvg > serverDeltaAvg
                ? clientDeltaAvg - serverDeltaAvg
                : serverDeltaAvg - clientDeltaAvg;
            if (dx > 15)
            {
                Log.Debug(
                    $"TickId: {tickId}, Client Delta: {_clientTimeLog.Sum() / _clientTimeLog.Count}, Server Delta: {_serverTimeLog.Sum() / _serverTimeLog.Count}");
            }*/
        }
    }
}