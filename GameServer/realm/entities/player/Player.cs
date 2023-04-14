using System.Globalization;
using common;
using common.resources;
using GameServer.logic;
using GameServer.networking;
using GameServer.networking.packets;
using GameServer.networking.packets.outgoing;
using GameServer.realm.logic.accountMails;
using GameServer.realm.logic.quests;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;
using NLog;
using Newtonsoft.Json;
using wServer.realm;
using File = System.IO.File;

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

        private readonly SV<int> _experienceGoal;

        public int ExperienceGoal
        {
            get => _experienceGoal.GetValue();
            set => _experienceGoal.SetValue(value);
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

        private readonly SV<int> _fameGoal;

        public int FameGoal
        {
            get => _fameGoal.GetValue();
            set => _fameGoal.SetValue(value);
        }

        private readonly SV<int> _stars;

        public int Stars
        {
            get => _stars.GetValue();
            set => _stars.SetValue(value);
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
            set => _lightMax.SetValue(value);
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
        public Quests Quests { get; private set; }
        public AccountMails Mails { get; private set; }

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

        protected override void ImportStats(StatsType stats, object val)
        {
            var items = Manager.Resources.GameData.Items;
            base.ImportStats(stats, val);
            switch (stats)
            {
                case StatsType.AccountId: AccountId = ((string)val).ToInt32(); break;
                case StatsType.Experience: Experience = (int)val; break;
                case StatsType.ExperienceGoal: ExperienceGoal = (int)val; break;
                case StatsType.Level: Level = (int)val; break;
                case StatsType.Fame: Fame = (int)val; break;
                case StatsType.CurrentFame: CurrentFame = (int)val; break;
                case StatsType.FameGoal: FameGoal = (int)val; break;
                case StatsType.Stars: Stars = (int)val; break;
                case StatsType.Guild: Guild = (string)val; break;
                case StatsType.GuildRank: GuildRank = (int)val; break;
                case StatsType.Credits: Credits = (int)val; break;
                case StatsType.NameChosen: NameChosen = (int)val != 0; break;
                case StatsType.Texture1: Texture1 = (int)val; break;
                case StatsType.Texture2: Texture2 = (int)val; break;
                case StatsType.Skin: Skin = (int)val; break;
                case StatsType.Glow: Glow = (int)val; break;
                case StatsType.MP: MP = (int)val; break;
                case StatsType.Inventory: Inventory.GetItems(); break;
                case StatsType.MaximumHP: Stats.Base[0] = (int)val; break;
                case StatsType.MaximumMP: Stats.Base[1] = (int)val; break;
                case StatsType.Strength: Stats.Base[2] = (int)val; break;
                case StatsType.Armor: Stats.Base[3] = (int)val; break;
                case StatsType.Agility: Stats.Base[4] = (int)val; break;
                case StatsType.Dexterity: Stats.Base[5] = (int)val; break;
                case StatsType.Stamina: Stats.Base[6] = (int)val; break;
                case StatsType.Intelligence: Stats.Base[7] = (int)val; break;
                case StatsType.Luck: Stats.Base[10] = (int)val; break;
                case StatsType.Haste: Stats.Base[11] = (int)val; break;
                case StatsType.Shield: Stats.Base[12] = (int)val; break;
                case StatsType.Tenacity: Stats.Base[13] = (int)val; break;
                case StatsType.CriticalStrike: Stats.Base[14] = (int)val; break;
                case StatsType.LifeSteal: Stats.Base[15] = (int)val; break;
                case StatsType.ManaLeech: Stats.Base[16] = (int)val; break;
                case StatsType.LifeStealKill: Stats.Base[17] = (int)val; break;
                case StatsType.ManaLeechKill: Stats.Base[18] = (int)val; break;
                case StatsType.Resistance: Stats.Base[19] = (int)val; break;
                case StatsType.Wit: Stats.Base[20] = (int)val; break;
                case StatsType.Lethality: Stats.Base[21] = (int)val; break;
                case StatsType.Piercing: Stats.Base[22] = (int)val; break;
                case StatsType.ShieldPoints: Shield = (int)val; break;
                case StatsType.ShieldPointsMax: ShieldMax = (int)val; break;
                case StatsType.HealthStackCount: HealthPots.Count = (int)val; break; 
                case StatsType.MagicStackCount: MagicPots.Count = (int)val; break;
                case StatsType.HasBackpack: HasBackpack = (int)val == 1; break;
                case StatsType.XPBoostTime: XPBoostTime = (int)val * 1000; break;
                case StatsType.LDBoostTime: LDBoostTime = (int)val * 1000; break;
                case StatsType.LTBoostTime: LTBoostTime = (int)val * 1000; break;
                case StatsType.Rank: Rank = (int)val; break; 
                case StatsType.Admin: Admin = (int)val; break;
                case StatsType.UnholyEssence: UnholyEssence = (int)val; break;
                case StatsType.DivineEssence: DivineEssence = (int)val; break;
            }
        }

        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            base.ExportStats(stats);
            stats[StatsType.AccountId] = AccountId.ToString();
            stats[StatsType.Experience] = Experience - GetLevelExp(Level);
            stats[StatsType.ExperienceGoal] = ExperienceGoal;
            stats[StatsType.Level] = Level;
            stats[StatsType.CurrentFame] = CurrentFame;
            stats[StatsType.Fame] = Fame;
            stats[StatsType.FameGoal] = FameGoal;
            stats[StatsType.Stars] = Stars;
            stats[StatsType.Guild] = Guild;
            stats[StatsType.GuildRank] = GuildRank;
            stats[StatsType.Credits] = Credits;
            stats[StatsType.NameChosen] = // check from account in case ingame registration
                _client.Account?.NameChosen ?? NameChosen ? 1 : 0;
            stats[StatsType.Texture1] = Texture1;
            stats[StatsType.Texture2] = Texture2;
            stats[StatsType.Skin] = Skin;
            stats[StatsType.Glow] = Glow;
            stats[StatsType.MP] = MP;
            stats[StatsType.Inventory] = Inventory.GetItems();
            stats[StatsType.MaximumHP] = Stats[0];
            stats[StatsType.MaximumMP] = Stats[1];
            stats[StatsType.Strength] = Stats[2];
            stats[StatsType.Armor] = Stats[3];
            stats[StatsType.Agility] = Stats[4];
            stats[StatsType.Dexterity] = Stats[5];
            stats[StatsType.Stamina] = Stats[6];
            stats[StatsType.Intelligence] = Stats[7];
            stats[StatsType.Luck] = Stats[10];
            stats[StatsType.Haste] = Stats[11];
            stats[StatsType.Shield] = Stats[12];
            stats[StatsType.Tenacity] = Stats[13];
            stats[StatsType.CriticalStrike] = Stats[14];
            stats[StatsType.LifeSteal] = Stats[15];
            stats[StatsType.ManaLeech] = Stats[16];
            stats[StatsType.LifeStealKill] = Stats[17];
            stats[StatsType.ManaLeechKill] = Stats[18];
            stats[StatsType.Resistance] = Stats[19];
            stats[StatsType.Wit] = Stats[20];
            stats[StatsType.Lethality] = Stats[21];
            stats[StatsType.Piercing] = Stats[22];
            stats[StatsType.HPBoost] = Stats.Boost[0];
            stats[StatsType.MPBoost] = Stats.Boost[1];
            stats[StatsType.StrengthBonus] = Stats.Boost[2];
            stats[StatsType.ArmorBonus] = Stats.Boost[3];
            stats[StatsType.AgilityBonus] = Stats.Boost[4];
            stats[StatsType.DexterityBonus] = Stats.Boost[5];
            stats[StatsType.StaminaBonus] = Stats.Boost[6];
            stats[StatsType.IntelligenceBonus] = Stats.Boost[7];
            stats[StatsType.LuckBonus] = Stats.Boost[10];
            stats[StatsType.HasteBoost] = Stats.Boost[11];
            stats[StatsType.ShieldBonus] = Stats.Boost[12];
            stats[StatsType.TenacityBoost] = Stats.Boost[13];
            stats[StatsType.CriticalStrikeBoost] = Stats.Boost[14];
            stats[StatsType.LifeStealBoost] = Stats.Boost[15];
            stats[StatsType.ManaLeechBoost] = Stats.Boost[16];
            stats[StatsType.LifeStealKillBoost] = Stats.Boost[17];
            stats[StatsType.ManaLeechKillBoost] = Stats.Boost[18];
            stats[StatsType.ResistanceBoost] = Stats.Boost[19];
            stats[StatsType.WitBoost] = Stats.Boost[20];
            stats[StatsType.LethalityBoost] = Stats.Boost[21];
            stats[StatsType.PiercingBoost] = Stats.Boost[22];
            stats[StatsType.HealthStackCount] = HealthPots.Count;
            stats[StatsType.MagicStackCount] = MagicPots.Count;
            stats[StatsType.HasBackpack] = (HasBackpack) ? 1 : 0;
            stats[StatsType.XPBoost] = (XPBoostTime != 0) ? 1 : 0;
            stats[StatsType.XPBoostTime] = XPBoostTime / 1000;
            stats[StatsType.LDBoostTime] = LDBoostTime / 1000;
            stats[StatsType.LTBoostTime] = LTBoostTime / 1000;
            stats[StatsType.OxygenBar] = OxygenBar;
            stats[StatsType.Rank] = Rank;
            stats[StatsType.Admin] = Admin;
            stats[StatsType.UnholyEssence] = UnholyEssence;
            stats[StatsType.DivineEssence] = DivineEssence;
            stats[StatsType.ShieldPoints] = Shield;
            stats[StatsType.ShieldPointsMax] = ShieldMax;
            stats[StatsType.LightMax] = LightMax;
            stats[StatsType.Light] = Light;
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
            chr.PetData = PetData;
            chr.Items = Inventory.GetItems();
            chr.Light = Light;
            chr.AvailableQuests = AvailableQuests;
            chr.CharacterQuests = CharacterQuests;
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
            _experience = new SV<int>(this, StatsType.Experience, client.Character.Experience, true);
            _experienceGoal = new SV<int>(this, StatsType.ExperienceGoal, 0, true);
            _level = new SV<int>(this, StatsType.Level, client.Character.Level);
            _currentFame = new SV<int>(this, StatsType.CurrentFame, client.Account.Fame, true);
            _fame = new SV<int>(this, StatsType.Fame, client.Character.Fame, true);
            _fameGoal = new SV<int>(this, StatsType.FameGoal, 0, true);
            _stars = new SV<int>(this, StatsType.Stars, 0);
            _guild = new SV<string>(this, StatsType.Guild, "");
            _guildRank = new SV<int>(this, StatsType.GuildRank, -1);
            _credits = new SV<int>(this, StatsType.Credits, client.Account.Credits, true);
            _nameChosen = new SV<bool>(this, StatsType.NameChosen, client.Account.NameChosen, false,
                v => _client.Account?.NameChosen ?? v);
            _texture1 = new SV<int>(this, StatsType.Texture1, client.Character.Tex1);
            _texture2 = new SV<int>(this, StatsType.Texture2, client.Character.Tex2);
            _skin = new SV<int>(this, StatsType.Skin, 0);
            _glow = new SV<int>(this, StatsType.Glow, 0);
            _mp = new SV<int>(this, StatsType.MP, client.Character.MP);
            _hasBackpack = new SV<bool>(this, StatsType.HasBackpack, client.Character.HasBackpack, true);
            _xpBoosted = new SV<bool>(this, StatsType.XPBoost, client.Character.XPBoostTime != 0, true);
            _oxygenBar = new SV<int>(this, StatsType.OxygenBar, -1, true);
            _rank = new SV<int>(this, StatsType.Rank, client.Account.Rank);
            _admin = new SV<int>(this, StatsType.Admin, client.Account.Admin ? 1 : 0);
            _unholyEssence = new SV<int>(this, StatsType.UnholyEssence, client.Account.UnholyEssence, true);
            _divineEssence = new SV<int>(this, StatsType.DivineEssence, client.Account.DivineEssence, true);
            _shield = new SV<int>(this, StatsType.ShieldPoints, -1, true);
            _shieldMax = new SV<int>(this, StatsType.ShieldPointsMax, -1, true);
            _lightMax = new SV<int>(this, StatsType.LightMax, -1, true);
            _light = new SV<int>(this, StatsType.Light, client.Character.Light, true);

            AvailableQuests = client.Character.AvailableQuests ?? new QuestData[0];
            CharacterQuests = client.Character.CharacterQuests ?? new AcceptedQuestData[0];
            _offensiveAbility = new SV<int>(this, StatsType.OffensiveAbility, client.Character.OffensiveAbility, true);
            _defensiveAbility = new SV<int>(this, StatsType.DefensiveAbility, client.Character.DefensiveAbility, true);

            Name = client.Account.Name;
            HP = client.Character.HP;
            ConditionEffects = 0;

            XPBoostTime = client.Character.XPBoostTime;
            LDBoostTime = client.Character.LDBoostTime;
            LTBoostTime = client.Character.LTBoostTime;
            PetData = client.Character.PetData;

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
            Inventory = new Inventory(this, Utils.ResizeArray((DbLink as DbCharInv).Items, settings.InventorySize));

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

            Quests = new Quests(this);
            Quests.MakeQuestGiver(this);

            if (ObjectDesc.ObjectId == "Lightmage")
            {
                LightMax = 100;
            }

            Mails = new AccountMails(client);
            SetItemXpBoost();
        }

        byte[,] tiles;
        public FameCounter FameCounter { get; private set; }

        public Entity SpectateTarget { get; set; }
        public bool IsControlling => SpectateTarget != null && !(SpectateTarget is Player);

        public void ResetFocus(object target, EventArgs e)
        {
            var entity = target as Entity;
            entity.FocusLost -= ResetFocus;
            entity.Controller = null;

            if (Owner == null)
                return;

            SpectateTarget = null;
            Owner.Timers.Add(new WorldTimer(3000, (w, t) =>
                ApplyConditionEffect(ConditionEffectIndex.Paused, 0)));
            Client.SendPacket(new SetFocus()
            {
                ObjectId = Id
            });
        }

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
            FameGoal = GetFameGoal(FameCounter.ClassStats[ObjectType].BestFame);
            ExperienceGoal = GetExpGoal(_client.Character.Level);
            Stars = GetStars();

            if (owner.Name.Equals("OceanTrench"))
                OxygenBar = 100;

            SetNewbiePeriod();
            
            var changed = false;
            foreach (var i in _client.Account.ActiveGiftChests)
            {
                var giftChest = new DbGiftSingle(_client.Account, i);
                if (giftChest.Items.Length == 0)
                {
                    _client.Account.ActiveGiftChests.Remove(i);
                    Manager.Database.Conn.HashDeleteAsync("vault." + AccountId,
                        giftChest.Field);
                    changed = true;
                    continue;
                }

                if (changed) _client.Account.FlushAsync();
            }
            
            Client.SendPacket(new GlobalNotification
            {
                Text = Client.Account.ActiveGiftChests.Count > 0 ? "giftChestOccupied" : "giftChestEmpty"
            });
            
            if (owner.IsNotCombatMapArea)
                AddSafeAreaNotifications();

            Quests.MakeQuestGiver(this);

            // daily quest stuff
            /* var date = DateTime.UtcNow.ToShortDateString().Replace("/", "");
            var folder = $"{Manager.Config.serverSettings.resourceFolder}/data/quests/{date}";
            if (!Directory.Exists(folder))
                MakeDailyQuest(folder);

            var dq = new DailyQuest();
            var quest = dq.Load($"{folder}/quest.json");

            if (!Quests.HasAccountQuest(quest.Id) && !Client.Account.DailyQuestsCompleted.Contains(int.Parse(date)))
                Quests.AddAccountQuest(quest, true);*/

            if (Client.Account.AccountMails != null)
            {
                AddUpdateMails();
                if (Client.Account.AccountMails.Count > 0)
                {
                    var accMails = Client.Account.AccountMails
                        .Where(m => m.CharacterId == -1 || m.CharacterId == Client.Character.CharId);
                    Client.SendPacket(new GlobalNotification
                    {
                        Text = $"Mail ({accMails.Count()})"
                    });
                }
            }

            // spawn pet if player has one attached
            SpawnPet(owner);

            base.Init(owner);
        }

        public void SpawnPet(World owner)
        {
            if (PetData == null) return;
            if (PlayerPet != null)
            {
                PlayerPet.Owner.LeaveWorld(PlayerPet);
                PlayerPet.Dispose();
                PlayerPet = null;
                return;
            }

            var objType = PetData.ObjectType;
            if (!Manager.Resources.GameData.ObjectDescs.TryGetValue(objType, out var objDesc) || !objDesc.IsPet) return;

            PlayerPet = Resolve(owner.Manager, objType);
            PlayerPet.Move(X, Y);
            Owner.EnterWorld(PlayerPet);
            PlayerPet.PetData = PetData;
            PlayerPet.SetPlayerOwner(this);
        }

        private void AddSafeAreaNotifications()
        {
        }

        private void AddUpdateMails()
        {
            var updates = Manager.Resources.UpdateList;
            for (var i = 0; i < updates.Count; i++)
            {
                var addTime = DateTime.ParseExact(updates[i].AddTime, "MM/dd/yyyy HH:mm:ss 'GMT'K",
                    CultureInfo.InvariantCulture).ToUnixTimestamp();
                var endTime = DateTime.ParseExact(updates[i].EndTime, "MM/dd/yyyy HH:mm:ss 'GMT'K",
                    CultureInfo.InvariantCulture).ToUnixTimestamp();
                if (!Mails.HasMail(updates[i].Id) &&
                    addTime < DateTime.UtcNow.ToUnixTimestamp() &&
                    endTime > DateTime.UtcNow.ToUnixTimestamp())
                {
                    var mail = new AccountMail
                    {
                        Id = updates[i].Id,
                        CharacterId = -1,
                        AddTime = addTime,
                        EndTime = endTime,
                        Content = updates[i].Content
                    };
                    Mails.Add(mail);
                }
            }
        }

        private void MakeDailyQuest(string folder)
        {
            Directory.CreateDirectory(folder);

            var rnd = MathUtils.Next(2, 7);
            var rndLoots = Manager.Resources.GameData.Items.ToList().Where(pair =>
                pair.Value.Tier < 8 && pair.Value.Tier > 4 ||
                QuestGiverContent.TierFourLoot.Contains(pair.Value.ObjectId)).ToList();

            var loots = new List<string>();
            for (var i = 0; i < rnd; i++)
            {
                loots.Add(rndLoots.PickRandom().Value.ObjectId);
            }

            File.WriteAllText($"{folder}/quest.json",
                JsonConvert.SerializeObject(Quests.GenerateQuest(-1, 100, 8, items: loots.ToArray(), hours: 24),
                    Utils.SerializerSettings()));
        }

        public override void Tick(RealmTime time)
        {
            if (!KeepAlive(time) || Client.State == ProtocolState.Reconnecting)
                return;

            CheckTradeTimeout(time);
            HandleQuest(time);

            if (!HasConditionEffect(ConditionEffects.Paused))
            {
                HandleRegen(time);
                HandleEffects(time);
                //HandleOceanTrenchGround(time);
                TickActivateEffects(time);
                FameCounter.Tick(time);
                Quests.Tick(time);
                Mails.Tick(time);
                // if (AvailableQuests.Length + CharacterQuests.Length < 3)
                    // Quests.QuestGiverTick(time);

                TickPassiveEffects();
            }

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
            if (HasConditionEffect(ConditionEffects.Suppressed))
                return;

            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i].Item == null)
                    continue;
                switch (Inventory[i].Item.Power)
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
                if (Inventory[i].Item == null)
                    continue;

                sum += Inventory[i].Item.XpBonus;
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
                ApplyConditionEffect(ConditionEffectIndex.Renewed, 2500);
                ApplyConditionEffect(ConditionEffectIndex.Paralyzed, 500);
            }

            HandleQuest(time, true, position);

            var id = IsControlling ? SpectateTarget.Id : Id;
            var tpPkts = new Packet[]
            {
                new Goto()
                {
                    ObjectId = id,
                    Pos = position
                },
                new ShowEffect()
                {
                    EffectType = EffectType.Teleport,
                    TargetObjectId = id,
                    Pos1 = position,
                    Color = new ARGB(0xFFFFFFFF)
                }
            };
            foreach (var plr in Owner.Players.Values)
            {
                plr.AwaitGotoAck(time.TotalElapsedMs);
                plr.Client.SendPackets(tpPkts);
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

                if (HasConditionEffect(ConditionEffects.Paused))
                {
                    SendError("Cannot teleport while paused.");
                    return;
                }

                if (obj is not Player)
                {
                    SendError("Can only teleport to players.");
                    return;
                }

                if (obj.HasConditionEffect(ConditionEffects.Invisible))
                {
                    SendError("Cannot teleport to an invisible player.");
                    return;
                }

                if (obj.HasConditionEffect(ConditionEffects.Paused))
                {
                    SendError("Cannot teleport to a paused player.");
                    return;
                }
            }

            TeleportPosition(time, obj.X, obj.Y, ignoreRestrictions);
        }

        public bool IsInvulnerable()
        {
            if (HasConditionEffect(ConditionEffects.Paused) ||
                HasConditionEffect(ConditionEffects.Stasis) ||
                HasConditionEffect(ConditionEffects.Invincible) ||
                HasConditionEffect(ConditionEffects.Invulnerable))
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

            Owner.BroadcastPacketNearby(new Damage()
            {
                TargetId = this.Id,
                Effects = HasConditionEffect(ConditionEffects.Invincible) ? 0 : projectile.ConditionEffects,
                DamageAmount = dmgAmount,
                Kill = HP <= 0,
                BulletId = projectile.BulletId,
                ObjectId = projectile.ProjectileOwner.Self.Id
            }, this, this);

            if (HP <= 0)
                Death(projectile.ProjectileOwner.Self.ObjectDesc.DisplayId ??
                      projectile.ProjectileOwner.Self.ObjectDesc.ObjectId,
                    projectile.ProjectileOwner.Self);

            return base.HitByProjectile(projectile, time);
        }

        private void HandleEffectsOnHit(Projectile projectile, RealmTime time)
        {
            if (HasConditionEffect(ConditionEffects.Suppressed))
                return;
            var pos = new Position() { X = X, Y = Y };
            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i].Item == null)
                    continue;
                switch (Inventory[i].Item.Power)
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

            Owner.BroadcastPacketNearby(new Damage()
            {
                TargetId = Id,
                Effects = 0,
                DamageAmount = (ushort)dmg,
                Kill = HP <= 0,
                BulletId = 0,
                ObjectId = src.Id
            }, this, this);

            if (HP <= 0)
                Death(src.ObjectDesc.DisplayId ??
                      src.ObjectDesc.ObjectId,
                    src);
        }

        private void HandleEffectsOnDamage()
        {
            if (HasConditionEffect(ConditionEffects.Suppressed))
                return;
            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i].Item == null)
                    continue;
                switch (Inventory[i].Item.Power)
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
            _client.Reconnect(new Reconnect()
            {
                Host = "",
                Port = 2050,
                GameId = World.Realm,
                Name = "Nexus"
            });
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
            if (_client.State == ProtocolState.Disconnected || Client.State == ProtocolState.Reconnecting || _dead)
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

            _client.SendPacket(new Death()
            {
                AccountId = AccountId.ToString(),
                CharId = _client.Character.CharId,
                KilledBy = killer,
                ZombieId = -1
            });

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

            Client.Reconnect(new Reconnect()
            {
                Host = "",
                Port = 2050,
                GameId = world.Id,
                Name = world.Name
            });
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

                Client.Reconnect(new Reconnect()
                {
                    Host = "",
                    Port = 2050,
                    GameId = world.Id,
                    Name = world.Name
                });
            }
        }
        
        private void ShowNotification(string notif, uint color = 0xFF00FF00)
        {
            if (Owner is null)
            {
                return;
            }

            Owner.BroadcastPacket(new Notification
            {
                Color = new ARGB(color),
                ObjectId = IsControlling ? SpectateTarget.Id : Id,
                Message = "fn>" + notif
            }, null);
        }

        public int GetCurrency(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Gold:
                    return Credits;
                case CurrencyType.Fame:
                    return CurrentFame;
                case CurrencyType.UnholyEssence:
                    return UnholyEssence;
                case CurrencyType.DivineEssence:
                    return DivineEssence;
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
                case CurrencyType.UnholyEssence:
                    UnholyEssence = amount;
                    break;
                case CurrencyType.DivineEssence:
                    DivineEssence = amount;
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

            if ((int)X != Sight.LastX || (int)Y != Sight.LastY)
            {
                if (IsNoClipping())
                {
                    _client.Manager.Logic.AddPendingAction(t => _client.Disconnect());
                }

                Sight.UpdateCount++;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (SpectateTarget != null)
            {
                SpectateTarget.FocusLost -= ResetFocus;
                SpectateTarget.Controller = null;
            }

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

        public void DropNextRandom(int times = 1)
        {
            for (var i = 0; i < times; i++)
                Client.ClientRandom.NextInt();
        }
    }
}