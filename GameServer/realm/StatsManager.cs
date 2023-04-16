using Shared;
using Shared.resources;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;
using wServer.realm;

namespace GameServer.realm
{
    public class StatsManager
    {
        internal const int NumStatTypes = 9;

        internal readonly Player Owner;
        internal readonly BaseStatManager Base;
        internal readonly BoostStatManager Boost;

        private readonly SV<short>[] _stats;

        public int this[int index] => Base[index] + Boost[index];

        public StatsManager(Player owner)
        {
            Owner = owner;
            Base = new BaseStatManager(this);
            Boost = new BoostStatManager(this);

            _stats = new SV<short>[NumStatTypes];
            for (var i = 0; i < NumStatTypes; i++)
                _stats[i] = new SV<short>(Owner, GetStatType(i), (short)this[i],
                    i != 0 && i != 1); // make maxHP and maxMP global update
        }

        public void ReCalculateValues(InventoryChangedEventArgs e = null)
        {
            Base.ReCalculateValues(e);
            Boost.ReCalculateValues(e);

            for (var i = 0; i < _stats.Length; i++)
                _stats[i].SetValue((short)this[i]);
        }

        internal void StatChanged(int index)
        {
            _stats[index].SetValue((short)this[index]);
        }

        public float GetAttackDamage(ProjectileDesc desc, bool isAbility = false)
        {
            return desc.Damage;
        }

        // for enemies - multiple damages and damage types
        public static float GetDefenseDamage(Entity host, int[] damages, DamageTypes[] damageTypes, Player hitter)
        {
            var sum = damages.Sum();
            var limit = sum * 0.25f; //0.15f;
            float ret = sum;
            // DamageTypes 0 is the projectile's DamageType
            if (damageTypes[0] != DamageTypes.True)
            {
                ret = DamageUtils.GetDamage(host, damages, damageTypes, hitter);
                if (ret < limit)
                    ret = limit;
            }

            if (host.HasConditionEffect(ConditionEffects.Invulnerable))
                ret = 0;
            return ret;
        }

        // for enemies - single damage and damage type
        public static float GetDefenseDamage(Entity host, int damage, DamageTypes damageType, Player hitter)
        {
            var limit = damage * 0.25f; //0.15f;
            float ret = damage;
            // DamageTypes 0 is the projectile's DamageType
            if (damageType != DamageTypes.True)
            {
                ret = DamageUtils.GetDamage(host, damage, damageType, hitter);
                if (ret < limit)
                    ret = limit;
            }

            if (host.HasConditionEffect(ConditionEffects.Invulnerable))
                ret = 0;
            return ret;
        }

        public float GetDefenseDamage(int damage, DamageTypes damageType, bool noDef = false) // for players
        {
            var limit = damage * 0.25f; //0.15f;
            float ret = damage;
            // DamageTypes 0 is the projectile's DamageType
            if (damageType != DamageTypes.True && !noDef)
            {
                ret = DamageUtils.GetDamage(Owner, damage, damageType);
                if (ret < limit)
                    ret = limit;
            }

            if (Owner.HasConditionEffect(ConditionEffects.Invulnerable))
                ret = 0;
            return ret;
        }

        public float GetSpeed(WmapTile tile)
        {
            float agility = this[4] <= 384 ? this[4] : 384;
            var ret = 4 + 5.6f * (agility / 75f);
            return ret * tile.TileDesc.Speed;
        }

        public float GetHpRegen()
        {
            var stamina = this[6] <= 384 ? this[6] : 384;
            if (Owner.HasConditionEffect(ConditionEffects.Sick))
                stamina = 0;

            return 1 + stamina * .12f;
        }

        public float GetMpRegen()
        {
            var intelligence = this[7] <= 384 ? this[7] : 384;
            return 0.5f + intelligence * .06f;
        }

        /*public float Dex()
        {
            var dex = this[5];
            if (Owner.HasConditionEffect(ConditionEffects.Crippled))
                dex = 0;

            var ret = 1.5f + 6.5f * (dex / 75f);
            if (Owner.HasConditionEffect(ConditionEffects.Berserk))
                ret *= 1.5f;
            if (Owner.HasConditionEffect(ConditionEffects.Stunned))
                ret = 0;
            return ret;
        }*/

        public static int GetStatIndex(StatsType stat)
        {
            switch (stat)
            {
                case StatsType.MaxHP:
                    return 0;
                case StatsType.MaxMP:
                    return 1;
                case StatsType.Strength:
                    return 2;
                case StatsType.Defense:
                    return 3;
                case StatsType.Speed:
                    return 4;
                case StatsType.Sight:
                    return 5;
                case StatsType.Stamina:
                    return 6;
                case StatsType.Luck:
                    return 7;
                case StatsType.Penetration:
                    return 8;
                default:
                    return -1;
            }
        }

        public static StatsType GetStatType(int stat)
        {
            switch (stat)
            {
                case 0:
                    return StatsType.MaxHP;
                case 1:
                    return StatsType.MaxMP;
                case 2:
                    return StatsType.Strength;
                case 3:
                    return StatsType.Defense;
                case 4:
                    return StatsType.Speed;
                case 5:
                    return StatsType.Sight;
                case 6:
                    return StatsType.Stamina;
                case 7:
                    return StatsType.Luck;
                case 8:
                    return StatsType.Penetration;
                default:
                    return StatsType.None;
            }
        }

        public static StatsType GetBoostStatType(int stat)
        {
            switch (stat)
            {
                case 0:
                    return StatsType.HPBoost;
                case 1:
                    return StatsType.MPBoost;
                case 2:
                    return StatsType.StrengthBonus;
                case 3:
                    return StatsType.DefenseBonus;
                case 4:
                    return StatsType.SpeedBonus;
                case 5:
                    return StatsType.SightBonus;
                case 6:
                    return StatsType.StaminaBonus;
                case 7:
                    return StatsType.LuckBonus;
                case 8:
                    return StatsType.PenetrationBonus;
                default:
                    return StatsType.None;
            }
        }

        public class DamageUtils
        {
            internal static int GetDamage(Entity entity, int[] damages, DamageTypes[] damageTypes, Player hitter = null)
            {
                if (entity is null)
                    return 0;
                var ret = 0;
                int i;
                for (i = 0; i < damages.Length; i++)
                {
                    if (damages[i] == 0)
                    {
                        continue;
                    }

                    ret += damages[i] - GetArmorForType(damageTypes[i], entity, hitter);
                }

                return ret;
            }
            
            internal static int GetDamage(Entity entity, int damage, DamageTypes damageType, Player hitter = null)
            {
                if (entity is null)
                    return 0;

                return damage - GetArmorForType(damageType, entity, hitter);
            }

            private static int GetArmorForType(DamageTypes damageType, Entity entity, Player hitter = null)
            {
                // calculate armor for players
                if (entity is Player p)
                {
                    var isMagic = (damageType & Constants.MagicTypes) != 0;
                    return isMagic ? Math.Min(p.Stats[19], 384) : Math.Min(p.Stats[3], 384);
                }

                // finally calculate armor for enemies - hitter should never be null 
                if (hitter == null)
                    return 0;

                var desc = entity.ObjectDesc;
                var armor = desc.Armor;
                var resistance = desc.Resistance;

                // calculate global modifiers
                if (entity.HasConditionEffect(ConditionEffects.Armored))
                    armor *= 2;

                var lethality = hitter.Stats[21];
                var piercing = hitter.Stats[22];

                // def calculation from incoming damage type
                // earth > air
                // water > fire
                // profane = holy
                return damageType switch
                {
                    DamageTypes.Physical => Math.Max(0, armor - lethality),
                    DamageTypes.Earth => Math.Max(0, armor + desc.EarthResistance - desc.AirResistance + desc.FireResistance - lethality),
                    DamageTypes.Air => Math.Max(0, armor + desc.AirResistance + desc.EarthResistance - desc.WaterResistance - lethality),
                    DamageTypes.Profane => Math.Max(0, armor + desc.ProfaneResistance - desc.HolyResistance - lethality),
                    DamageTypes.Magical => Math.Max(0, resistance - piercing),
                    DamageTypes.Water => Math.Max(0, resistance + desc.WaterResistance - desc.FireResistance + desc.AirResistance - piercing),
                    DamageTypes.Fire => Math.Max(0, resistance + desc.FireResistance + desc.WaterResistance - desc.EarthResistance - piercing),
                    DamageTypes.Holy => Math.Max(0, resistance + desc.HolyResistance - desc.ProfaneResistance - piercing),
                    _ => 0
                };
            }

            /// <summary>
            /// Returns added values from <b>adder</b> into <b>source</b>
            /// </summary>
            internal static int[] Add(int[] source, int[] adder)
            {
                if (adder.Length > source.Length)
                {
                    source = Utils.ResizeArray(source, adder.Length);
                }

                var ret = source;
                for (var i = 0; i < ret.Length; i++)
                {
                    ret[i] += adder[i];
                }

                return ret;
            }
        }
    }
}