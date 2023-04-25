using Shared;
using Shared.resources;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;
using wServer.realm;

namespace GameServer.realm;

public class StatsManager {
    internal const int NumStatTypes = 13;

    internal readonly Player Owner;
    internal readonly BaseStatManager Base;
    internal readonly BoostStatManager Boost;

    private readonly SV<short>[] _stats;

    public int this[int index] => Base[index] + Boost[index];

    public StatsManager(Player owner) {
        Owner = owner;
        Base = new BaseStatManager(this);
        Boost = new BoostStatManager(this);

        _stats = new SV<short>[NumStatTypes];
        for (var i = 0; i < NumStatTypes; i++)
            _stats[i] = new SV<short>(Owner, GetStatType(i), (short) this[i],
                i != 0 && i != 1); // make maxHP and maxMP global update
    }

    public void ReCalculateValues(InventoryChangedEventArgs e = null) {
        Base.ReCalculateValues(e);
        Boost.ReCalculateValues(e);

        for (var i = 0; i < _stats.Length; i++)
            _stats[i].SetValue((short) this[i]);
    }

    internal void StatChanged(int index) {
        _stats[index].SetValue((short) this[index]);
    }

    public static float GetPhysDamage(Entity host, int damage, Player hitter) {
        damage = (int) (damage * hitter.DamageMultiplier);
        var limit = damage * 0.25f;
        int def;
        if (host is Player p)
            def = p.Stats[2];
        else {
            if (hitter == null)
                return 0;

            var desc = host.ObjectDesc;
            def = desc.Defense;

            if (host.HasConditionEffect(ConditionEffects.Armored))
                def = (int) (def * 1.25);
        }


        var penetration = hitter.Stats[11];
        float ret = damage - (def - penetration);
        if (ret < limit)
            ret = limit;

        if (host.HasConditionEffect(ConditionEffects.Invulnerable))
            ret = 0;

        return ret;
    }

    public static float GetMagicDamage(Entity host, int damage, Player hitter) {
        damage = (int) (damage * hitter.DamageMultiplier);
        var limit = damage * 0.25f;
        int res;
        if (host is Player p)
            res = p.Stats[3];
        else {
            if (hitter == null)
                return 0;

            res = host.ObjectDesc.Resistance;
        }


        var piercing = hitter.Stats[10];
        float ret = damage - (res - piercing);
        if (ret < limit)
            ret = limit;

        if (host.HasConditionEffect(ConditionEffects.Invulnerable))
            ret = 0;

        return ret;
    }

    public static float GetTrueDamage(Entity host, int damage, Player hitter) {
        damage = (int) (damage * hitter.DamageMultiplier);
        float ret = damage;

        if (host.HasConditionEffect(ConditionEffects.Invulnerable))
            ret = 0;

        return ret;
    }

    public float GetPhysicalDamage(int damage, Player p) {
        damage = (int) (damage * p.HitMultiplier);
        var limit = damage * 0.25f;

        var def = p.Stats[2];
        if (p.HasConditionEffect(ConditionEffects.Armored))
            def = (int) (def * 1.25);

        float ret = damage - def;
        if (ret < limit)
            ret = limit;

        if (Owner.HasConditionEffect(ConditionEffects.Invulnerable))
            ret = 0;

        return ret;
    }

    public float GetMagicDamage(int damage, Player p) {
        damage = (int) (damage * p.HitMultiplier);
        var limit = damage * 0.25f;

        var res = p.Stats[3];
        float ret = damage - res;
        if (ret < limit)
            ret = limit;

        if (Owner.HasConditionEffect(ConditionEffects.Invulnerable))
            ret = 0;

        return ret;
    }
    
    public float GetTrueDamage(int damage) {
        float ret = damage;

        if (Owner.HasConditionEffect(ConditionEffects.Invulnerable))
            ret = 0;
        
        return ret;
    }

    public float GetSpeed(MapTile tile) {
        var ret = 4 + 5.6f * (this[6] / 75f);
        return ret * tile.TileDesc.Speed;
    }

    public float GetHpRegen() {
        var stamina = this[8];
        if (Owner.HasConditionEffect(ConditionEffects.Sick))
            stamina = 0;

        return 1 + stamina * .12f;
    }

    public float GetMpRegen() {
        return 0.5f + this[9] * .06f;
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

    public static int GetStatIndex(StatsType stat) {
#pragma warning disable CS8509
        return stat switch {
#pragma warning restore CS8509
            StatsType.MaxHP => 0,
            StatsType.MaxMP => 1,
            StatsType.Strength => 2,
            StatsType.Wit => 3,
            StatsType.Defense => 4,
            StatsType.Resistance => 5,
            StatsType.Speed => 6,
            StatsType.Haste => 7,
            StatsType.Stamina => 8,
            StatsType.Intelligence => 9,
            StatsType.Piercing => 10,
            StatsType.Penetration => 11,
            StatsType.Tenacity => 12
        };
    }

    public static StatsType GetStatType(int stat) {
        return stat switch {
            0 => StatsType.MaxHP,
            1 => StatsType.MaxMP,
            2 => StatsType.Strength,
            3 => StatsType.Wit,
            4 => StatsType.Defense,
            5 => StatsType.Resistance,
            6 => StatsType.Speed,
            7 => StatsType.Haste,
            8 => StatsType.Stamina,
            9 => StatsType.Intelligence,
            10 => StatsType.Piercing,
            11 => StatsType.Penetration,
            12 => StatsType.Tenacity,
            _ => StatsType.None
        };
    }

    public static StatsType GetStatTypeShort(string stat) {
        return stat switch {
            "HP" => StatsType.MaxHP,
            "MP" => StatsType.MaxMP,
            "STR" => StatsType.Strength,
            "WIT" => StatsType.Wit,
            "DEF" => StatsType.Defense,
            "RES" => StatsType.Resistance,
            "SPD" => StatsType.Speed,
            "HST" => StatsType.Haste,
            "STM" => StatsType.Stamina,
            "INT" => StatsType.Intelligence,
            "PRC" => StatsType.Piercing,
            "PEN" => StatsType.Penetration,
            "TEN" => StatsType.Tenacity,
            _ => StatsType.None
        };
    }

    public static StatsType GetBoostStatType(int stat) {
        return stat switch {
            0 => StatsType.HPBoost,
            1 => StatsType.MPBoost,
            2 => StatsType.StrengthBonus,
            3 => StatsType.WitBonus,
            4 => StatsType.DefenseBonus,
            5 => StatsType.ResistanceBonus,
            6 => StatsType.SpeedBonus,
            7 => StatsType.HasteBonus,
            8 => StatsType.StaminaBonus,
            9 => StatsType.IntelligenceBonus,
            10 => StatsType.PiercingBonus,
            11 => StatsType.PenetrationBonus,
            12 => StatsType.TenacityBonus,
            _ => StatsType.None
        };
    }

    public class DamageUtils {
        internal static int GetDamage(Entity entity, int damage, DamageTypes damageType, Player hitter = null) {
            if (entity is null)
                return 0;

            return damage - GetArmorForType(damageType, entity, hitter);
        }

        private static int GetArmorForType(DamageTypes damageType, Entity entity, Player hitter = null) {
            // calculate armor for players
            if (entity is Player p) {
                var isMagic = (damageType & DamageTypes.Magical) != 0;
                return isMagic ? Math.Min(p.Stats[3], 384) : Math.Min(p.Stats[2], 384);
            }

            // finally calculate armor for enemies - hitter should never be null 
            if (hitter == null)
                return 0;

            var desc = entity.ObjectDesc;
            var armor = desc.Defense;
            var resistance = desc.Resistance;

            // calculate global modifiers
            if (entity.HasConditionEffect(ConditionEffects.Armored))
                armor = (int) (armor * 1.25);

            var penetration = hitter.Stats[11];
            var piercing = hitter.Stats[10];

            // def calculation from incoming damage type
            return damageType switch {
                DamageTypes.Physical => Math.Max(0, armor - penetration),
                DamageTypes.Magical => Math.Max(0, resistance - piercing),
                _ => 0
            };
        }
    }
}