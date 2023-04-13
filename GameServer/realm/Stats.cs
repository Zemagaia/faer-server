using GameServer.realm.entities.player;

namespace GameServer.realm
{
    public enum StatsType : byte
    {
        MaximumHP = 0,
        HP = 1,
        Size = 2,
        MaximumMP = 3,
        MP = 4,
        ExperienceGoal = 5,
        Experience = 6,
        Level = 7,
        Inventory = 8,
        // 9-19 unused
        Strength = 20,
        Armor = 21,
        Agility = 22,
        Stamina = 26,
        Intelligence = 27,
        Dexterity = 28,
        Effects = 29,
        Stars = 30,
        Name = 31,
        Texture1 = 32,
        Texture2 = 33,
        MerchantMerchandiseType = 34,
        Credits = 35,
        SellablePrice = 36,
        PortalUsable = 37,
        AccountId = 38,
        CurrentFame = 39,
        SellablePriceCurrency = 40,
        ObjectConnection = 41,

        /*
         * Mask :F0F0F0F0
         * each byte -> type
         * 0:Dot
         * 1:ShortLine
         * 2:L
         * 3:Line
         * 4:T
         * 5:Cross
         * 0x21222112
        */
        MerchantRemainingCount = 42,
        MerchantRemainingMinute = 43,
        MerchantDiscount = 44,
        SellableRankRequirement = 45,
        HPBoost = 46,
        MPBoost = 47,
        StrengthBonus = 48,
        ArmorBonus = 49,
        AgilityBonus = 50,
        StaminaBonus = 51,
        IntelligenceBonus = 52,
        DexterityBonus = 53,
        OwnerAccountId = 54,
        NameChangerStar = 55,
        NameChosen = 56,
        Fame = 57,
        FameGoal = 58,
        Glow = 59,
        SinkOffset = 60,
        AltTextureIndex = 61,
        Guild = 62,
        GuildRank = 63,
        OxygenBar = 64,
        XPBoost = 65,
        XPBoostTime = 66,
        LDBoostTime = 67,
        LTBoostTime = 68,
        HealthStackCount = 69,
        MagicStackCount = 70,
        // 71-78 unused
        HasBackpack = 79,
        Skin = 80,
        // 89-95 unused
        Effects2 = 96,
        Tokens = 97,
        // 98-101 unused
        Luck = 102,
        Rank = 103,
        Admin = 104,
        LuckBonus = 105,
        UnholyEssence = 106,
        DivineEssence = 107,
        Haste = 108,
        HasteBoost = 109,
        Shield = 110,
        ShieldBonus = 111,
        ShieldPoints = 112,
        ShieldPointsMax = 113,
        LightMax = 114,
        Light = 115,
        // 116 unused
        Tenacity = 117,
        TenacityBoost = 118,
        CriticalStrike = 119,
        CriticalStrikeBoost = 120,
        LifeSteal = 121,
        LifeStealBoost = 122,
        ManaLeech = 123,
        ManaLeechBoost = 124,
        LifeStealKill = 125,
        LifeStealKillBoost = 126,
        ManaLeechKill = 127,
        ManaLeechKillBoost = 128,
        // 129 - 155 unused
        Resistance = 156,
        ResistanceBoost = 157,
        OffensiveAbility = 158,
        DefensiveAbility = 159,
        PetData = 81,
        Wit = 82,
        WitBoost = 83,
        Lethality = 84,
        LethalityBoost = 85,
        Piercing = 86,
        PiercingBoost = 87,
        Immunities = 88,

        None = 255
    }

    public class SV<T>
    {
        private readonly Entity _owner;
        private readonly StatsType _type;
        private readonly bool _updateSelfOnly;
        private readonly Func<T, T> _transform;
        private T _value;
        private T _tValue;

        public SV(Entity e, StatsType type, T value, bool updateSelfOnly = false, Func<T, T> transform = null)
        {
            _owner = e;
            _type = type;
            _updateSelfOnly = updateSelfOnly;
            _transform = transform;

            _value = value;
            _tValue = Transform(value);
        }

        public T GetValue()
        {
            return _value;
        }

        public void SetValue(T value)
        {
            if (_value != null && _value.Equals(value))
                return;
            _value = value;

            var tVal = Transform(value);
            if (_tValue != null && _tValue.Equals(tVal))
                return;
            _tValue = tVal;

            // hacky fix to xp
            if (_owner is Player p && _type == StatsType.Experience)
            {
                _owner.InvokeStatChange(_type, (int)(object)tVal - Player.GetLevelExp(p.Level), _updateSelfOnly);
                return;
            }

            _owner.InvokeStatChange(_type, tVal, _updateSelfOnly);
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        private T Transform(T value)
        {
            return _transform == null ? value : _transform(value);
        }
    }

    public class StatChangedEventArgs : EventArgs
    {
        public StatChangedEventArgs(StatsType stat, object value, bool updateSelfOnly = false)
        {
            Stat = stat;
            Value = value;
            UpdateSelfOnly = updateSelfOnly;
        }

        public StatsType Stat { get; private set; }
        public object Value { get; private set; }
        public bool UpdateSelfOnly { get; private set; }
    }
}