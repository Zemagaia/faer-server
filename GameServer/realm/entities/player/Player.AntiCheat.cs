using common;
using GameServer.networking.packets.incoming;
using NLog;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        internal const int MaxToleranceMs = 1000;
        private static readonly Logger CheatLog = LogManager.GetLogger("CheatLog");

        // hit
        public long AcLastHitTime;
        public int AcShotsHit;
        public int AcMissedShots;
        private int _acLastMissReset;
        // shoot
        public long AcClientLastShot;
        public int AcShotNum;
        // move
        public bool[] AcIgnoreLastMove = new bool[256];
        public byte AcLastMoveId;
        public Position[] AcLastMove = new Position[256];
        public int AcMoveInfractions;
        private int _acMoveRefreshTime;

        public double DexRateOfFire()
        {
            var dex = Stats[5] <= 384 ? Stats[5] : 384;
            if (HasConditionEffect(ConditionEffects.Crippled))
                return 0.0015;

            var rof = 0.0015 + (dex / 75.0) * (0.008 - 0.0015);

            if (HasConditionEffect(ConditionEffects.Berserk))
                rof = rof * 1.5;

            return rof;
        }

        public bool IsNoClipping()
        {
            if (Owner == null || !TileOccupied(RealX, RealY) && !TileFullOccupied(RealX, RealY))
                return false;

            CheatLog.Info($"{Name} is walking on an occupied tile.");
            return true;
        }

        public bool IsInvalidTime(long serverTime, int clientTime)
        {
            var estimatedServerTime = C2STime(clientTime);
            return estimatedServerTime > serverTime + MaxToleranceMs || estimatedServerTime - MaxToleranceMs > serverTime;
        }

        public void DequeueShotSync(RealmTime time, ShootAck packet)
        {
            if (IsInvalidTime(time.TotalElapsedMs, packet.Time))
            {
                Client.Disconnect("Invalid Sync time");
                return;
            }

            var shots = _shoot[0];
            for (var i = 0; i < shots.Value.Length; i++)
            {
                var proj = Owner.GetProjectile(Id, shots.Value[i]);
                if (proj == null)
                    continue;
                proj.OverrideCreationTime(packet.Time);
            }

            ShootAckReceived();
        }
    }
}