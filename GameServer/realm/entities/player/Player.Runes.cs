namespace GameServer.realm.entities.player
{
    partial class Player
    {
        public void AddRuneBoost(ushort objType, int slot)
        {
            // nah surely this wont be null
            var item = Manager.Resources.GameData.Items[objType];

            // should always have a boost but why not
            if (item.RuneBoosts is null)
            {
                Log.Warn($"\"{item.ObjectId}\" is a rune but has no boosts...");
                return;
            }

            var boosts = item.RuneBoosts;
            var inv = Inventory;
            // make damage boosts
            if (inv[slot].DamageBoosts is null)
                inv[slot].DamageBoosts = new[]
                {
                    boosts.PhysicalDmg, // 0
                    boosts.MagicalDmg, // 1
                    boosts.EarthDmg, // 2
                    boosts.AirDmg, // 3
                    boosts.ProfaneDmg, // 4
                    boosts.FireDmg, // 5
                    boosts.WaterDmg, // 6
                    boosts.HolyDmg // 7
                };
            else
            {
                inv[slot].DamageBoosts[0] += boosts.PhysicalDmg;
                inv[slot].DamageBoosts[1] += boosts.MagicalDmg;
                inv[slot].DamageBoosts[2] += boosts.EarthDmg;
                inv[slot].DamageBoosts[3] += boosts.AirDmg;
                inv[slot].DamageBoosts[4] += boosts.ProfaneDmg;
                inv[slot].DamageBoosts[5] += boosts.FireDmg;
                inv[slot].DamageBoosts[6] += boosts.WaterDmg;
                inv[slot].DamageBoosts[7] += boosts.HolyDmg;
            }

            for (var i = 0; i < boosts.StatsBoost.Length; i++)
            {
                // make stat boosts
                if (inv[slot].StatBoosts is null)
                {
                    inv[slot].StatBoosts = boosts.StatsBoost;
                    break;
                }

                // add rune boosts to stat boosts
                AddRuneBoostsToItemData(slot, boosts.StatsBoost[i]);
            }

            // as itemdata is updated, forceupdate on slot
            this.ForceUpdate(slot);
        }

        private void AddRuneBoostsToItemData(int slot, KeyValuePair<byte, short> boost)
        {
            var inv = Inventory;
            var dataBoosts = inv[slot].StatBoosts.ToList();
            for (var i = 0; i < dataBoosts.Count; i++)
            {
                if (dataBoosts[i].Key != boost.Key) continue;
                var key = dataBoosts[i].Key;
                var val = dataBoosts[i].Value;
                val += boost.Value;
                dataBoosts[i] = new KeyValuePair<byte, short>(key, val);
            }

            if (!dataBoosts.Exists(kvp => kvp.Key == boost.Key))
            {
                dataBoosts.Add(boost);
            }

            inv[slot].StatBoosts = dataBoosts.ToArray();
        }
    }
}