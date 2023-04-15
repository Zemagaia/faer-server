using System.Reflection;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        private readonly object _skillTreeLock = new();

        public void UseOffensiveAbility(RealmTime time, int clientTime, Position target, float angle)
        {
            // no offensive ability
            if (OffensiveAbility == 0)
            {
                return;
            }

            lock (_skillTreeLock)
            {
                if (SpectateTarget != null)
                {
                    return;
                }

                if (OnCooldown(TreeOffensive))
                {
                    SendError(
                        $"Offensive ability on cooldown for {Math.Round((float)PassiveCooldown[TreeOffensive] / 1000, 1)}s");
                    Client.SendInvResult(1);
                    return;
                }

                GetOffensiveAbilities(target, angle);
            }
        }

        public void UseDefensiveAbility(RealmTime time, int clientTime, Position target, float angle)
        {
            // no defensive ability
            if (DefensiveAbility == 0)
            {
                return;
            }

            lock (_skillTreeLock)
            {
                if (SpectateTarget != null)
                {
                    return;
                }

                if (OnCooldown(TreeDefensive))
                {
                    SendError(
                        $"Defensive ability on cooldown for {Math.Round((float)PassiveCooldown[TreeDefensive] / 1000, 1)}s");
                    Client.SendInvResult(1);
                    return;
                }

                GetDefensiveAbilities(target, angle);
            }
        }
        
        public void GetOffensiveAbilities(Position target, float angle)
        {
            var name = Enum.GetName(typeof(OffensiveAbilities), OffensiveAbility);
            if (name == null)
            {
                return;
            }

            var type = GetType();
            var method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, null);
        }

        public void GetDefensiveAbilities(Position target, float angle)
        {
            var name = Enum.GetName(typeof(DefensiveAbilities), DefensiveAbility);
            if (name == null)
            {
                return;
            }

            var type = GetType();
            var method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, null);
        }
    }
}