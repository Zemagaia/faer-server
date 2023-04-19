using System.Collections.Concurrent;
using Shared;
using Shared.resources;
using Shared.terrain;
using DungeonGenerator;
using DungeonGenerator.Templates;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.entities.vendors;
using GameServer.realm.worlds.logic;
using NLog;

namespace GameServer.realm.worlds
{
    public class World
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        protected static readonly Random Rand = new((int)DateTime.Now.Ticks);

        public const int Tutorial = -1;
        public const int Realm = -2;
        public const int Vault = -3;
        public const int Tinker = -4;
        public const int GuildHall = -5;
        public const int ClothBazaar = -6;
        public const int PetYard = -7;
        public const int MarketPlace = -8;
		public const int Test = -9;

        private RealmManager _manager;

        public RealmManager Manager
        {
            get => _manager;
            internal set
            {
                _manager = value;
                if (_manager != null)
                    Init();
            }
        }

        public int Id { get; internal set; }
        public string Name { get; set; }
        public string SBName { get; set; }
        public int Difficulty { get; protected set; }
        public int Background { get; protected set; }
        public bool IsLimbo { get; protected set; }
        public bool AllowTeleport { get; protected set; }
        public bool ShowDisplays { get; protected set; }
        public bool Persist { get; protected set; }
        public int Blocking { get; protected set; }
        public string Music { get; set; }
        public bool PlayerDungeon { get; set; }
        public string Opener { get; set; }
        public HashSet<string> Invites { get; set; }
        public Dictionary<string, Player> InviteDict { get; set; }

        public Wmap Map { get; private set; }
        public bool Deleted { get; protected set; }

        private long _elapsedTime;
        private int _totalConnects;

        public int TotalConnects => _totalConnects;

        private KeyValuePair<IntPoint, TileRegion>[] _spawnPoints;

        public bool Closed { get; set; }

        public ConcurrentDictionary<int, Player> Players { get; private set; }
        public ConcurrentDictionary<int, Enemy> Enemies { get; private set; }
        public ConcurrentDictionary<int, Enemy> Quests { get; private set; }
        public ConcurrentDictionary<Tuple<int, byte>, Projectile> Projectiles { get; private set; }
        public ConcurrentDictionary<int, StaticObject> StaticObjects { get; private set; }
        public ConcurrentDictionary<int, Enemy> Pets { get; private set; }

        public CollisionMap<Entity> EnemiesCollision { get; private set; }
        public CollisionMap<Entity> PlayersCollision { get; private set; }

        public List<WorldTimer> Timers { get; private set; }

        private static int _entityInc;
        public Entity QuestTracker;

        private readonly object _deleteLock = new();
        public readonly ProtoWorld Proto;

        public World(ProtoWorld proto)
        {
            Proto = proto;
            Setup();
            Id = proto.id;
            Name = proto.name;
            SBName = proto.sbName;
            Difficulty = proto.difficulty;
            Background = proto.background;
            IsLimbo = proto.isLimbo;
            Persist = proto.persist;
            AllowTeleport = !proto.restrictTp;
            ShowDisplays = proto.showDisplays;
            Blocking = proto.blocking;
            Opener = "";

            var rnd = new Random();
            if (proto.music != null)
                Music = proto.music[rnd.Next(0, proto.music.Length)];
            else
                Music = "Test";
        }

        private void Setup()
        {
            Players = new ConcurrentDictionary<int, Player>();
            Enemies = new ConcurrentDictionary<int, Enemy>();
            Quests = new ConcurrentDictionary<int, Enemy>();
            Projectiles = new ConcurrentDictionary<Tuple<int, byte>, Projectile>();
            StaticObjects = new ConcurrentDictionary<int, StaticObject>();
            Pets = new ConcurrentDictionary<int, Enemy>();
            Timers = new List<WorldTimer>();
            AllowTeleport = true;
            ShowDisplays = true;
            Persist = false; // if false, attempts to delete world with 0 players
            Blocking = 0; // toggles sight block (0 disables sight block)
        }

        public string GetDisplayName()
        {
            if (SBName != null && SBName.Length > 0)
            {
                return SBName[0] == '{' ? Name : SBName;
            }
            else
            {
                return Name;
            }
        }

        public bool IsNotCombatMapArea => Id == Vault || Id == GuildHall || Id == ClothBazaar || Id == Tinker ||
                                          Id == MarketPlace || Id == PetYard;

        public virtual bool AllowedAccess(Client client)
        {
            return !Closed || client.Account.Admin;
        }

        public virtual KeyValuePair<IntPoint, TileRegion>[] GetSpawnPoints()
        {
            if (_spawnPoints is null)
            {
                _spawnPoints = Map.Regions.Where(t => t.Value == TileRegion.Spawn).ToArray();
            }

            return _spawnPoints;
        }

        public virtual World GetInstance(Client client)
        {
            World world;
            DynamicWorld.TryGetWorld(_manager.Resources.Worlds[Name], client, out world);

            if (world == null)
                world = new World(_manager.Resources.Worlds[Name]);

            world.IsLimbo = false;
            return Manager.AddWorld(world);
        }

        public long GetAge()
        {
            return _elapsedTime;
        }

        protected virtual void Init()
        {
            if (IsLimbo) return;

            var proto = Manager.Resources.Worlds[Name];

            if (proto.maps != null && proto.maps.Length <= 0)
            {
                var template = DungeonTemplates.GetTemplate(Name);
                if (template == null)
                    throw new KeyNotFoundException($"Template for {Name} not found.");
                FromDungeonGen(Rand.Next(), template);
                return;
            }

            var map = Rand.Next(0, (proto.maps == null) ? 1 : proto.maps.Length);
            FromWorldMap(new MemoryStream(proto.wmap[map]));

            InitShops();
        }

        protected void InitShops()
        {
            foreach (var shop in Manager.Resources.GameData.MerchantLists)
            {
                var items = new List<ISellableItem>(shop.Items);
                var mLocations = Map.Regions
                    .Where(r => shop.Region == r.Value)
                    .Select(r => r.Key)
                    .ToArray();

                if (items.Count <= 0 || items.All(i => i.ItemId == ushort.MaxValue))
                    continue;

                var rotate = items.Count > mLocations.Length;
                var reloadOffset = 0;
                foreach (var loc in mLocations)
                {
                    var item = items[0];
                    items.RemoveAt(0);
                    while (item.ItemId == ushort.MaxValue)
                    {
                        if (items.Count <= 0)
                            items.AddRange(shop.Items);

                        item = items[0];
                        items.RemoveAt(0);
                    }

                    reloadOffset += 500;
                    var m = new WorldMerchant(Manager, 0x0400)
                    {
                        ShopItem = item,
                        Item = item.ItemId,
                        Price = item.Price,
                        Count = item.Count,
                        Currency = shop.Currency,
                        ItemList = shop.Items,
                        ReloadOffset = reloadOffset,
                        Rotate = rotate
                    };

                    m.Move(loc.X + .5f, loc.Y + .5f);
                    EnterWorld(m);

                    if (items.Count <= 0)
                        items.AddRange(shop.Items);
                }
            }
        }

        protected void LoadMap(string embeddedResource)
        {
            if (embeddedResource == null)
                return;
            var stream = typeof(RealmManager).Assembly.GetManifestResourceStream(embeddedResource);
            if (stream == null)
                throw new ArgumentException("Resource not found", nameof(embeddedResource));

            FromWorldMap(stream);
        }

        public bool Delete()
        {
            lock (_deleteLock)
            {
                if (Players.Count > 0)
                    return false;

                Deleted = true;
                Manager.RemoveWorld(this);
                Id = 0;

                DisposeEntities(Players);
                DisposeEntities(Enemies);
                DisposeEntities(Projectiles);
                DisposeEntities(StaticObjects);
                DisposeEntities(Pets);

                Players = null;
                Enemies = null;
                Projectiles = null;
                StaticObjects = null;
                Pets = null;
                _spawnPoints = null;

                return true;
            }
        }

        private void DisposeEntities<T, TU>(ConcurrentDictionary<T, TU> dictionary)
        {
            foreach (var entity in dictionary)
                (entity.Value as Entity).Dispose();
        }

        protected void FromDungeonGen(int seed, DungeonTemplate template)
        {
            Log.Info("Loading template for world {0}({1})...", Id, Name);

            var gen = new Generator(seed, template);
            gen.Generate();
            var ras = new Rasterizer(seed, gen.ExportGraph());
            ras.Rasterize();
            var dTiles = ras.ExportMap();

            if (Map == null)
            {
                Map = new Wmap(Manager.Resources.GameData);
                Interlocked.Add(ref _entityInc, Map.Load(dTiles, _entityInc));
                if (Blocking == 3)
                    Sight.CalcRegionBlocks(Map);
            }
            else
                Map.ResetTiles();

            InitMap();
        }

        protected void FromWorldMap(Stream dat)
        {
            Log.Info("Loading map for world {0}({1})...", Id, Name);

            if (Map == null)
            {
                Map = new Wmap(Manager.Resources.GameData);
                Interlocked.Add(ref _entityInc, Map.Load(dat, _entityInc));
                if (Blocking == 3)
                    Sight.CalcRegionBlocks(Map);
            }
            else
                Map.ResetTiles();

            InitMap();
        }

        private void InitMap()
        {
            int w = Map.Width, h = Map.Height;
            EnemiesCollision = new CollisionMap<Entity>(0, w, h);
            PlayersCollision = new CollisionMap<Entity>(1, w, h);

            Projectiles.Clear();
            StaticObjects.Clear();
            Enemies.Clear();
            Players.Clear();
            Quests.Clear();
            Timers.Clear();
            Pets.Clear();

            foreach (var i in Map.InstantiateEntities(Manager))
                EnterWorld(i);
        }

        public virtual int EnterWorld(Entity entity)
        {
            if (entity is Player p)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                Players.TryAdd(entity.Id, p);
                PlayersCollision.Insert(entity);
                Interlocked.Increment(ref _totalConnects);
                return entity.Id;
            }

            // for regular enemies
            if (entity is Enemy e)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                Enemies.TryAdd(entity.Id, e);
                EnemiesCollision.Insert(entity);
                if (entity.ObjectDesc.Quest)
                    Quests.TryAdd(entity.Id, e);
                return entity.Id;
            }

            // for pets
            if (entity is Enemy pet)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                Pets.TryAdd(entity.Id, pet);
                PlayersCollision.Insert(entity);
                return entity.Id;
            }

            if (entity is Projectile)
            {
                entity.Init(this);
                var prj = entity as Projectile;
                Projectiles[new Tuple<int, byte>(prj.ProjectileOwner.Self.Id, prj.BulletId)] = prj;
                return entity.Id;
            }

            if (entity is StaticObject)
            {
                entity.Id = GetNextEntityId();
                entity.Init(this);
                StaticObjects.TryAdd(entity.Id, entity as StaticObject);
                EnemiesCollision.Insert(entity);
                return entity.Id;
            }

            return entity.Id;
        }

        public virtual void LeaveWorld(Entity entity)
        {
            if (entity is Player)
            {
                Players.TryRemove(entity.Id, out var player);
                PlayersCollision.Remove(entity);

                // if in trade, cancel it...
                if (player?.tradeTarget != null)
                    player.CancelTrade();
                
                player.CollisionNode = null;
                player.Parent = null;
            }
            // for enemies
            else if (entity is Enemy e)
            {
                Enemy dummy;
                Enemies.TryRemove(entity.Id, out dummy);
                EnemiesCollision.Remove(entity);
                if (entity.ObjectDesc.Quest)
                    Quests.TryRemove(entity.Id, out dummy);
            }
            else if (entity is Enemy)
            {
                Enemy dummy;
                Pets.TryRemove(entity.Id, out dummy);
                PlayersCollision.Remove(entity);
            }
            else if (entity is Projectile)
            {
                var p = entity as Projectile;
                Projectiles.TryRemove(new Tuple<int, byte>(p.ProjectileOwner.Self.Id, p.BulletId), out p);
            }
            else if (entity is StaticObject)
            {
                StaticObject dummy;
                StaticObjects.TryRemove(entity.Id, out dummy);

                if (entity.ObjectDesc?.BlocksSight == true)
                {
                    if (Blocking == 3)
                        Sight.UpdateRegion(Map, (int)entity.X, (int)entity.Y);

                    foreach (var plr in Players
                        .Where(p => MathsUtils.DistSqr(p.Value.X, p.Value.Y, entity.X, entity.Y) < Player.RadiusSqr))
                        plr.Value.Sight.UpdateCount++;
                }

                EnemiesCollision.Remove(entity);
            }

            entity.Dispose();
        }

        public int GetNextEntityId()
        {
            return Interlocked.Increment(ref _entityInc);
        }

        public Entity GetEntity(int id)
        {
            Player ret1;
            if (Players.TryGetValue(id, out ret1)) return ret1;
            Enemy ret2;
            if (Enemies.TryGetValue(id, out ret2)) return ret2;
            StaticObject ret3;
            if (StaticObjects.TryGetValue(id, out ret3)) return ret3;
            return null;
        }

        public Player GetUniqueNamedPlayer(string name)
        {
            foreach (var i in Players)
            {
                if (i.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!i.Value.NameChosen && !(this is Test))
                        Manager.Database.ReloadAccount(i.Value.Client.Account);

                    return i.Value;
                }
            }

            return null;
        }

        public bool IsPassable(double x, double y, bool spawning = false)
        {
            var x_ = (int)x;
            var y_ = (int)y;

            if (!Map.Contains(x_, y_))
                return false;

            var tile = Map[x_, y_];

            if (tile.TileDesc.NoWalk)
                return false;

            if (tile.ObjType != 0 && tile.ObjDesc != null)
            {
                if (tile.ObjDesc.FullOccupy || tile.ObjDesc.EnemyOccupySquare ||
                    (spawning && tile.ObjDesc.OccupySquare))
                    return false;
            }

            return true;
        }

        public void QuakeToWorld(World newWorld)
        {
            if (!Persist || this is Realm)
                Closed = true;

            foreach (var p in Players)
                p.Value.Client.SendShowEffect(EffectType.Earthquake, 0, 0, 0, 0, 0, 0);

            Timers.Add(new WorldTimer(8000, (w, t) =>
            {
                foreach (var plr in w.Players.Values)
                    plr.Client.Reconnect(newWorld.SBName, newWorld.Id);
            }));

            if (!Persist)
                Timers.Add(new WorldTimer(20000, (w2, t2) =>
                {
                    // to ensure people get kicked out of world
                    foreach (var plr in w2.Players)
                        plr.Value.Client.Disconnect();
                }));
        }

        public void ChatReceived(Player player, string text)
        {
            foreach (var en in Enemies)
                en.Value.OnChatTextReceived(player, text);
            foreach (var en in StaticObjects)
                en.Value.OnChatTextReceived(player, text);
        }

        public virtual void Tick(RealmTime time)
        {
            // if Tick is overrided and you make a call to this function
            // make sure not to do anything after the call (or at least check)
            // as it is possible for the world to have been removed at that point.

            try
            {
                _elapsedTime += time.ElapsedMsDelta;

                if (IsLimbo) return;

                if (!Persist && (this is Vault ? _elapsedTime > 5000 : _elapsedTime > 60000) && Players.Count <= 0)
                {
                    Delete();
                    return;
                }

                lock (_deleteLock)
                {
                    if (Deleted)
                        return;

                    if (EnemiesCollision != null)
                    {
                        foreach (var i in EnemiesCollision.GetActiveChunks(PlayersCollision))
                            i.Tick(time);
                    }
                    else
                    {
                        foreach (var i in Enemies)
                            i.Value.Tick(time);
                    
                        foreach (var i in StaticObjects)
                            i.Value.Tick(time);
                    }
                
                    foreach (var i in Players)
                        i.Value.Tick(time);

                    foreach (var i in Projectiles)
                        i.Value.Tick(time);
                }
            
                for (var i = Timers.Count - 1; i >= 0; i--)
                    try
                    {
                        if (Timers[i].Tick(this, time))
                            Timers.RemoveAt(i);
                    }
                    catch (Exception e)
                    {
                        var msg = e.Message + "\n" + e.StackTrace;
                        Log.Error(msg);
                        Timers.RemoveAt(i);
                    }
            }
            catch (Exception e)
            {
                var msg = e.Message + "\n" + e.StackTrace;
                Log.Error(msg);
            }
        }

        public Projectile GetProjectile(int objectId, int bulletId)
        {
            var entity = GetEntity(objectId) as IProjectileOwner;
            return entity != null
                ? entity.Projectiles[bulletId]
                : Projectiles.SingleOrDefault(p =>
                    p.Value.ProjectileOwner.Self.Id == objectId &&
                    p.Value.BulletId == bulletId).Value;
        }
    }
}