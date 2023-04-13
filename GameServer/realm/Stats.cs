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
        	Inventory0 = 8,
        	Inventory1 = 9,
        	Inventory2 = 10,
        	Inventory3 = 11,
        	Inventory4 = 12,
        	Inventory5 = 13,
        	Inventory6 = 14,
        	Inventory7 = 15,
        	Inventory8 = 16,
        	Inventory9 = 17,
        	Inventory10 = 18,
        	Inventory11 = 19,
        	Strength = 20,
        	Defense = 21,
        	Speed = 22,
        	Sight = 26,
        	Stamina = 27,
        	Luck = 28,
        	Effects = 29,
        	Penetration = 30,
        	Name = 31,
        	Texture1 = 32,
        	Texture2 = 33,
        	MerchantMerchandiseType = 34,
        	Gems = 35,
        	SellablePrice = 36,
        	PortalUsable = 37,
        	AccountId = 38,
        	SellablePriceCurrency = 40,
        	MerchantRemainingCount = 42,
        	Gold = 43,
        	Crowns = 44,
        	HPBoost = 46,
        	MPBoost = 47,
        	StrengthBonus = 48,
        	DefenseBonus = 49,
        	SpeedBonus = 50,
        	SightBonus = 51,
        	StaminaBonus = 52,
        	LuckBonus = 53,
        	OwnerAccountId = 54,
        	DamageMultiplier = 55,
        	Tier = 56,
        	PenetrationBonus = 57,
        	HitMultiplier = 58,
        	Glow = 59,
        	AltTextureIndex = 61,
        	Guild = 62,
        	GuildRank = 63,
        	HealthStackCount = 65,
        	MagicStackCount = 66,
        	BackPack0 = 67,
        	BackPack1 = 68,
        	BackPack2 = 69,
        	BackPack3 = 70,
        	BackPack4 = 71,
        	BackPack5 = 72,
        	BackPack6 = 73,
        	BackPack7 = 74,
        	HasBackpack = 75,
        	Skin = 76,
        	None = byte.MaxValue
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