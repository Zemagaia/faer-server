using System;
using System.Xml.Linq;
using NLog.LayoutRenderers;
using StackExchange.Redis;

namespace Shared
{ 
    public enum ItemType
    {
        Weapon,
        Ability,
        Armor,
        Ring,
        Potion,
        StatPot,
        Other,
        None
    }

    public enum CurrencyType
    {
        Gold = 0,
        Fame = 1,
        GuildFame = 2
    }
    
    [Flags]
    public enum ConditionEffects : ulong
    {
        Dead = 1 << 0,
        Weak = 1 << 1,
        Slowed = 1 << 2,
        Sick = 1 << 3,
        Speedy = 1 << 4,
        Bleeding = 1 << 5,
        Healing = 1 << 6,
        Damaging = 1 << 7,
        Invulnerable = 1 << 8,
        Armored = 1 << 9,
        ArmorBroken = 1 << 10,
        Hidden = 1 << 11,
        Targeted = 1 << 12
    }

    public enum ConditionEffectIndex
    {
        Dead = 0,
        Weak = 1,
        Slowed = 2,
        Sick = 3,
        Speedy = 4,
        Bleeding = 5,
        Healing = 6,
        Damaging = 7,
        Invulnerable = 8,
        Armored = 9,
        ArmorBroken = 10,
        Hidden = 11,
        Targeted = 12
    }
    
    [Flags]
    public enum DamageTypes : byte
    {
        True = 0,
        Physical = 1 << 0,
        Magical = 1 << 1
    }
    
    public enum EffectType
    {
        Potion = 1,
        Teleport = 2,
        Stream = 3,
        Throw = 4,
        AreaBlast = 5, //radius=pos1.x
        Dead = 6,
        Trail = 7,
        Diffuse = 8, //radius=dist(pos1,pos2)
        Flow = 9,
        Trap = 10, //radius=pos1.x
        Lightning = 11, //particleSize=pos2.x
        Concentrate = 12, //radius=dist(pos1,pos2)
        BlastWave = 13, //origin=pos1, radius = pos2.x
        Earthquake = 14,
        Flashing = 15, //period=pos1.x, numCycles=pos1.y
        BeachBall = 16
    }
    
    public enum ActivateEffects
    {
        OpenPortal,
        TierIncrease,
        Cage,
        Clock,
        HitMultiplier,
        DamageMultiplier,
        StatBoostSelf,
        StatBoostAura,
        ConditionEffectAura,
        ConditionEffectSelf,
        Heal,
        HealNova,
        Magic,
        MagicNova,
        Teleport,
        IncrementStat,
        Create,
        Totem,
        UnlockPortal,
        UnlockSkin,
        ChangeSkin,
        FixedStat,
        Backpack,
        UnlockEmote,
        Bloodstone
    }

    public struct TotemEffect {
        public ConditionEffectIndex? ConditionEffect;
        public int StatType;
        public int Value;

        public TotemEffect(string val) {
            if (Enum.TryParse(val, out ConditionEffectIndex conditionEffect)) {
                ConditionEffect = conditionEffect;
                return;
            }

            var split = val.Replace("+", "").Split(' ');
            StatType = GetStatTypeShort(split[1]);
            Value = int.Parse(split[0]);
        }
        
        // todo this is terrible
        public static int GetStatTypeShort(string stat) {
            return stat switch {
                "HP" => 0,
                "MP" => 3,  
                "STR" => 20, 
                "WIT" => 77,
                "DEF" => 21,
                "RES" => 78,
                "SPD" => 22,
                "HST" => 79,
                "STM" => 27,
                "INT" => 80,
                "PRC" => 81,
                "PEN" => 30,
                "TEN" => 82,
                _ => 255
            };
        }
    }
}