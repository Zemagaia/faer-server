using Shared;

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
            if (_client.Account.Hidden && !HasConditionEffect(ConditionEffects.Hidden))
            {
                ApplyConditionEffect(ConditionEffectIndex.Hidden);
                Manager.Clients[Client].Hidden = true;
            }
            
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
            if (_isDrainingMana)
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