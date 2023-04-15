using System.Globalization;
using Shared;
using GameServer.logic;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;
using Newtonsoft.Json;
using NLog;
using wServer.realm;

namespace GameServer.realm.entities.player
{
    interface IPlayer
    {
        void Damage(int dmg, Entity src, bool noDef);
        bool IsVisibleToEnemy();
    }

    public partial class Player : Character, IContainer, IPlayer
    {
        new static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Client _client;
        public Client Client => _client;

        //Stats
        private readonly SV<int> _accountId;

        public int AccountId
        {
            get => _accountId.GetValue();
            set => _accountId.SetValue(value);
        }

        private readonly SV<int> _experience;

        public int Experience
        {
            get => _experience.GetValue();
            set => _experience.SetValue(value);
        }

        private readonly SV<int> _level;

        public int Level
        {
            get => _level.GetValue();
            set => _level.SetValue(value);
        }

        private readonly SV<int> _currentFame;

        public int CurrentFame
        {
            get => _currentFame.GetValue();
            set => _currentFame.SetValue(value);
        }

        private readonly SV<int> _fame;

        public int Fame
        {
            get => _fame.GetValue();
            set => _fame.SetValue(value);
        }

        private readonly SV<string> _guild;

        public string Guild
        {
            get => _guild.GetValue();
            set => _guild.SetValue(value);
        }

        private readonly SV<int> _guildRank;

        public int GuildRank
        {
            get => _guildRank.GetValue();
            set => _guildRank.SetValue(value);
        }

        private readonly SV<int> _credits;

        public int Credits
        {
            get => _credits.GetValue();
            set => _credits.SetValue(value);
        }

        private readonly SV<int> _unholyEssence;

        public int UnholyEssence
        {
            get => _unholyEssence.GetValue();
            set => _unholyEssence.SetValue(value);
        }

        private readonly SV<int> _divineEssence;

        public int DivineEssence
        {
            get => _divineEssence.GetValue();
            set => _divineEssence.SetValue(value);
        }

        private readonly SV<bool> _nameChosen;

        public bool NameChosen
        {
            get => _nameChosen.GetValue();
            set => _nameChosen.SetValue(value);
        }

        private readonly SV<int> _texture1;

        public int Texture1
        {
            get => _texture1.GetValue();
            set => _texture1.SetValue(value);
        }

        private readonly SV<int> _texture2;

        public int Texture2
        {
            get => _texture2.GetValue();
            set => _texture2.SetValue(value);
        }

        private int _originalSkin;
        private readonly SV<int> _skin;

        public int Skin
        {
            get => _skin.GetValue();
            set => _skin.SetValue(value);
        }

        public int PrevSkin;
        public int PrevSize;

        private readonly SV<int> _glow;

        public int Glow
        {
            get => _glow.GetValue();
            set => _glow.SetValue(value);
        }

        private readonly SV<int> _mp;

        public int MP
        {
            get => _mp.GetValue();
            set => _mp.SetValue(value);
        }

        private readonly SV<bool> _hasBackpack;

        public bool HasBackpack
        {
            get => _hasBackpack.GetValue();
            set => _hasBackpack.SetValue(value);
        }

        private readonly SV<bool> _xpBoosted;

        public bool XPBoosted
        {
            get => _xpBoosted.GetValue();
            set => _xpBoosted.SetValue(value);
        }

        private readonly SV<int> _oxygenBar;

        public int OxygenBar
        {
            get => _oxygenBar.GetValue();
            set => _oxygenBar.SetValue(value);
        }

        private readonly SV<int> _lightMax;

        public int LightMax
        {
            get => _lightMax.GetValue();
            init => _lightMax.SetValue(value);
        }

        private readonly SV<int> _light;

        public int Light
        {
            get => _light.GetValue();
            set => _light.SetValue(value);
        }

        private readonly SV<int> _rank;

        public int Rank
        {
            get => _rank.GetValue();
            set => _rank.SetValue(value);
        }

        private readonly SV<int> _admin;

        public int Admin
        {
            get => _admin.GetValue();
            set => _admin.SetValue(value);
        }

        private readonly SV<int> _shield;

        public int Shield
        {
            get => _shield.GetValue();
            set => _shield.SetValue(value);
        }

        private readonly SV<int> _shieldMax;

        public int ShieldMax
        {
            get => _shieldMax.GetValue();
            set => _shieldMax.SetValue(value);
        }

        public int XPBoostTime { get; set; }
        public int LDBoostTime { get; set; }
        public int LTBoostTime { get; set; }
        public ushort SetSkin { get; set; }
        public int SetSkinSize { get; set; }
        public Entity PlayerPet { get; set; }
        public int? GuildInvite { get; set; }
        public bool Muted { get; set; }

        public RInventory DbLink { get; private set; }
        public int[] SlotTypes { get; private set; }
        public Inventory Inventory { get; private set; }

        public ItemStacker HealthPots { get; private set; }
        public ItemStacker MagicPots { get; private set; }
        public ItemStacker[] Stacks { get; private set; }

        public readonly StatsManager Stats;

        public int XpBoostItem { get; set; }
        public int DamageDealt { get; set; }

        private readonly SV<int> _offensiveAbility;

        public int OffensiveAbility
        {
            get => _offensiveAbility.GetValue();
            set => _offensiveAbility.SetValue(value);
        }

        private readonly SV<int> _defensiveAbility;

        public int DefensiveAbility
        {
            get => _defensiveAbility.GetValue();
            set => _defensiveAbility.SetValue(value);
        }
        
        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            base.ExportStats(stats);
            stats[StatsType.AccountId] = AccountId.ToString();
            stats[StatsType.Guild] = Guild;
            stats[StatsType.GuildRank] = GuildRank;
            stats[StatsType.Texture1] = Texture1;
            stats[StatsType.Texture2] = Texture2;
            stats[StatsType.Skin] = Skin;
            stats[StatsType.Glow] = Glow;
            stats[StatsType.MP] = MP;
            stats[StatsType.Inventory0] = Inventory[0]?.ObjectType ?? -1;
            stats[StatsType.Inventory1] = Inventory[1]?.ObjectType ?? -1;
            stats[StatsType.Inventory2] = Inventory[2]?.ObjectType ?? -1;
            stats[StatsType.Inventory3] = Inventory[3]?.ObjectType ?? -1;
            stats[StatsType.Inventory4] = Inventory[4]?.ObjectType ?? -1;
            stats[StatsType.Inventory5] = Inventory[5]?.ObjectType ?? -1;
            stats[StatsType.Inventory6] = Inventory[6]?.ObjectType ?? -1;
            stats[StatsType.Inventory7] = Inventory[7]?.ObjectType ?? -1;
            stats[StatsType.Inventory8] = Inventory[8]?.ObjectType ?? -1;
            stats[StatsType.Inventory9] = Inventory[9]?.ObjectType ?? -1;
            stats[StatsType.Inventory10] = Inventory[10]?.ObjectType ?? -1;
            stats[StatsType.Inventory11] = Inventory[11]?.ObjectType ?? -1;
            stats[StatsType.BackPack0] = Inventory[12]?.ObjectType ?? -1;
            stats[StatsType.BackPack1] = Inventory[13]?.ObjectType ?? -1;
            stats[StatsType.BackPack2] = Inventory[14]?.ObjectType ?? -1;
            stats[StatsType.BackPack3] = Inventory[15]?.ObjectType ?? -1;
            stats[StatsType.BackPack4] = Inventory[16]?.ObjectType ?? -1;
            stats[StatsType.BackPack5] = Inventory[17]?.ObjectType ?? -1;
            stats[StatsType.BackPack6] = Inventory[18]?.ObjectType ?? -1;
            stats[StatsType.BackPack7] = Inventory[19]?.ObjectType ?? -1;
            stats[StatsType.MaximumHP] = Stats[0];
            stats[StatsType.MaximumMP] = Stats[1];
            stats[StatsType.Strength] = Stats[2];
            stats[StatsType.Stamina] = Stats[6];
            stats[StatsType.Luck] = Stats[10];
            stats[StatsType.HPBoost] = Stats.Boost[0];
            stats[StatsType.MPBoost] = Stats.Boost[1];
            stats[StatsType.StrengthBonus] = Stats.Boost[2];
            stats[StatsType.StaminaBonus] = Stats.Boost[6];
            stats[StatsType.LuckBonus] = Stats.Boost[10];
            stats[StatsType.HealthStackCount] = HealthPots.Count;
            stats[StatsType.MagicStackCount] = MagicPots.Count;
            stats[StatsType.HasBackpack] = (HasBackpack) ? 1 : 0;
        }

        public void SaveToCharacter()
        {
            var chr = _client.Character;
            chr.Level = Level;
            chr.Experience = Experience;
            chr.Fame = Fame;
            chr.HP = Math.Max(1, HP);
            chr.MP = MP;
            chr.Stats = Stats.Base.GetStats();
            chr.Tex1 = Texture1;
            chr.Tex2 = Texture2;
            chr.Skin = _originalSkin;
            chr.FameStats = FameCounter.Stats.Write();
            chr.LastSeen = DateTime.Now;
            chr.HealthStackCount = HealthPots.Count;
            chr.MagicStackCount = MagicPots.Count;
            chr.HasBackpack = HasBackpack;
            chr.XPBoostTime = XPBoostTime;
            chr.LDBoostTime = LDBoostTime;
            chr.LTBoostTime = LTBoostTime;
            chr.Items = Inventory.GetItemTypes();
            chr.Light = Light;
            chr.OffensiveAbility = OffensiveAbility;
            chr.DefensiveAbility = DefensiveAbility;
        }

        public Player(Client client, bool saveInventory = true)
            : base(client.Manager, client.Character.ObjectType)
        {
            var settings = Manager.Resources.Settings;
            var gameData = Manager.Resources.GameData;

            _client = client;

            // found in player.update partial
            Sight = new Sight(this);
            _clientEntities = new UpdatedSet(this);

            _accountId = new SV<int>(this, StatsType.AccountId, client.Account.AccountId, true);
            _guild = new SV<string>(this, StatsType.Guild, "");
            _guildRank = new SV<int>(this, StatsType.GuildRank, -1);
            _texture1 = new SV<int>(this, StatsType.Texture1, client.Character.Tex1);
            _texture2 = new SV<int>(this, StatsType.Texture2, client.Character.Tex2);
            _skin = new SV<int>(this, StatsType.Skin, 0);
            _glow = new SV<int>(this, StatsType.Glow, 0);
            _mp = new SV<int>(this, StatsType.MP, client.Character.MP);
            _hasBackpack = new SV<bool>(this, StatsType.HasBackpack, client.Character.HasBackpack, true);
            
            Name = client.Account.Name;
            HP = client.Character.HP;
            ConditionEffects = 0;

            XPBoostTime = client.Character.XPBoostTime;
            LDBoostTime = client.Character.LDBoostTime;
            LTBoostTime = client.Character.LTBoostTime;

            var s = (ushort)client.Character.Skin;
            if (gameData.Skins.Keys.Contains(s))
            {
                SetDefaultSkin(s);
                PrevSkin = client.Character.Skin;
                SetDefaultSize(gameData.Skins[s].Size);
                PrevSize = gameData.Skins[s].Size;
            }

            var guild = Manager.Database.GetGuild(client.Account.GuildId);
            if (guild?.Name != null)
            {
                Guild = guild.Name;
                GuildRank = client.Account.GuildRank;
            }

            HealthPots = new ItemStacker(this, 254, 0x14E2,
                client.Character.HealthStackCount, settings.MaxStackablePotions);
            MagicPots = new ItemStacker(this, 255, 0x14E3,
                client.Character.MagicStackCount, settings.MaxStackablePotions);
            Stacks = new ItemStacker[] { HealthPots, MagicPots };

            // inventory setup
            DbLink = new DbCharInv(Client.Account, Client.Character.CharId);
            Inventory = new Inventory(this,
                Utils.ResizeArray(
                    (DbLink as DbCharInv).Items
                    .Select(_ => (_ == 0xffff || !gameData.Items.ContainsKey(_)) ? null : gameData.Items[_])
                    .ToArray(),
                    20));
            
            if (!saveInventory)
                DbLink = null;

            Inventory.InventoryChanged += (_, e) =>
            {
                Stats.ReCalculateValues(e);
                SetItemXpBoost();
            };
            
            SlotTypes = Utils.ResizeArray(
                gameData.Classes[ObjectType].SlotTypes,
                settings.InventorySize);
            Stats = new StatsManager(this);

            // set size of player if using set skin
            var skin = (ushort)Skin;
            if (gameData.SkinTypeToEquipSetType.ContainsKey(skin))
            {
                var setType = gameData.SkinTypeToEquipSetType[skin];
                var ae = gameData.EquipmentSets[setType].ActivateOnEquipAll
                    .SingleOrDefault(e => e.SkinType == skin);

                if (ae != null)
                    Size = ae.Size;
            }

            // override size
            if (Client.Account.Size > 0)
                Size = Client.Account.Size;

            Manager.Database.IsMuted(client.IP)
                .ContinueWith(t => { Muted = !Client.Account.Admin && t.IsCompleted && t.Result; });

            Manager.Database.IsLegend(AccountId)
                .ContinueWith(t =>
                {
                    Glow = t.Result && client.Account.GlowColor == 0
                        ? 0xFF0000
                        : client.Account.GlowColor;
                });
            
            SetItemXpBoost();
        }

        byte[,] tiles;
        public FameCounter FameCounter { get; private set; }

        public Entity SpectateTarget { get; set; }
        public bool IsControlling => SpectateTarget != null && !(SpectateTarget is Player);
        

        public override void Init(World owner)
        {
            var x = 0;
            var y = 0;
            var spawnRegions = owner.GetSpawnPoints();
            if (spawnRegions.Any())
            {
                var rand = new Random();
                var sRegion = spawnRegions.ElementAt(rand.Next(0, spawnRegions.Length));
                x = sRegion.Key.X;
                y = sRegion.Key.Y;
            }

            Move(x + 0.5f, y + 0.5f);
            tiles = new byte[owner.Map.Width, owner.Map.Height];

            FameCounter = new FameCounter(this);

            if (owner.Name.Equals("OceanTrench"))
                OxygenBar = 100;

            SetNewbiePeriod();
            base.Init(owner);
        }

        
        public override void Tick(RealmTime time)
        {
            if (!KeepAlive(time))
                return;

            CheckTradeTimeout(time);

            HandleRegen(time);
            HandleEffects(time);
            //HandleOceanTrenchGround(time);
            TickActivateEffects(time);
            FameCounter.Tick(time);

            TickPassiveEffects();
            base.Tick(time);

            SendUpdate(time);
            SendNewTick(time);

            if (HP <= 0)
            {
                Death("Unknown", rekt: true);
            }
        }

        private void TickPassiveEffects()
        {
            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i] == null)
                    continue;
                switch (Inventory[i].Power)
                {
                    case "Lifeline" when (double)HP / Stats.Base[0] <= 0.3 && !OnCooldown(i):
                        var shieldAmt = Stats[3] + Stats[0] / 2;
                        StatBoostSelf(12, shieldAmt, 15);
                        StatBoostSelf(15, 15, 15);
                        SetCooldown(i, 300);
                        continue;
                    case "Godly Vigor" when !Owner.IsNotCombatMapArea && !OnCooldown(i):
                        HealSelf(Stats[0] / 4, true);
                        SetCooldown(i, 10);
                        continue;
                }
            }
        }

        private void SetItemXpBoost()
        {
            var sum = 0;
            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i] == null)
                    continue;

                sum += Inventory[i].XpBonus;
            }

            XpBoostItem = sum;
        }

        void TickActivateEffects(RealmTime time)
        {
            var dt = time.ElapsedMsDelta;

            if (XPBoostTime != 0)
                if (Level >= 300)
                    XPBoostTime = 0;

            if (XPBoostTime > 0)
                XPBoostTime = Math.Max(XPBoostTime - dt, 0);
            if (XPBoostTime == 0)
                XPBoosted = false;

            if (LDBoostTime > 0)
                LDBoostTime = Math.Max(LDBoostTime - dt, 0);

            if (LTBoostTime > 0)
                LTBoostTime = Math.Max(LTBoostTime - dt, 0);
        }

        float _hpRegenCounter;
        float _mpRegenCounter;

        void HandleRegen(RealmTime time)
        {
            // hp regen
            if (HP == Stats[0] || !CanHpRegen())
                _hpRegenCounter = 0;
            else
            {
                _hpRegenCounter += Stats.GetHpRegen() * time.ElapsedMsDelta / 1000f;
                var regen = (int)_hpRegenCounter;
                if (regen > 0)
                {
                    HP = Math.Min(Stats[0], HP + regen);
                    _hpRegenCounter -= regen;
                }
            }

            // mp regen
            if (MP == Stats[1] || !CanMpRegen())
                _mpRegenCounter = 0;
            else
            {
                _mpRegenCounter += Stats.GetMpRegen() * time.ElapsedMsDelta / 1000f;
                var regen = (int)_mpRegenCounter;
                if (regen > 0)
                {
                    MP = Math.Min(Stats[1], MP + regen);
                    _mpRegenCounter -= regen;
                }
            }
        }

        public void TeleportPosition(RealmTime time, float x, float y, bool ignoreRestrictions = false, bool removeNegative = false)
        {
            TeleportPosition(time, new Position() { X = x, Y = y }, ignoreRestrictions, removeNegative);
        }

        public void TeleportPosition(RealmTime time, Position position, bool ignoreRestrictions = false, bool removeNegative = false)
        {
            if (!ignoreRestrictions)
            {
                if (!TPCooledDown())
                {
                    SendError("Too soon to teleport again!");
                    return;
                }

                SetTPDisabledPeriod();
                SetNewbiePeriod();
                FameCounter.Teleport();
            }

            if (removeNegative)
            {
                ApplyConditionEffect(NegativeEffs);
            }

            foreach (var plr in Owner.Players.Values) {
                plr.AwaitGotoAck(time.TotalElapsedMs);
                plr.Client.SendGoto(Id, position.X, position.Y);
                plr.Client.SendShowEffect(EffectType.Teleport, Id, position.X, position.Y, 0, 0, 0xFFFFFF);
            }
        }

        public void Teleport(RealmTime time, int objId, bool ignoreRestrictions = false)
        {
            var obj = Owner.GetEntity(objId);
            if (obj == null)
            {
                SendError("Target does not exist.");
                return;
            }

            if (!ignoreRestrictions)
            {
                if (Id == objId)
                {
                    SendInfo("You are already at yourself, and always will be!");
                    return;
                }

                if (!Owner.AllowTeleport)
                {
                    SendError("Cannot teleport here.");
                    return;
                }

                if (obj is not Player)
                {
                    SendError("Can only teleport to players.");
                    return;
                }
            }

            TeleportPosition(time, obj.X, obj.Y, ignoreRestrictions);
        }

        public bool IsInvulnerable()
        {
            if (HasConditionEffect(ConditionEffects.Invulnerable))
                return true;
            return false;
        }

        public override bool HitByProjectile(Projectile projectile, RealmTime time)
        {
            ushort dmgAmount;
            if (projectile.ProjectileOwner is Player || IsInvulnerable())
                return false;

            var truedmg = (int)Stats.GetDefenseDamage(projectile.Damage, projectile.DamageType, true);
            var dmg = (int)Stats.GetDefenseDamage(projectile.Damage,projectile.DamageType);
            if (Shield > 0)
                dmgAmount = (ushort)truedmg;
            else
                dmgAmount = (ushort)dmg;

            var limit = (int)Math.Min(ShieldMax + 100, ShieldMax * 1.3);
            if (Shield > 0)
                ShieldDamage += truedmg;
            else if (ShieldDamage + truedmg <= limit)
            {
                // more accurate... maybe
                ShieldDamage += truedmg;
                HP -= dmg;
            }
            else
                HP -= dmg;

            ApplyConditionEffect(projectile.ProjDesc.Effects);

            HandleEffectsOnHit(projectile, time);

            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(X, Y, p.X, p.Y) < 16 * 16)
                    p.Client.SendDamage(Id, projectile.ConditionEffects, dmgAmount, HP <= 0, projectile.BulletId, projectile.ProjectileOwner.Self.Id);

            if (HP <= 0)
                Death(projectile.ProjectileOwner.Self.ObjectDesc.DisplayId ??
                      projectile.ProjectileOwner.Self.ObjectDesc.ObjectId,
                    projectile.ProjectileOwner.Self);

            return base.HitByProjectile(projectile, time);
        }

        private void HandleEffectsOnHit(Projectile projectile, RealmTime time)
        {
            var pos = new Position() { X = X, Y = Y };
            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i] == null)
                    continue;
                switch (Inventory[i].Power)
                {
                    case "Demonic Wrath" when (double)HP / Stats.Base[0] <= 0.4 && !OnCooldown(i):
                        DamageBlast(2500, 6, pos, time);
                        SetCooldown(i, 60);
                        continue;
                    case "Thorns" when (double)HP / Stats.Base[0] <= 0.6 && !OnCooldown(i):
                        var amount = 50 + Stats[3];
                        ((Enemy)projectile.ProjectileOwner).Damage(this, time, amount, true);
                        SetCooldown(i, 0.5f);
                        continue;
                    case "Godly Vigor":
                        SetCooldown(i, 10);
                        continue;
                }
            }
        }

        public void Damage(int dmg, Entity src, bool noDef = false)
        {
            if (IsInvulnerable())
                return;

            dmg = (int)Stats.GetDefenseDamage(dmg, DamageTypes.Magical, noDef);
            var truedmg = (int)Stats.GetDefenseDamage(dmg, DamageTypes.Magical, true);
            var limit = (int)Math.Min(ShieldMax + 100, ShieldMax * 1.3);
            if (Shield > 0)
                ShieldDamage += truedmg;
            else if (ShieldDamage + truedmg <= limit)
            {
                // more accurate... maybe
                ShieldDamage += truedmg;
                HP -= dmg;
            }
            else
                HP -= dmg;

            HandleEffectsOnDamage();

            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(X, Y, p.X, p.Y) < 16 * 16)
                    p.Client.SendDamage(Id, 0, (ushort)dmg, HP <= 0, 0, src.Id);

            if (HP <= 0)
                Death(src.ObjectDesc.DisplayId ??
                      src.ObjectDesc.ObjectId,
                    src);
        }

        private void HandleEffectsOnDamage()
        {
            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i] == null)
                    continue;
                switch (Inventory[i].Power)
                {
                    case "Godly Vigor":
                        SetCooldown(i, 10);
                        continue;
                }
            }
        }

        private void GenerateGravestone(bool phantomDeath = false)
        {
            var playerDesc = Manager.Resources.GameData.Classes[ObjectType];
            var maxed = playerDesc.Stats.Where((t, i) => Stats.Base[i] >= t.MaxValue).Count();
            ushort objType;
            int time;
            switch (maxed)
            {
                case 9:
                    objType = 0x042C;
                    time = 600000;
                    break;
                case 8:
                    objType = 0x042C;
                    time = 600000;
                    break;
                case 7:
                    objType = 0x042B;
                    time = 600000;
                    break;
                case 6:
                    objType = 0x042A;
                    time = 600000;
                    break;
                case 5:
                    objType = 0x0429;
                    time = 600000;
                    break;
                case 4:
                    objType = 0x0428;
                    time = 600000;
                    break;
                case 3:
                    objType = 0x0427;
                    time = 600000;
                    break;
                case 2:
                    objType = 0x0426;
                    time = 600000;
                    break;
                case 1:
                    objType = 0x0425;
                    time = 600000;
                    break;
                default:
                    objType = 0x0424;
                    time = 300000;
                    if (Level < 20)
                    {
                        objType = 0x0423;
                        time = 60000;
                    }

                    if (Level <= 1)
                    {
                        objType = 0x0422;
                        time = 30000;
                    }

                    break;
            }

            var obj = new StaticObject(Manager, objType, time, true, true, false);
            obj.Move(X, Y);
            obj.Name = (!phantomDeath) ? Name : $"{Name} got rekt";
            Owner.EnterWorld(obj);
        }

        private bool NonPermaKillEnemy(Entity entity, string killer)
        {
            if (entity == null)
            {
                return false;
            }

            if ((!entity.Spawned && !entity.DevSpawned) && entity.Controller == null)
                return false;

            //foreach (var player in Owner.Players.Values)
            //    player.SendInfo(Name + " was sent home crying by a phantom " + killer);

            GenerateGravestone(true);
            ReconnectToNexus();
            return true;
        }

        private bool Rekted(bool rekt)
        {
            if (!rekt)
                return false;

            GenerateGravestone(true);
            ReconnectToNexus();
            return true;
        }

        private bool TestWorld(string killer)
        {
            if (!(Owner is Test))
                return false;

            GenerateGravestone();
            ReconnectToNexus();
            return true;
        }

        private bool _dead;

        private bool Resurrection()
        {
            for (var i = 0; i < 6; i++)
            {
                var item = Inventory[i];

                if (item == null || !item.Resurrects)
                    continue;

                Inventory[i] = null;
                foreach (var player in Owner.Players.Values)
                    player.SendInfo(
                        $"{Name}'s {item.DisplayName} breaks and they disappear");

                ReconnectToNexus();
                return true;
            }

            return false;
        }

        private void ReconnectToNexus()
        {
            HP = 1;
            _client.Reconnect("Nexus", World.Realm);
        }

        private void AnnounceDeath(string killer)
        {
            var playerDesc = Manager.Resources.GameData.Classes[ObjectType];
            var maxed = playerDesc.Stats.Where((t, i) => Stats.Base[i] >= t.MaxValue && (i < 8 || i > 18)).Count();
            var notableDeath = maxed >= 6 || Fame >= 1000;

            List<string> deathmsgVariations = new();

            string[] commonDeathMsgs =
            {
                $"was slain by {killer}", $"was killed by {killer}", $"died to {killer}", $"was massacred by {killer}",
                $"couldn't handle {killer}", $"was taken care of by {killer}", $"was engraved 6 feet under by {killer}",
                $"expired thanks to {killer}", $"departed when fighting {killer}",
                $"is no more thanks to {killer}", $"couldn't take care of {killer}", $"couldn't kill {killer}",
                $"was too kind when facing {killer}", $"got the worst death message variation when dueling {killer}",
                $"was permanently eliminated by {killer}", $"got banished from the world of the living by {killer}",
                $"proved Oryx's point by dying to {killer}", $"got smashed by {killer}", $"was deleted by {killer}", 
                $"lost the life or death duel against {killer}"
            };
            deathmsgVariations.AddRange(commonDeathMsgs);
            if (notableDeath)
            {
                #region Notable Death death message variations

                deathmsgVariations.AddRange(new []
                    {
                        $"took the L against {killer}",
                        $"rage quit thanks to {killer}",
                        $"left the server thanks to {killer}",
                        $"was timed out by {killer}",
                        $"has a skill issue and can't kill {killer}",
                        $"is trippin', {killer}!",
                        $"got soft wiped by {killer}",
                        $"was weaker than {killer}",
                        $"got slam dunked by {killer}",
                        $"lost all that they had thanks to {killer}"
                    });

                #endregion
            }

            var deathMessage =
                $"{Name} [Level {Level}, {_client.Character.FinalFame} Fame, {maxed}/9] {deathmsgVariations.PickRandom()}";

            // notable deaths
            if (notableDeath && !Client.Account.Admin)
            {
                foreach (var w in Manager.Worlds.Values)
                foreach (var p in w.Players.Values)
                    p.SendHelp(deathMessage);
                return;
            }

            var pGuild = Client.Account.GuildId;

            // guild case, only for level 25 or higher
            if (pGuild > 0 && Level >= 25)
            {
                foreach (var w in Manager.Worlds.Values)
                {
                    foreach (var p in w.Players.Values)
                    {
                        if (p.Client.Account.GuildId == pGuild)
                        {
                            p.SendGuildDeath(deathMessage);
                        }
                    }
                }

                foreach (var i in Owner.Players.Values)
                {
                    if (i.Client.Account.GuildId != pGuild)
                    {
                        i.SendInfo(deathMessage);
                    }
                }
            }
            // guildless case
            else
            {
                foreach (var i in Owner.Players.Values)
                {
                    i.SendInfo(deathMessage);
                }
            }
        }

        public void Death(string killer, Entity entity = null, WmapTile tile = null, bool rekt = false)
        {
            if (_dead)
                return;

            if (tile != null && (tile.Spawned || tile.DevSpawned))
            {
                rekt = true;
            }

            _dead = true;

            if (Rekted(rekt))
                return;
            if (NonPermaKillEnemy(entity, killer))
                return;
            if (TestWorld(killer))
                return;
            if (Resurrection())
                return;

            SaveToCharacter();
            Manager.Database.Death(Manager.Resources.GameData, _client.Account,
                _client.Character, FameCounter.Stats, killer);

            GenerateGravestone();
            AnnounceDeath(killer);

            _client.SendDeath(AccountId, _client.Character.CharId, killer);

            Owner.Timers.Add(new WorldTimer(1000, (w, t) =>
            {
                if (_client.Player != this)
                    return;

                _client.Disconnect();
            }));
        }

        private bool CheckLimitedWorlds(World world)
        {
            if (world.Name == "Sex Land" && Level < 300)
            {
                SendError("no");
                return false;
            }

            return true;
        }

        public void Reconnect(World world)
        {
            if (!CheckLimitedWorlds(world))
                return;

            Client.Reconnect(world.Name, world.Id);
        }

        public void Reconnect(object portal, World world)
        {
            ((Portal)portal).WorldInstanceSet -= Reconnect;

            if (world == null)
                SendError("Portal Not Implemented!");
            else
            {
                if (!CheckLimitedWorlds(world))
                    return;

                Client.Reconnect(world.Name, world.Id);
            }
        }
        
        private void ShowNotification(string notif, uint color = 0xFF00FF00)
        {
            if (Owner is null)
            {
                return;
            }
            
            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(X, Y, p.X, p.Y) < 16 * 16)
                    p.Client.SendNotification(Id, notif, color);
        }

        public int GetCurrency(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Gold:
                    return Credits;
                case CurrencyType.Fame:
                    return CurrentFame;
                default:
                    return 0;
            }
        }

        public void SetCurrency(CurrencyType currency, int amount)
        {
            switch (currency)
            {
                case CurrencyType.Gold:
                    Credits = amount;
                    break;
                case CurrencyType.Fame:
                    CurrentFame = amount;
                    break;
            }
        }

        public override void Move(float x, float y)
        {
            if (SpectateTarget != null && !(SpectateTarget is Player))
            {
                SpectateTarget.MoveEntity(x, y);
            }
            else
            {
                base.Move(x, y);
            }

            if ((int)X != Sight.LastX || (int)Y != Sight.LastY) {
                if (IsNoClipping())
                    _client.Disconnect();
                Sight.UpdateCount++;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _clientEntities.Dispose();
        }

        // allow other admins to see hidden people
        public override bool CanBeSeenBy(Player player)
        {
            if (Client?.Account != null && Client.Account.Hidden)
            {
                return player.Admin != 0;
            }
            else
            {
                return true;
            }
        }

        public void SetDefaultSkin(int skin)
        {
            _originalSkin = skin;
            Skin = skin;
        }

        public void RestoreDefaultSkin()
        {
            Skin = _originalSkin;
        }
    }
}