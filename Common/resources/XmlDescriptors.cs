using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using common.terrain;
using NLog;

namespace common.resources
{
    public class ConditionEffect
    {
        public ConditionEffectIndex Effect;
        public int DurationMS;
        public float Range;

        public ConditionEffect()
        {
        }

        public ConditionEffect(XElement e)
        {
            Effect = Utils.GetEffect(e.Value);
            DurationMS = (int)(e.GetAttribute<float>("duration") * 1000);
            Range = e.GetAttribute<float>("range");
        }
    }

    public class ProjectileDesc
    {
        public readonly int BulletType;
        public readonly string ObjectId;
        public readonly DamageTypes DamageType;
        public readonly float LifetimeMS;
        public readonly float Speed;
        public readonly int Size;
        public readonly int MinDamage;
        public readonly int MaxDamage;

        public readonly bool MultiHit;
        public readonly bool PassesCover;
        public readonly bool Parametric;
        public readonly bool Boomerang;
        public readonly bool ParticleTrail;
        public readonly bool Wavy;

        public readonly ConditionEffect[] Effects;

        public readonly float Amplitude;
        public readonly float Frequency;
        public readonly float Magnitude;

        public readonly float Acceleration;
        public readonly float MSPerAcceleration;
        public readonly float SpeedCap;

        public ProjectileDesc(XElement e)
        {
            BulletType = e.GetAttribute<int>("id");
            ObjectId = e.GetValue<string>("ObjectId");
            DamageType = e.ParseDamageType("DamageType");
            LifetimeMS = e.GetValue<float>("LifetimeMS");
            Speed = e.GetValue<float>("Speed", 100);
            Size = e.GetAttribute("Size", 100);

            var dmg = e.Element("Damage");
            if (dmg != null)
                MinDamage = MaxDamage = e.GetValue<int>("Damage");
            else
            {
                MinDamage = e.GetValue<int>("MinDamage");
                MaxDamage = e.GetValue<int>("MaxDamage");
            }

            List<ConditionEffect> effects = new();
            foreach (var i in e.Elements("ConditionEffect"))
                effects.Add(new ConditionEffect(i));
            Effects = effects.ToArray();

            MultiHit = e.HasElement("MultiHit");
            PassesCover = e.HasElement("PassesCover");
            Wavy = e.HasElement("Wavy");
            Parametric = e.HasElement("Parametric");
            Boomerang = e.HasElement("Boomerang");
            ParticleTrail = e.HasElement("ParticleTrail");

            Amplitude = e.GetValue<float>("Amplitude", 0);
            Frequency = e.GetValue<float>("Frequency", 1);
            Magnitude = e.GetValue<float>("Magnitude", 3);

            Acceleration = e.ParseFloat("Acceleration");
            MSPerAcceleration = e.ParseFloat("MSPerAcceleration", 50);
            SpeedCap = e.ParseFloat("SpeedCap", Speed + Acceleration * 10);
        }
    }

    public class ActivateEffect
    {
        public readonly ActivateEffects Effect;
        public readonly int Stats;
        public readonly int Amount;
        public readonly float Range;
        public readonly float DurationSec;
        public readonly int DurationMS;
        public readonly int DurationMS2;
        public readonly ConditionEffectIndex? ConditionEffect;
        public readonly ConditionEffectIndex? CheckExistingEffect;
        public readonly float EffectDuration;
        public readonly int MaximumDistance;
        public readonly float Radius;
        public readonly int TotalDamage;
        public readonly string ObjectId;
        public readonly string ObjectId2;
        public readonly int AngleOffset;
        public readonly int MaxTargets;
        public readonly string Id;
        public readonly string DungeonName;
        public readonly string LockedName;
        public readonly uint Color;
        public readonly ushort SkinType;
        public readonly int Size;
        public readonly bool NoStack;
        public readonly bool UseWisMod;
        public readonly string Target;
        public readonly string Center;
        public readonly int VisualEffect;
        public readonly double ThrowTime;
        public readonly int ImpactDamage;

        public readonly string ObjType;
        public readonly int MaxAmount;

        public readonly int NumShots;
        public readonly float WismodMult;
        public readonly int[] BoostValues;
        public readonly int[] BoostValuesStats;
        public readonly string[] BoostValuesStatsString;
        public readonly string TransformationSkin;
        public readonly int TransformationSkinSize;
        public readonly int[] Chances;

        public readonly int MaxRoll;
        public readonly string[] ConditionEffects;
        public readonly string StatName;

        public readonly int ManaDrain;
        public readonly int LightGain;
        public readonly bool Shoots;

        public readonly float EffectiveLoss;

        public readonly int DecreaseDamage;
        public readonly float WisDamageBase;
        public readonly int WisPerTarget;
        public readonly DamageTypes DamageType;

        public ActivateEffect(XElement e)
        {
            Effect = (ActivateEffects)Enum.Parse(typeof(ActivateEffects), e.Value);

            StatName = e.GetAttribute<string>("stat");
            Stats = StatUtils.StatNameToId(StatName);

            if (e.HasAttribute("statId"))
                Stats = e.GetAttribute<int>("statId");

            Amount = e.GetAttribute<int>("amount");
            Range = e.GetAttribute<float>("range");

            DurationSec = e.GetAttribute<float>("duration");
            DurationMS = (int)(DurationSec * 1000.0f);
            if (e.HasAttribute("duration2"))
                DurationMS = (int)(e.GetAttribute<float>("duration2") * 1000);

            if (e.HasAttribute("effect"))
                ConditionEffect = Utils.GetEffect(e.GetAttribute<string>("effect"));

            if (e.HasAttribute("condEffect"))
                ConditionEffect = Utils.GetEffect(e.GetAttribute<string>("condEffect"));

            if (e.HasAttribute("checkExistingEffect"))
                CheckExistingEffect = Utils.GetEffect(e.GetAttribute<string>("checkExistingEffect"));

            EffectDuration = e.GetAttribute<float>("condDuration");
            MaximumDistance = e.GetAttribute<int>("maxDistance");
            Radius = e.GetAttribute<float>("radius");
            TotalDamage = e.GetAttribute<int>("totalDamage");
            ObjectId = e.GetAttribute<string>("objectId");
            if (e.HasAttribute("objectId2"))
                ObjectId = e.GetAttribute<string>("objectId2");
            AngleOffset = e.GetAttribute<int>("angleOffset");
            MaxTargets = e.GetAttribute<int>("maxTargets");
            Id = e.GetAttribute<string>("id");
            DungeonName = e.GetAttribute<string>("dungeonName");
            LockedName = e.GetAttribute<string>("lockedName");

            if (e.HasAttribute("color"))
            {
                Color = e.GetAttribute<uint>("color", 0xddff00);
            }

            SkinType = e.GetAttribute<ushort>("skinType");
            Size = e.GetAttribute<int>("size");
            NoStack = e.GetAttribute<bool>("noStack");
            UseWisMod = e.GetAttribute<bool>("useWisMod");
            Target = e.GetAttribute<string>("target");
            Center = e.GetAttribute<string>("center");
            VisualEffect = e.GetAttribute<int>("visualEffect");
            ThrowTime = (e.GetAttribute("throwTime", 0.8) * 1000);
            ImpactDamage = e.GetAttribute<int>("impactDamage");
            ObjType = e.GetAttribute<string>("objType");
            MaxAmount = e.GetAttribute("maxAmount", 5);
            NumShots = e.GetAttribute("numShots", 20);
            WismodMult = e.GetAttribute<float>("wismodMult", 1);
            if (e.HasAttribute("boostAmounts"))
                BoostValues = e.GetAttribute<string>("boostAmounts").CommaToArray<int>();
            if (e.HasAttribute("boostStats"))
            {
                BoostValuesStatsString = e.GetAttribute<string>("boostStats").CommaToArray<string>();
                BoostValuesStats = StatUtils.ArrayStatNameToId(BoostValuesStatsString);
            }

            TransformationSkin = e.GetAttribute<string>("transformSkin");
            TransformationSkinSize = e.GetAttribute("transformSize", 100);
            if (e.HasAttribute("chances"))
                Chances = e.GetAttribute<string>("chances").CommaToArray<int>();
            if (e.HasAttribute("condEffs"))
                ConditionEffects = e.GetAttribute<string>("condEffs").CommaToArray<string>();
            MaxRoll = e.GetAttribute("maxRoll", 100);
            ManaDrain = e.GetAttribute("manaDrain", 10);
            LightGain = e.GetAttribute("lightGain", 5);
            Shoots = e.GetAttribute("shoots", true);
            EffectiveLoss = e.GetAttribute("effectiveLoss", 0.025f);
            DecreaseDamage = e.GetAttribute<int>("decreaseDmg");
            WisDamageBase = e.GetAttribute<float>("wisDmgBase");
            WisPerTarget = e.GetAttribute("wisPerTarget", 10);
            DamageType = e.ParseDamageType("damageType", DamageTypes.Magical);
        }
    }

    public class Setpiece
    {
        public readonly string Type;
        public readonly ushort Slot;
        public readonly ushort ItemType;

        public Setpiece(XElement elem)
        {
            Type = elem.Value;
            Slot = elem.GetAttribute<ushort>("slot");
            ItemType = elem.GetAttribute<ushort>("itemtype");
        }
    }

    public class PortalDesc : ObjectDesc
    {
        public readonly int Timeout;
        public readonly bool NexusPortal;
        public readonly bool Locked;

        public PortalDesc(ushort type, XElement e) : base(type, e)
        {
            NexusPortal = e.HasElement("NexusPortal");
            Locked = e.HasElement("LockedPortal");
            Timeout = e.GetValue("Timeout", 30);
        }
    }

    public class Item
    {
        public readonly ushort ObjectType;
        public readonly string ObjectId;
        public readonly int SlotType;
        public readonly int Tier;
        public readonly string Description;
        public readonly float RateOfFire;
        public readonly bool Usable;
        public readonly int BagType;
        public readonly int MpCost;
        public readonly int MpCost2;
        public readonly int MpEndCost;
        public readonly int MpEndCost2;
        public readonly int XpBonus;
        public readonly int NumProjectiles;
        public readonly float ArcGap;
        public readonly float ArcGap1;
        public readonly float ArcGap2;
        public readonly int NumProjectiles1;
        public readonly int NumProjectiles2;
        public readonly bool DualShooting;
        public readonly bool Consumable;
        public readonly bool Potion;
        public readonly string DisplayId;
        public readonly string DisplayName;
        public readonly string SuccessorId;
        public readonly bool Soulbound;
        public readonly bool Undead;
        public readonly bool PUndead;
        public readonly bool SUndead;
        public readonly float Cooldown;
        public readonly float Cooldown2;
        public readonly bool Resurrects;
        public readonly int Texture1;
        public readonly int Texture2;
        public readonly bool Secret;
        public readonly int FeedPower;
        public readonly bool Untiered;

        public readonly int LevelRequirement;

        public readonly string Power;
        public readonly int LightCost;
        public readonly int LightEndCost;
        public readonly int HpCost;
        public readonly float MinQuality;
        public readonly float MaxQuality;
        public readonly int Quantity;
        public readonly int MaxQuantity;

        public readonly bool Rune;

        public readonly KeyValuePair<int, int>[] StatsBoost;
        public readonly KeyValuePair<int, float>[] StatsBoostPerc;
        public readonly ActivateEffect[] ActivateEffects;
        public readonly ActivateEffect[] ActivateEffects2;
        public readonly ProjectileDesc[] Projectiles;
        public readonly RuneBoosts RuneBoosts;

        public Item(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            SlotType = e.GetValue<int>("SlotType");
            if (e.HasElement("Tier"))
                Tier = e.GetValue<int>("Tier");
            else
                Untiered = true;
            Description = e.GetValue<string>("Description");
            RateOfFire = e.GetValue<float>("RateOfFire", 1);
            Usable = e.HasElement("Usable");
            BagType = e.GetValue<int>("BagType");
            MpCost = e.GetValue<int>("MpCost");
            MpEndCost = e.GetValue<int>("MpEndCost");
            XpBonus = e.GetValue<int>("XpBonus");
            NumProjectiles = e.GetValue("NumProjectiles", 1);
            NumProjectiles1 = e.GetValue("NumProjectiles1", 1);
            NumProjectiles2 = e.GetValue("NumProjectiles2", 1);
            ArcGap = e.GetValue("ArcGap", 11.25f);
            ArcGap1 = e.GetValue("ArcGap1", 11.25f);
            ArcGap2 = e.GetValue("ArcGap2", 11.25f);
            DualShooting = e.HasElement("DualShooting");
            Consumable = e.HasElement("Consumable");
            Potion = e.HasElement("Potion");
            DisplayId = e.GetValue<string>("DisplayId");
            DisplayName = string.IsNullOrWhiteSpace(DisplayId) ? ObjectId : DisplayId;
            FeedPower = e.GetValue<int>("FeedPower");
            SuccessorId = e.GetValue<string>("SuccessorId");
            Soulbound = e.HasElement("Soulbound");
            Undead = e.HasElement("Undead");
            PUndead = e.HasElement("PUndead");
            SUndead = e.HasElement("SUndead");
            Secret = e.HasElement("Secret");
            Cooldown = e.GetValue("Cooldown", 0.5f);
            Resurrects = e.HasElement("Resurrects");
            Texture1 = e.GetValue<int>("Tex1");
            Texture2 = e.GetValue<int>("Tex2");
            LevelRequirement = e.GetValue<int>("LevelReq");
            Power = e.GetValue<string>("Power");
            MpCost2 = e.GetValue<int>("MpCost2");
            MpEndCost2 = e.GetValue<int>("MpEndCost2");
            LightCost = e.GetValue<int>("LightCost");
            LightEndCost = e.GetValue<int>("LightEndCost");
            HpCost = e.GetValue<int>("HpCost");
            MinQuality = e.GetValue("MinQuality", 0.85f);
            MaxQuality = e.GetValue("MaxQuality", 1.16f); // 1.15f
            if (Potion)
                MaxQuality = MinQuality = 1;
            Quantity = e.GetValue<int>("Quantity");
            MaxQuantity = e.GetValue<int>("MaxQuantity");
            Rune = e.HasElement("Rune");
            Cooldown2 = e.GetValue("Cooldown2", 0.5f);
            
            var stats = new List<KeyValuePair<int, int>>();
            foreach (var i in e.Elements("ActivateOnEquip"))
            {
                var statName = i.GetAttribute<string>("stat");
                var stat = StatUtils.StatNameToId(statName);

                if (i.HasAttribute("statId"))
                    stat = i.GetAttribute<int>("statId");

                stats.Add(new KeyValuePair<int, int>(
                    stat, i.GetAttribute<int>("amount")));
            }

            StatsBoost = stats.ToArray();
            
            var statsPerc = new List<KeyValuePair<int, float>>();
            foreach (var i in e.Elements("ActivateOnEquipPerc"))
            {
                var statName = i.GetAttribute<string>("stat");
                var stat = StatUtils.StatNameToId(statName);

                if (i.HasAttribute("statId"))
                    stat = i.GetAttribute<int>("statId");

                statsPerc.Add(new KeyValuePair<int, float>(
                    stat, i.GetAttribute<float>("amount")));
            }

            StatsBoostPerc = statsPerc.ToArray();

            var activate = new List<ActivateEffect>();
            foreach (var i in e.Elements("Activate"))
                activate.Add(new ActivateEffect(i));
            ActivateEffects = activate.ToArray();

            var projs = new List<ProjectileDesc>();
            foreach (var i in e.Elements("Projectile"))
                projs.Add(new ProjectileDesc(i));
            Projectiles = projs.ToArray();

            var activate2 = new List<ActivateEffect>();
            foreach (var i in e.Elements("Activate2"))
                activate2.Add(new ActivateEffect(i));
            ActivateEffects2 = activate2.ToArray();

            RuneBoosts = e.HasElement("RuneBoosts") ? new RuneBoosts(e.Element("RuneBoosts")) : null;
        }
    }

    public class RuneBoosts
    {
        public readonly int PhysicalDmg;
        public readonly int MagicalDmg;
        public readonly int EarthDmg;
        public readonly int AirDmg;
        public readonly int ProfaneDmg;
        public readonly int FireDmg;
        public readonly int WaterDmg;
        public readonly int HolyDmg;

        public readonly KeyValuePair<byte, short>[] StatsBoost;

        public RuneBoosts(XElement e)
        {
            PhysicalDmg = e.GetValue("PhysicalDmg", 0);
            MagicalDmg = e.GetValue("MagicalDmg", 0);
            EarthDmg = e.GetValue("EarthDmg", 0);
            AirDmg = e.GetValue("AirDmg", 0);
            ProfaneDmg = e.GetValue("ProfaneDmg", 0);
            FireDmg = e.GetValue("FireDmg", 0);
            WaterDmg = e.GetValue("WaterDmg", 0);
            HolyDmg = e.GetValue("HolyDmg", 0);

            var stats = new List<KeyValuePair<byte, short>>();
            foreach (var i in e.Elements("StatOnEquip"))
            {
                var statName = i.GetAttribute<string>("stat");
                var stat = (byte)StatUtils.StatNameToId(statName);

                if (i.HasAttribute("statId"))
                    stat = i.GetAttribute<byte>("statId");

                stats.Add(new KeyValuePair<byte, short>(stat, (short)i.ParseUshort("amount")));
            }

            StatsBoost = stats.ToArray();
        }
    }

    public class EquipmentSetDesc
    {
        public ushort Type { get; private set; }
        public string Id { get; private set; }

        public ActivateEffect[] ActivateOnEquipAll { get; private set; }
        public Setpiece[] Setpieces { get; private set; }

        public static EquipmentSetDesc FromElem(ushort type, XElement setElem, out ushort skinType)
        {
            skinType = 0;

            var activate = new List<ActivateEffect>();
            foreach (var i in setElem.Elements("ActivateOnEquipAll"))
            {
                var ae = new ActivateEffect(i);
                activate.Add(ae);

                if (ae.SkinType != 0)
                    skinType = ae.SkinType;
            }

            var setpiece = new List<Setpiece>();
            foreach (var i in setElem.Elements("Setpiece"))
                setpiece.Add(new Setpiece(i));

            var eqSet = new EquipmentSetDesc();
            eqSet.Type = type;
            eqSet.Id = setElem.GetAttribute<string>("id");
            eqSet.ActivateOnEquipAll = activate.ToArray();
            eqSet.Setpieces = setpiece.ToArray();

            return eqSet;
        }
    }

    public class SkinDesc
    {
        public ushort Type { get; private set; }
        public ObjectDesc ObjDesc { get; private set; }

        public ushort PlayerClassType { get; private set; }
        public ushort UnlockLevel { get; private set; }
        public bool Restricted { get; private set; }
        public bool Expires { get; private set; }
        public int Cost { get; private set; }
        public bool UnlockSpecial { get; private set; }
        public bool NoSkinSelect { get; private set; }
        public int Size { get; private set; }
        public string PlayerExclusive { get; private set; }

        public static SkinDesc FromElem(ushort type, XElement skinElem)
        {
            var pct = skinElem.Element("PlayerClassType");
            if (pct == null) return null;

            var sd = new SkinDesc();
            sd.Type = type;
            sd.ObjDesc = new ObjectDesc(type, skinElem);
            sd.PlayerClassType = (ushort)Utils.FromString(pct.Value);
            sd.Restricted = skinElem.HasElement("Restricted");
            sd.Expires = skinElem.HasElement("Expires");
            sd.UnlockSpecial = skinElem.HasElement("UnlockSpecial");
            sd.NoSkinSelect = skinElem.HasElement("NoSkinSelect");
            sd.PlayerExclusive = skinElem.GetValue<string>("PlayerExclusive");

            var ul = skinElem.Element("UnlockLevel");
            if (ul != null) sd.UnlockLevel = ushort.Parse(ul.Value);
            sd.Cost = skinElem.GetValue("Cost", 1000);
            sd.Size = skinElem.GetAttribute("size", 100);

            return sd;
        }
    }

    public class SpawnCount
    {
        public readonly int Mean;
        public readonly int StdDev;
        public readonly int Min;
        public readonly int Max;

        public SpawnCount(XElement elem)
        {
            Mean = elem.GetValue<int>("Mean");
            StdDev = elem.GetValue<int>("StdDev");
            Min = elem.GetValue<int>("Min");
            Max = elem.GetValue<int>("Max");
        }
    }

    public class UnlockClass
    {
        public readonly ushort? Type;
        public readonly ushort? Level;
        public readonly uint? Cost;

        public UnlockClass(XElement e)
        {
            var n = e.Element("UnlockLevel");
            if (n != null && n.HasAttribute("type") && n.HasAttribute("level"))
            {
                Type = n.GetAttribute<ushort>("type");
                Level = n.GetAttribute<ushort>("level");
            }

            n = e.Element("UnlockCost");
            if (n != null)
            {
                Cost = (uint)int.Parse(n.Value);
            }
        }
    }

    public class Stat
    {
        public readonly string Type;
        public readonly int MaxValue;
        public readonly int StartingValue;
        public readonly int MinIncrease;
        public readonly int MaxIncrease;

        public Stat(int index, XElement e)
        {
            Type = StatIndexToName(index);
            var x = e.Element(Type);
            if (x != null)
            {
                StartingValue = int.Parse(x.Value);
                MaxValue = x.GetAttribute<int>("max");
            }

            var y = e.Elements("LevelIncrease");
            foreach (var s in y)
                if (s.Value == Type)
                {
                    MinIncrease = s.GetAttribute<int>("min");
                    MaxIncrease = s.GetAttribute<int>("max");
                    break;
                }
        }

        private static string StatIndexToName(int index)
        {
            switch (index)
            {
                case 0: return "MaxHitPoints";
                case 1: return "MaxMagicPoints";
                case 2: return "Strength";
                case 3: return "Armor";
                case 4: return "Agility";
                case 5: return "Dexterity";
                case 6: return "Stamina";
                case 7: return "Intelligence";
                case 19: return "Resistance";
                case 20: return "Wit";
            }

            return null;
        }
    }

    public class PlayerDesc : ObjectDesc
    {
        public readonly int[] SlotTypes;
        public readonly ushort[] Equipment;
        public readonly Stat[] Stats;
        public readonly UnlockClass Unlock;

        public PlayerDesc(ushort type, XElement e) : base(type, e)
        {
            SlotTypes = e.GetValue<string>("SlotTypes").CommaToArray<int>();
            Equipment = e.GetValue<string>("Equipment").CommaToArray<ushort>();
            Stats = new Stat[23];
            for (var i = 0; i < Stats.Length; i++)
                Stats[i] = new Stat(i, e);
            if (e.HasElement("UnlockLevel") || e.HasElement("UnlockCost"))
                Unlock = new UnlockClass(e);
        }
    }

    public class ObjectDesc
    {
        public readonly ushort ObjectType;
        public readonly string ObjectId;
        public readonly string DisplayId;
        public readonly string DungeonName;
        public readonly string Group;
        public readonly string Class;
        public readonly bool Character;
        public readonly bool Player;
        public readonly bool Enemy;
        public readonly bool OccupySquare;
        public readonly bool FullOccupy;
        public readonly bool EnemyOccupySquare;
        public readonly bool Static;
        public readonly bool BlocksSight;
        public readonly bool NoMiniMap;
        public readonly bool ProtectFromGroundDamage;
        public readonly bool ProtectFromSink;
        public readonly bool Flying;
        public readonly bool ShowName;
        public readonly bool DontFaceAttacks;
        public readonly int MinSize;
        public readonly int MaxSize;
        public readonly int SizeStep;
        public readonly TagList Tags;
        public readonly ProjectileDesc[] Projectiles;
        public readonly int MaxHP;
        public readonly int Armor;
        public readonly int Resistance;
        public readonly int Tenacity;
        public readonly int Agility;
        public readonly SpawnCount Spawn;
        public readonly bool Cube;
        public readonly bool God;
        public readonly bool Quest;
        public readonly int? Level;
        public readonly bool UnarmoredImmune;
        public readonly bool CurseImmune;
        public readonly bool CrippledImmune;
        public readonly bool ParalyzeImmune;
        public readonly bool PetrifyImmune;
        public readonly bool SlowImmune;
        public readonly bool StasisImmune;
        public readonly bool StunImmune;
        public readonly bool Oryx;
        public readonly bool Hero;
        public readonly int? PerRealmMax;
        public readonly float? ExpMultiplier; //Exp gained = level total / 10 * multi
        public readonly bool Restricted;
        public readonly bool IsPet;
        public readonly bool Connects;
        public readonly bool TrollWhiteBag;

        public readonly bool Invincible;
        public readonly bool KeepMinimap;
        public readonly bool Invulnerable;
        public readonly bool UncappedXP;

        // elemental resistances
        public readonly int EarthResistance;
        public readonly int AirResistance;
        public readonly int ProfaneResistance;
        public readonly int WaterResistance;
        public readonly int FireResistance;
        public readonly int HolyResistance;

        public readonly PetDesc PetDesc;

        public ObjectDesc(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            DisplayId = e.GetValue<string>("DisplayId");
            Class = e.GetValue<string>("Class");
            Static = e.HasElement("Static");
            OccupySquare = e.HasElement("OccupySquare");
            FullOccupy = e.HasElement("FullOccupy");
            EnemyOccupySquare = e.HasElement("EnemyOccupySquare");
            BlocksSight = e.HasElement("BlocksSight");
            Enemy = e.HasElement("Enemy");
            MaxHP = e.GetValue<int>("MaxHitPoints");
            Armor = e.GetValue<int>("Armor");
            Resistance = e.GetValue<int>("Resistance");
            Tenacity = e.GetValue<int>("Tenacity");
            Agility = e.GetValue("Agility", 50);
            ExpMultiplier = e.GetValue("XpMult", 1.0f);
            PerRealmMax = e.GetValue<int>("PerRealmMax");
            Group = e.GetValue<string>("Group");
            DungeonName = e.GetValue<string>("DungeonName");
            Character = Class.Equals("Character");
            Player = e.HasElement("Player");
            NoMiniMap = e.HasElement("NoMiniMap");
            ProtectFromGroundDamage = e.HasElement("ProtectFromGroundDamage");
            ProtectFromSink = e.HasElement("ProtectFromSink");
            Flying = e.HasElement("Flying");
            ShowName = e.HasElement("ShowName");
            DontFaceAttacks = e.HasElement("DontFaceAttacks");
            Restricted = e.HasElement("Restricted");

            if (e.HasElement("Size"))
            {
                MinSize = MaxSize = e.GetValue<int>("Size");
                SizeStep = 0;
            }
            else
            {
                MinSize = e.GetValue("MinSize", 100);
                MaxSize = e.GetValue("MaxSize", 100);
                SizeStep = e.GetValue("SizeStep", 0);
            }

            var projs = new List<ProjectileDesc>();
            foreach (var i in e.Elements("Projectile"))
                projs.Add(new ProjectileDesc(i));
            Projectiles = projs.ToArray();

            if (e.HasElement("Spawn"))
                Spawn = new SpawnCount(e.Element("Spawn"));
            God = e.HasElement("God");
            Cube = e.HasElement("Cube");
            Quest = e.HasElement("Quest");
            Level = e.GetValue<int>("Level");

            Tags = new TagList();
            if (e.Elements("Tag").Any())
                foreach (var i in e.Elements("Tag"))
                    Tags.Add(new Tag(i));

            UnarmoredImmune = e.HasElement("UnarmoredImmune");
            CurseImmune = e.HasElement("CurseImmune");
            CrippledImmune = e.HasElement("CrippledImmune");
            ParalyzeImmune = e.HasElement("ParalyzeImmune");
            PetrifyImmune = e.HasElement("PetrifyImmune");
            SlowImmune = e.HasElement("SlowImmune");
            StasisImmune = e.HasElement("StasisImmune");
            StunImmune = e.HasElement("StunImmune");
            Invincible = e.HasElement("Invincible");
            Invulnerable = e.HasElement("Invulnerable");

            Oryx = e.HasElement("Oryx");
            Hero = e.HasElement("Hero");

            IsPet = e.HasElement("Pet");
            Connects = e.HasElement("Connects");
            TrollWhiteBag = e.HasElement("TrollWhiteBag");

            KeepMinimap = e.HasElement("KeepMinimap");
            UncappedXP = e.HasElement("UncappedXP");

            EarthResistance = e.GetValue("EarthResistance", 0);
            AirResistance = e.GetValue("AirResistance", 0);
            ProfaneResistance = e.GetValue("ProfaneResistance", 0);
            WaterResistance = e.GetValue("WaterResistance", 0);
            FireResistance = e.GetValue("FireResistance", 0);
            HolyResistance = e.GetValue("HolyResistance", 0);
            
            if (e.HasElement("PetDesc"))
                PetDesc = new PetDesc(e.Element("PetDesc"));
        }
    }

    public class TagList : List<Tag>
    {
        public bool ContainsTag(string name)
        {
            return this.Any(i => i.Name == name);
        }

        public string TagValue(string name, string value)
        {
            return
                (from i in this where i.Name == name where i.Values.ContainsKey(value) select i.Values[value])
                .FirstOrDefault();
        }
    }

    public class Tag
    {
        public string Name { get; private set; }
        public Dictionary<string, string> Values { get; private set; }

        public Tag(XElement elem)
        {
            Name = elem.GetAttribute<string>("name");
            Values = new Dictionary<string, string>();
            foreach (XElement i in elem.Elements())
            {
                if (Values.ContainsKey(i.Name.ToString()))
                    Values.Remove(i.Name.ToString());
                Values.Add(i.Name.ToString(), i.Value);
            }
        }
    }

    public class PetDesc
    {
        public readonly string Family;
        public readonly string Description;

        public PetDesc(XElement e)
        {
            Family = e.GetAttribute("family", "???");
            Description = e.GetValue<string>("Description");
        }
    }

    public class TileDesc
    {
        public readonly ushort ObjectType;
        public readonly string ObjectId;
        public readonly bool NoWalk;
        public readonly bool Damaging;
        public readonly int MinDamage;
        public readonly int MaxDamage;
        public readonly float Speed;
        public readonly bool Push;
        public readonly float PushX;
        public readonly float PushY;

        public TileDesc(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            NoWalk = e.HasElement("NoWalk");

            if (e.HasElement("MinDamage"))
            {
                MinDamage = e.GetValue<int>("MinDamage");
                Damaging = true;
            }

            if (e.HasElement("MaxDamage"))
            {
                MaxDamage = e.GetValue<int>("MaxDamage");
                Damaging = true;
            }

            Speed = e.GetValue("Speed", 1.0f);
            Push = e.HasElement("Push");
            if (Push)
            {
                var anim = e.Element("Animate");
                if (anim.HasAttribute("dx"))
                    PushX = anim.GetAttribute<float>("dx");
                if (anim.HasAttribute("dy"))
                    PushY = anim.GetAttribute<float>("dy");
            }
        }
    }

    public class DungeonDesc
    {
        public readonly string Name;
        public readonly ushort PortalId;
        public readonly int Background;
        public readonly bool AllowTeleport;
        public readonly string Json;

        public DungeonDesc(XElement e)
        {
            Name = e.GetAttribute<string>("name");
            PortalId = e.GetAttribute<ushort>("type");
            Background = e.GetValue<int>("Background");
            AllowTeleport = e.HasElement("AllowTeleport");
            Json = e.GetValue<string>("Json");
        }
    }

    public class MerchantList
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public readonly TileRegion Region;
        public readonly CurrencyType Currency;
        public readonly int StarsRequired;
        public readonly List<ISellableItem> Items;

        public MerchantList(XElement e, XmlData gameData)
        {
            Region = (TileRegion)Enum.Parse(typeof(TileRegion), e.ParseString("@region").Replace(' ', '_'));
            Currency = (CurrencyType)Enum.Parse(typeof(CurrencyType), e.ParseString("@currency"));
            StarsRequired = e.ParseInt("@starsRequired");
            var idToObjectType = gameData.IdToObjectType;
            Items = new List<ISellableItem>();
            foreach (var i in e.Elements("Item"))
            {
                if (!idToObjectType.TryGetValue(i.Value, out var item))
                {
                    Log.Error($"Failed when adding \"{i.Value}\" to shop. Item does not exist.");
                    continue;
                }
                
                Items.Add(new MerchantItem(i, item));
            }
        }
    }

    public class MerchantItem : ISellableItem
    {
        public readonly string Name;
        public ushort ItemId { get; }
        public int Price { get; }
        public int Count => -1;

        public MerchantItem(XElement e, ushort type)
        {
            ItemId = type;
            Name = e.Value;
            Price = e.ParseInt("@price");
        }

    }

    public class StatUtils
    {
        public static int[] ArrayStatNameToId(string[] arr)
        {
            return arr.Select(x => StatNameToId(x)).ToArray();
        }

        public static int StatNameToId(string stat)
        {
            switch (stat)
            {
                case "MaximumHP": return 0;
                case "MaximumMP": return 3;
                case "Strength": return 20;
                case "Armor": return 21;
                case "Agility": return 22;
                case "Dexterity": return 28;
                case "Stamina": return 26;
                case "Intelligence": return 27;
                case "Luck": return 102;
                case "Haste": return 108;
                case "ShieldPoints": return 110;
                case "Tenacity": return 117;
                case "CriticalStrike": return 119;
                case "LifeSteal": return 121;
                case "LifeStealKill": return 125;
                case "ManaLeech": return 123;
                case "ManaLeechKill": return 127;
                case "Resistance": return 156;
                case "Wit": return 82;
                case "Lethality": return 84;
                case "Piercing": return 86;
                default: return 0;
            }
        }
    }
}