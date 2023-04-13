using common;
using common.resources;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;

namespace GameServer.realm
{
    public class StatsManager
    {
        internal const int NumStatTypes = 23;

        internal readonly Player Owner;
        internal readonly BaseStatManager Base;
        internal readonly BoostStatManager Boost;

        private readonly SV<int>[] _stats;

        public int this[int index] => Base[index] + Boost[index];

        public StatsManager(Player owner)
        {
            Owner = owner;
            Base = new BaseStatManager(this);
            Boost = new BoostStatManager(this);

            _stats = new SV<int>[NumStatTypes];
            for (var i = 0; i < NumStatTypes; i++)
                _stats[i] = new SV<int>(Owner, GetStatType(i), this[i],
                    i != 0 && i != 1); // make maxHP and maxMP global update
        }

        public void ReCalculateValues(InventoryChangedEventArgs e = null)
        {
            Base.ReCalculateValues(e);
            Boost.ReCalculateValues(e);

            for (var i = 0; i < _stats.Length; i++)
                _stats[i].SetValue(this[i]);
        }

        internal void StatChanged(int index)
        {
            _stats[index].SetValue(this[index]);
        }

        public float GetAttackDamage(ProjectileDesc desc, bool isAbility = false)
        {
            if (Owner.HasConditionEffect(ConditionEffects.Blind))
                return 0;

            var attackMult = 1f;
            if (!isAbility)
            {
                var isMagic = (desc.DamageType & Constants.MagicTypes) != 0;
                var power = isMagic ? Math.Min(this[20], 384) : Math.Min(this[2], 384);
                if (Owner.HasConditionEffect(ConditionEffects.Weak))
                    power = 0;
                attackMult = 0.5f + power / 50f;
            }

            var ret = Owner.Client.Random.Next(desc.MinDamage, desc.MaxDamage) * attackMult;
            if (Owner.HasConditionEffect(ConditionEffects.Brave))
                ret *= 1.5f;

            ret += CriticalModifier(ret);

            return ret;
        }

        private float CriticalModifier(float damage)
        {
            float criticalStrike = this[14];
            float strength = this[2] <= 384 ? this[2] : 384;
            float ret = 0;
            var roll = Owner.Client.Random.Next(0, 1000);
            var chance = (1 + criticalStrike) * 10 > 500 ? 500 : (int)(1 + criticalStrike) * 10;
            if (roll > chance || Owner.HasConditionEffect(ConditionEffects.Unsteady))
                return ret;
            for (var i = 0; i < 6; i++)
            {
                if (Owner.Inventory[i].Item == null)
                    continue;
                switch (Owner.Inventory[i].Item.Power)
                {
                    case "Rotting Touch":
                        ret += strength / 2;
                        continue;
                }
            }

            ret += damage * 0.67f;
            return ret;
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

            if (host.HasConditionEffect(ConditionEffects.Curse))
                ret = (int)(ret * 1.20);
            if (host.HasConditionEffect(ConditionEffects.Petrify))
                ret = (int)(ret * 0.9);
            if (host.HasConditionEffect(ConditionEffects.Invulnerable) ||
                host.HasConditionEffect(ConditionEffects.Invincible))
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

            if (host.HasConditionEffect(ConditionEffects.Curse))
                ret = (int)(ret * 1.20);
            if (host.HasConditionEffect(ConditionEffects.Petrify))
                ret = (int)(ret * 0.9);
            if (host.HasConditionEffect(ConditionEffects.Invulnerable) ||
                host.HasConditionEffect(ConditionEffects.Invincible))
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

            if (Owner.HasConditionEffect(ConditionEffects.Curse))
                ret = (int)(ret * 1.20);
            if (Owner.HasConditionEffect(ConditionEffects.Petrify))
                ret = (int)(ret * 0.9);
            if (Owner.HasConditionEffect(ConditionEffects.Invulnerable) ||
                Owner.HasConditionEffect(ConditionEffects.Invincible))
                ret = 0;
            return ret;
        }

        public float GetSpeed(WmapTile tile)
        {
            float agility = this[4] <= 384 ? this[4] : 384;
            var ret = 4 + 5.6f * (agility / 75f);
            if (Owner.HasConditionEffect(ConditionEffects.Swift))
                ret *= 1.5f;
            if (Owner.HasConditionEffect(ConditionEffects.Slow))
                ret = 4;
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
            if (Owner.HasConditionEffect(ConditionEffects.Stupefied))
                return 0;

            int multiplier = Owner.HasConditionEffect(ConditionEffects.Enlightened) ? 2 : 1;
            return (0.5f + intelligence * .06f) * multiplier;
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
                case StatsType.MaximumHP:
                    return 0;
                case StatsType.MaximumMP:
                    return 1;
                case StatsType.Strength:
                    return 2;
                case StatsType.Armor:
                    return 3;
                case StatsType.Agility:
                    return 4;
                case StatsType.Dexterity:
                    return 5;
                case StatsType.Stamina:
                    return 6;
                case StatsType.Intelligence:
                    return 7;
                case StatsType.Luck:
                    return 10;
                case StatsType.Haste:
                    return 11;
                case StatsType.Shield:
                    return 12;
                case StatsType.Tenacity:
                    return 13;
                case StatsType.CriticalStrike:
                    return 14;
                case StatsType.LifeSteal:
                    return 15;
                case StatsType.ManaLeech:
                    return 16;
                case StatsType.LifeStealKill:
                    return 17;
                case StatsType.ManaLeechKill:
                    return 18;
                case StatsType.Resistance:
                    return 19;
                case StatsType.Wit:
                    return 20;
                case StatsType.Lethality:
                    return 21;
                case StatsType.Piercing:
                    return 22;
                default:
                    return -1;
            }
        }

        public static StatsType GetStatType(int stat)
        {
            switch (stat)
            {
                case 0:
                    return StatsType.MaximumHP;
                case 1:
                    return StatsType.MaximumMP;
                case 2:
                    return StatsType.Strength;
                case 3:
                    return StatsType.Armor;
                case 4:
                    return StatsType.Agility;
                case 5:
                    return StatsType.Dexterity;
                case 6:
                    return StatsType.Stamina;
                case 7:
                    return StatsType.Intelligence;
                case 10:
                    return StatsType.Luck;
                case 11:
                    return StatsType.Haste;
                case 12:
                    return StatsType.Shield;
                case 13:
                    return StatsType.Tenacity;
                case 14:
                    return StatsType.CriticalStrike;
                case 15:
                    return StatsType.LifeSteal;
                case 16:
                    return StatsType.ManaLeech;
                case 17:
                    return StatsType.LifeStealKill;
                case 18:
                    return StatsType.ManaLeechKill;
                case 19:
                    return StatsType.Resistance;
                case 20:
                    return StatsType.Wit;
                case 21:
                    return StatsType.Lethality;
                case 22:
                    return StatsType.Piercing;
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
                    return StatsType.ArmorBonus;
                case 4:
                    return StatsType.AgilityBonus;
                case 5:
                    return StatsType.DexterityBonus;
                case 6:
                    return StatsType.StaminaBonus;
                case 7:
                    return StatsType.IntelligenceBonus;
                case 10:
                    return StatsType.LuckBonus;
                case 11:
                    return StatsType.HasteBoost;
                case 12:
                    return StatsType.ShieldBonus;
                case 13:
                    return StatsType.TenacityBoost;
                case 14:
                    return StatsType.CriticalStrikeBoost;
                case 15:
                    return StatsType.LifeStealBoost;
                case 16:
                    return StatsType.ManaLeechBoost;
                case 17:
                    return StatsType.LifeStealKillBoost;
                case 18:
                    return StatsType.ManaLeechKillBoost;
                case 19:
                    return StatsType.ResistanceBoost;
                case 20:
                    return StatsType.WitBoost;
                case 21:
                    return StatsType.LethalityBoost;
                case 22:
                    return StatsType.PiercingBoost;
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
                if (entity.HasConditionEffect(ConditionEffects.Unarmored))
                    return 0;

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