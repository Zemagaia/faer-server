using common;
using common.resources;
using GameServer.realm.entities.player;

namespace GameServer.realm
{
    class BoostStatManager
    {
        private readonly StatsManager _parent;
        private readonly Player _player;
        private readonly SV<int>[] _boostSV;
        private readonly int[] _boost;
        private readonly ActivateBoost[] _activateBoost;

        public ActivateBoost[] ActivateBoost => _activateBoost;
        public int this[int index] => _boost[index];

        public BoostStatManager(StatsManager parent)
        {
            _parent = parent;
            _player = parent.Owner;

            _boost = new int[StatsManager.NumStatTypes];
            _boostSV = new SV<int>[_boost.Length];
            for (var i = 0; i < _boostSV.Length; i++)
                _boostSV[i] = new SV<int>(_player, StatsManager.GetBoostStatType(i), _boost[i], i != 0 && i != 1);
            _activateBoost = new ActivateBoost[_boost.Length];
            for (int i = 0; i < _activateBoost.Length; i++)
                _activateBoost[i] = new ActivateBoost();
            ReCalculateValues();
        }

        protected internal void ReCalculateValues(InventoryChangedEventArgs e = null)
        {
            for (var i = 0; i < _boost.Length; i++)
                _boost[i] = 0;

            ApplyEquipBonus(e);
            ApplySetBonus(e);
            ApplyActivateBonus(e);

            for (var i = 0; i < _boost.Length; i++)
                _boostSV[i].SetValue(_boost[i]);
        }

        private void ApplyEquipBonus(InventoryChangedEventArgs e)
        {
            for (var i = 0; i < 6; i++)
            {
                if (_player.Inventory[i].Item == null)
                    continue;

                int value;
                foreach (var boost in _player.Inventory[i].Item.StatsBoost)
                {
                    value = boost.Value;
                    if (value < 0)
                        value = (int)Math.Floor((double) (value / _player.Inventory[i].Quality));
                    else
                        value = (int)Math.Floor((double) (value * _player.Inventory[i].Quality));
                    IncrementBoost((StatsType)boost.Key, value);
                }

                if (_player.Inventory[i].StatBoosts != null)
                    foreach (var dBoost in _player.Inventory[i].StatBoosts)
                    {
                        value = dBoost.Value;
                        IncrementBoost((StatsType)dBoost.Key, value);
                    }


                float valuePerc;
                foreach (var pBoost in _player.Inventory[i].Item.StatsBoostPerc)
                {
                    valuePerc = pBoost.Value;
                    if (valuePerc < 0)
                        valuePerc = valuePerc / _player.Inventory[i].Quality;
                    else
                        valuePerc = valuePerc * _player.Inventory[i].Quality;
                    value = (int)(_parent.Base[StatsManager.GetStatIndex((StatsType)pBoost.Key)] * valuePerc);
                    IncrementBoost((StatsType)pBoost.Key, value);
                }
            }
        }

        private void ApplySetBonus(InventoryChangedEventArgs e)
        {
            var gameData = _player.Manager.Resources.GameData;
            foreach (var equipSet in gameData.EquipmentSets.Values)
            {
                var setEquipped = equipSet.Setpieces
                    .Where(piece => piece.Type.Equals("Equipment"))
                    .All(piece => (_player.Inventory[piece.Slot] == null && piece.ItemType == 0xFFFF) ||
                                  (_player.Inventory[piece.Slot] != null &&
                                   _player.Inventory[piece.Slot].ObjectType == piece.ItemType));

                if (setEquipped)
                {
                    // apply bonus
                    foreach (var ae in equipSet.ActivateOnEquipAll)
                    {
                        switch (ae.Effect)
                        {
                            case ActivateEffects.ChangeSkin:
                                _player.Skin = ae.SkinType;
                                _player.Size = ae.Size;
                                break;
                            case ActivateEffects.IncrementStat:
                                IncrementBoost((StatsType)ae.Stats, ae.Amount);
                                break;
                            case ActivateEffects.FixedStat:
                                FixedStat((StatsType)ae.Stats, ae.Amount);
                                break;
                            case ActivateEffects.ConditionEffectSelf:
                                _player.ApplyConditionEffect(ae.ConditionEffect.Value);
                                break;
                        }
                    }

                    return;
                }

                if (e == null)
                    continue;

                var setRemoved = equipSet.Setpieces
                    .Where(piece => piece.Type.Equals("Equipment"))
                    .All(piece => (e.OldItems[piece.Slot] == null && piece.ItemType == 0xFFFF) ||
                                  (e.OldItems[piece.Slot] != null &&
                                   e.OldItems[piece.Slot].ObjectType == piece.ItemType));

                if (setRemoved)
                {
                    foreach (var ae in equipSet.ActivateOnEquipAll)
                    {
                        // remove changes
                        switch (ae.Effect)
                        {
                            case ActivateEffects.ChangeSkin:
                                _player.RestoreDefaultSkin();
                                _player.RestoreDefaultSize();
                                break;
                            case ActivateEffects.ConditionEffectSelf:
                                _player.ApplyConditionEffect(ae.ConditionEffect.Value, 0);
                                break;
                        }
                    }
                }
            }
        }

        private void ApplyActivateBonus(InventoryChangedEventArgs e)
        {
            for (var i = 0; i < _activateBoost.Length; i++)
            {
                // set boost
                var b = _activateBoost[i].GetBoost();
                _boost[i] += b;

                if (i > 7 && i != 12 && i != 19 && i != 20)
                    continue;

                // set condition icon
                var idx = i + 31;
                switch (i)
                {
                    case 12:
                        idx = 42;
                        break;
                    case 19:
                        idx = 49;
                        break;
                    case 20:
                        idx = 50;
                        break;
                }
                var haveCondition = _player.HasConditionEffect((ConditionEffects)((ulong)1 << idx));
                if (b > 0)
                {
                    if (!haveCondition)
                        _player.ApplyConditionEffect(new ConditionEffect()
                        {
                            Effect = (ConditionEffectIndex)idx,
                            DurationMS = -1
                        });
                }
                else
                {
                    if (haveCondition)
                        _player.ApplyConditionEffect(new ConditionEffect()
                        {
                            Effect = (ConditionEffectIndex)idx,
                            DurationMS = 0
                        });
                }
            }
        }

        private void IncrementBoost(StatsType stat, int amount)
        {
            var i = StatsManager.GetStatIndex(stat);
            if (_parent.Base[i] + amount < 1)
            {
                amount = (i == 0) ? -_parent.Base[i] + 1 : -_parent.Base[i];
            }

            _boost[i] += amount;
        }

        private void FixedStat(StatsType stat, int value)
        {
            var i = StatsManager.GetStatIndex(stat);
            _boost[i] = value - _parent.Base[i];
        }
    }
}