using common;
using common.resources;
using GameServer.logic.loot;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        internal const int PassiveSlot = 4;
        internal const int TreeOffensive = 5;
        internal const int TreeDefensive = 6;
        public int[] PassiveCooldown = new int[7];

        // Checked every ~250ms
        public void RegularPassives()
        {
            for (var i = 0; i < 6; i++)
            {
                var item = Inventory[i].Item;
                if (item == null)
                    continue;
                ActivateRegularPowers(item.Power);
            }
        }

        // Tick passives
        private void ActivateRegularPowers(string power)
        {
            // gets the power name
            switch (power)
            {
                case "Poison Coat":
                    if (HasConditionEffect(ConditionEffects.Bleeding))
                        ApplyConditionEffect(ConditionEffectIndex.Bleeding, 0);
                    return;
                default: return;
            }
        }
        
        public void SetCooldown(int slot, float sec)
        {
            if (slot > 3 || sec < 0)
            {
                Log.Debug($"Invalid slot ({slot}) or duration ({sec}) for ability");
                return;
            }

            PassiveCooldown[slot] = (int)(sec * 1000);
        }

        public bool OnCooldown(int slot)
        {
            if (slot > PassiveCooldown.Length)
            {
                Log.Debug($"Attempted to check cooldown on ability slot: {slot}");
                return false;
            }
            
            if (PassiveCooldown[slot] <= 0)
                return false;

            return true;
        }

        public void SetCooldown(Item item, float sec)
        {
            if (sec < 0)
            {
                Log.Debug($"{item.ObjectId} has improper cooldown for passive ability: {sec}");
                return;
            }

            var slot = GetSlot(item);
            PassiveCooldown[slot] = (int)(sec * 1000);
        }

        public bool OnCooldown(Item item)
        {
            var slot = GetSlot(item);

            if (PassiveCooldown[slot] <= 0)
                return false;

            return true;
        }

        private int GetSlot(Item item)
        {
            // default to weapon slot
            if (TierLoot.AbilityT.Contains(item.SlotType))
                return 1;
            if (TierLoot.ArmorT.Contains(item.SlotType))
                return 2;
            if (TierLoot.RingT.Contains(item.SlotType))
                return 3;
            return 0;
        }

        public void SetOffensiveCooldown(float sec)
        {
            PassiveCooldown[TreeOffensive] = (int)(sec * 1000);
        }

        public void SetDefensiveCooldown(float sec)
        {
            PassiveCooldown[TreeDefensive] = (int)(sec * 1000);
        }
    }
}