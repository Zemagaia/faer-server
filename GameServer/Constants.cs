using Shared;

namespace GameServer
{
    public static class Constants
    {
        // Block names *containing*...
        public static readonly HashSet<string> BannedNames = new(new[]
        {
#if !DEBUG
            "Zemagaia", "Fiow", "FiowDev", "Nigg", "Nigger", "Nigga", "Fagg", "Faggot"
#else
            ""
#endif
        }, StringComparer.InvariantCultureIgnoreCase);

        // Events: 
        // these are not +x*100%, they're x*100%
        public static readonly float? GlobalXpBoost = 1f;
        public static readonly float? GlobalLootBoost = 10f;
        public static readonly DateTime EventEnds = new(2021, 12, 01);

        public static readonly ConditionEffectIndex[] NegativeEffsIdx = new ConditionEffectIndex[]
        {
            ConditionEffectIndex.Bleeding, ConditionEffectIndex.Sick,
        };

        public static double GetGameHour()
        {
            return (DateTime.UtcNow.Hour + DateTime.UtcNow.Minute / 60.0) * 4 % 24;
        }

        public const DamageTypes MagicTypes =
            DamageTypes.Magical | DamageTypes.Water | DamageTypes.Fire | DamageTypes.Holy;
    }

    // Immunities for AddImmunity behavior
    // these match the StatsType id
    public enum Immunity : byte
    {
        // Ids for Entity.Immunities
        SlowImmune = 0,
        StunImmune = 1,
        UnarmoredImmune = 2,
        StasisImmune = 3,
        ParalyzeImmune = 4,
        CurseImmune = 5,
        PetrifyImmune = 6,
        CrippledImmune = 7,
    }
}