namespace GameServer.realm; 

public enum StatsType : byte
{
    MaxHP = 0,
    HP = 1,
    Size = 2,
    MaxMP = 3,
    MP = 4,
    Inv0 = 8,
    Inv1 = 9,
    Inv2 = 10,
    Inv3 = 11,
    Inv4 = 12,
    Inv5 = 13,
    Inv6 = 14,
    Inv7 = 15,
    Inv8 = 16,
    Inv9 = 17,
    Inv10 = 18,
    Inv11 = 19,
    Inv12 = 67,
    Inv13 = 68,
    Inv14 = 69,
    Inv15 = 70,
    Inv16 = 71,
    Inv17 = 72,
    Inv18 = 73,
    Inv19 = 74,
    HasBackpack = 75,
    Name = 31,
    MerchType = 34,
    Gems = 35,
    MerchPrice = 40,
    MerchCount = 42,
    Gold = 43,
    Crowns = 44,
    OwnerAccountId = 54,

    Strength = 20,
    Defense = 21,
    Speed = 22,
    Stamina = 27,
    Condition = 29,
    Penetration = 30,
    Texture1 = 32,
    Texture2 = 33,
    SellablePrice = 36,
    PortalUsable = 37,
    AccountId = 38,
    HPBoost = 46,
    MPBoost = 47,
    StrengthBonus = 48,
    DefenseBonus = 49,
    SpeedBonus = 50,
    StaminaBonus = 52,
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
    Texture = 76,
    Wit = 77,
    Resistance = 78,
    Haste = 79,
    Intelligence = 80,
    Piercing = 81,
    Tenacity = 82,
    WitBonus = 83,
    ResistanceBonus = 84,
    HasteBonus = 85,
    IntelligenceBonus = 86,
    PiercingBonus = 87,
    TenacityBonus = 88,
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