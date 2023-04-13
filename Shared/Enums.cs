using System;

namespace common
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
        GuildFame = 2,
        Tokens = 3,
        UnholyEssence = 4,
        DivineEssence = 5
    }

    [Flags]
    public enum ConditionEffects : ulong
    {
        Dead = 1 << 0,
        Stupefied = 1 << 1,
        Weak = 1 << 2,
        Slow = 1 << 3,
        Sick = 1 << 4,
        Crippled = 1 << 5,
        Stunned = 1 << 6,
        Blind = 1 << 7,
        Hallucinating = 1 << 8,
        Drunk = 1 << 9,
        Confused = 1 << 10,
        Invisible = 1 << 11,
        Paralyzed = 1 << 12,
        Swift = 1 << 13,
        Bleeding = 1 << 14,
        Renewed = 1 << 15,
        Brave = 1 << 16,
        Berserk = 1 << 17,
        Paused = 1 << 18,
        Stasis = 1 << 19,
        Invincible = 1 << 20,
        Invulnerable = 1 << 21,
        Armored = 1 << 22,
        Unarmored = 1 << 23,
        Hexed = 1 << 24,
        NinjaSpeedy = 1 << 25,
        Unsteady = 1 << 26,
        Unsighted = 1 << 27,
        Petrify = 1 << 28,
        PetDisable = 1 << 29,
        Curse = 1 << 30,
        HPBoost = (uint)1 << 31,
        MPBoost = (ulong)1 << 32,
        StrBoost = (ulong)1 << 33,
        ArmBoost = (ulong)1 << 34,
        AglBoost = (ulong)1 << 35,
        DexBoost = (ulong)1 << 36,
        StaBoost = (ulong)1 << 37,
        IntBoost = (ulong)1 << 38,
        Hidden = (ulong)1 << 39,
        Muted = (ulong)1 << 40,
        Shielded = (ulong)1 << 41,
        ShieldBoost = (ulong)1 << 42,
        Enlightened = (ulong)1 << 43,
        Suppressed = (ulong)1 << 44,
        Exposed = (ulong)1 << 45,
        Staggered = (ulong)1 << 46,
        Unstoppable = (ulong)1 << 47,
        Taunted = (ulong)1 << 48,
        ResistanceBoost = (ulong)1 << 49,
        WitBoost = (ulong)1 << 50
    }

    public enum ConditionEffectIndex
    {
        Dead = 0,
        Stupefied = 1,
        Weak = 2,
        Slow = 3,
        Sick = 4,
        Crippled = 5,
        Stunned = 6,
        Blind = 7,
        Hallucinating = 8,
        Drunk = 9,
        Confused = 10,
        Invisible = 11,
        Paralyzed = 12,
        Swift = 13,
        Bleeding = 14,
        Renewed = 15,
        Brave = 16,
        Berserk = 17,
        Paused = 18,
        Stasis = 19,
        Invincible = 20,
        Invulnerable = 21,
        Armored = 22,
        Unarmored = 23,
        Hexed = 24,
        NinjaSpeedy = 25,
        Unsteady = 26,
        Unsighted = 27,
        Petrify = 28,
        PetDisable = 29,
        Curse = 30,
        HPBoost = 31,
        MPBoost = 32,
        StrBoost = 33,
        ArmBoost = 34,
        AglBoost = 35,
        DexBoost = 36,
        StaBoost = 37,
        IntBoost = 38,
        Hidden = 39,
        Muted = 40,
        Shielded = 41,
        ShieldBoost = 42,
        Enlightened = 43,
        Suppressed = 44,
        Exposed = 45,
        Staggered = 46,
        Unstoppable = 47,
        Taunted = 48,
        ResistanceBoost = 49,
        WitBoost = 50
    }
    
    [Flags]
    public enum DamageTypes : byte
    {
        True = 0,
        Physical = 1 << 0,
        Earth = 1 << 1,
        Air = 1 << 2,
        Profane = 1 << 3,
        Magical = 1 << 4,
        Water = 1 << 5,
        Fire = 1 << 6,
        Holy = 1 << 7
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
        Shoot,
        DualShoot,
        StatBoostSelf,
        StatBoostAura,
        BulletNova,
        ConditionEffectAura,
        ConditionEffectSelf,
        Heal,
        HealNova,
        Magic,
        MagicNova,
        Teleport,
        SpawnUndead,
        Trap,
        StasisBlast,
        Decoy,
        Lightning,
        Vial,
        RemoveNegativeConditions,
        RemoveNegativeConditionsSelf,
        IncrementStat,
        Drake,
        PermaPet,
        Create,
        DazeBlast,
        ClearConditionEffectAura,
        ClearConditionEffectSelf,
        Dye,
        ShurikenAbility,
        TomeDamage,
        MultiDecoy,
        Mushroom,
        PearlAbility,
        BuildTower,
        MonsterToss,
        PartyAOE,
        MiniPot,
        Halo,
        Fame,
        SamuraiAbility,
        Summon,
        ChristmasPopper,
        Belt,
        Totem,
        UnlockPortal,
        CreatePet,
        Pet,
        UnlockSkin,
        GenericActivate,
        MysteryPortal,
        ChangeSkin,
        FixedStat,
        Backpack,
        MiscBoosts,
        UnlockEmote,
        HealingGrenade,
        PetSkin,
        Unlock,
        MysteryDyes,
        Card,
        Orb,
        Tome,
    }
}