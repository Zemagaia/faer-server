namespace GameServer.realm.entities.player
{
    partial class Player
    {
        private enum OffensiveAbilities
        {
            SkeletonSpawn = -1,
        }

        private enum DefensiveAbilities
        {
            MagicShield = -1,
        }
        
        private void SkeletonSpawn()
        {
            SpawnAlly(new Position { X = X, Y = Y }, "Brute Skeleton", 10, forceSpawn: true);
            SetOffensiveCooldown(20);
            ShowNotification("Skeleton Spawn", 0xCCCCCC);
        }

        private void MagicShield()
        {
            StatBoostSelf(12, Stats.Base[1] / 2, 10, true);
            SetDefensiveCooldown(20);
            ShowNotification("Magic Shield", 0x87CEEB);
            Owner.Timers.Add(new WorldTimer(500, (_, _) => ShowNotification($"+{Stats.Base[1] / 2} Shield", 0x87CEEB)));
        }
    }
}