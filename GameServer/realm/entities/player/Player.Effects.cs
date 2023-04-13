using common;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        float _healing;
        float _bleeding;

        int _newbieTime;
        int _canTpCooldownTime;
        public int ShieldDamage;

        private bool _isDrainingMana;
        private int _manaDrain;
        private int _lightGain;

        void HandleEffects(RealmTime time)
        {
            if (PassiveCooldown[PassiveSlot] == 0)
            {
                RegularPassives();
                PassiveCooldown[PassiveSlot] = 250;
            }

            if (HasConditionEffect(ConditionEffects.Exposed) && Stats.Boost[12] > 0)
            {
                Stats.Boost.ActivateBoost[12].PopAll();
                Stats.ReCalculateValues();
                ShieldDamage = 0;
            }

            ShieldMax = Stats[12];
            Shield = Stats[12] - ShieldDamage;

            if (HasConditionEffect(ConditionEffects.Shielded) && ShieldDamage > 0)
            {
                ShieldDamage -= Math.Max(6,
                    (int)Math.Round(Stats[12] * 0.05f * (time.ElapsedMsDelta / 1000f), 0));
            }
            else if (ShieldDamage < 0)
            {
                ShieldDamage = 0;
            }


            if (_isDrainingMana)
            {
                MP = Math.Max(0, (int)(MP - _manaDrain * time.ElapsedMsDelta / 1000f));

                if (_lightGain > 0)
                    Light += Math.Max(1,
                        (int)((HasConditionEffect(ConditionEffects.Enlightened) ? _lightGain + 3 : _lightGain) *
                            time.ElapsedMsDelta / 1000f));

                if (MP == 0)
                    _isDrainingMana = false;
            }

            if (Light > LightMax)
            {
                Light = LightMax;
            }

            //WIP(?): certain immunities if you have shield points

            if (_client.Account.Hidden && !HasConditionEffect(ConditionEffects.Hidden))
            {
                ApplyConditionEffect(ConditionEffectIndex.Hidden);
                ApplyConditionEffect(ConditionEffectIndex.Invincible);
                Manager.Clients[Client].Hidden = true;
            }

            if (Muted && !HasConditionEffect(ConditionEffects.Muted))
                ApplyConditionEffect(ConditionEffectIndex.Muted);

            if (HasConditionEffect(ConditionEffects.Renewed) && !HasConditionEffect(ConditionEffects.Sick))
            {
                if (_healing > 1)
                {
                    HP = Math.Min(Stats[0], HP + (int)_healing);
                    _healing -= (int)_healing;
                }

                _healing += 28 * (time.ElapsedMsDelta / 1000f);
            }

            if (HasConditionEffect(ConditionEffects.Stupefied) && MP > 0)
                MP = 0;

            if (HasConditionEffect(ConditionEffects.Bleeding) && HP > 1 && Shield <= 0)
            {
                if (_bleeding > 1)
                {
                    HP -= (int)_bleeding;
                    if (HP < 1)
                        HP = 1;
                    _bleeding -= (int)_bleeding;
                }

                _bleeding += 28 * (time.ElapsedMsDelta / 1000f);
            }

            if (HasConditionEffect(ConditionEffects.NinjaSpeedy))
            {
                MP = Math.Max(0, (int)(MP - 10 * time.ElapsedMsDelta / 1000f));

                if (MP == 0)
                    ApplyConditionEffect(ConditionEffectIndex.NinjaSpeedy, 0);
            }

            if (_newbieTime > 0)
            {
                _newbieTime -= time.ElapsedMsDelta;
                if (_newbieTime < 0)
                    _newbieTime = 0;
            }

            if (_canTpCooldownTime > 0)
            {
                _canTpCooldownTime -= time.ElapsedMsDelta;
                if (_canTpCooldownTime < 0)
                    _canTpCooldownTime = 0;
            }

            // for individual effects
            for (int i = 0; i < PassiveCooldown.Length; i++)
            {
                if (PassiveCooldown[i] > 0)
                {
                    PassiveCooldown[i] -= time.ElapsedMsDelta;
                    if (PassiveCooldown[i] < 0)
                        PassiveCooldown[i] = 0;
                }
            }
        }

        bool CanHpRegen()
        {
            if (HasConditionEffect(ConditionEffects.Sick))
                return false;
            if (HasConditionEffect(ConditionEffects.Bleeding))
                return false;
            return true;
        }

        bool CanMpRegen()
        {
            if (HasConditionEffect(ConditionEffects.Stupefied) ||
                HasConditionEffect(ConditionEffects.NinjaSpeedy) ||
                _isDrainingMana)
                return false;

            return true;
        }

        internal void SetNewbiePeriod()
        {
            _newbieTime = 3000;
        }

        internal void SetTPDisabledPeriod()
        {
            _canTpCooldownTime = 10 * 1000; // 10 seconds
        }

        public bool IsVisibleToEnemy()
        {
            if (HasConditionEffect(ConditionEffects.Paused))
                return false;
            if (HasConditionEffect(ConditionEffects.Invisible))
                return false;
            if (HasConditionEffect(ConditionEffects.Hidden))
                return false;
            if (_newbieTime > 0)
                return false;
            return true;
        }

        public bool TPCooledDown()
        {
            return _canTpCooldownTime <= 0;
        }
    }
}