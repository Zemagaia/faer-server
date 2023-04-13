using common.resources;
using common.terrain;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.setpieces;
using GameServer.realm.worlds;
using NLog;
using Castle = GameServer.realm.worlds.logic.Castle;

namespace GameServer.realm
{
    // World overseer (spawning, setpieces, events, taunts, etc)
    public class Overseer
    {
        public bool Closing;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly XmlData _gameData;
        private readonly World _world;
        private readonly Random _rand = new();
        private readonly int[] _enemyMaxCounts = new int[15];
        private readonly int[] _enemyCounts = new int[15];
        private long _prevTick;
        private int _tenSecondTick;
        private RealmTime _dummyTime = new();

        private readonly EventData[] _eventData;
        private readonly TauntData[] _tauntData;
        private readonly SpawnData[] _spawnData;

        public Overseer(World world, string worldName)
        {
            _world = world;
            _gameData = world.Manager.Resources.GameData;
            world.Manager.Resources.Worlds.BiomeData.TryGetValue(worldName, out var data);
            _tauntData = data?.TauntData;
            _spawnData = data?.SpawnData;
            _eventData = data?.EventData;
        }

        private static double GetUniform(Random rand)
        {
            // 0 <= u < 2^32
            var u = (uint)(rand.NextDouble() * uint.MaxValue);
            // The magic number below is 1/(2^32 + 2).
            // The result is strictly between 0 and 1.
            return (u + 1.0) * 2.328306435454494e-10;
        }

        private static double GetNormal(Random rand)
        {
            // Use Box-Muller algorithm
            var u1 = GetUniform(rand);
            var u2 = GetUniform(rand);
            var r = Math.Sqrt(-2.0 * Math.Log(u1));
            var theta = 2.0 * Math.PI * u2;
            return r * Math.Sin(theta);
        }

        private static double GetNormal(Random rand, double mean, double standardDeviation)
        {
            return mean + standardDeviation * GetNormal(rand);
        }

        private ushort GetRandomObjType(List<SpawnInfo> spawns)
        {
            var p = _rand.NextDouble();
            ushort objType = 0;
            foreach (var spawn in spawns)
            {
                if (spawn.Chance > p)
                {
                    objType = _gameData.IdToObjectType[spawn.Name];
                    break;
                }
            }

            return objType;
        }

        private int Spawn(ObjectDesc desc, TileRegion region, int w, int h)
        {
            Entity entity;

            var ret = 0;
            var pt = new IntPoint();

            if (desc.Spawn != null)
            {
                var num = (int)GetNormal(_rand, desc.Spawn.Mean, desc.Spawn.StdDev);

                if (num > desc.Spawn.Max)
                    num = desc.Spawn.Max;
                else if (num < desc.Spawn.Min)
                    num = desc.Spawn.Min;

                do
                {
                    pt.X = _rand.Next(0, w);
                    pt.Y = _rand.Next(0, h);
                } while (_world.Map[pt.X, pt.Y].Region != region ||
                         !_world.IsPassable(pt.X, pt.Y) ||
                         _world.AnyPlayerNearby(pt.X, pt.Y));

                for (var k = 0; k < num; k++)
                {
                    entity = Entity.Resolve(_world.Manager, desc.ObjectType);
                    entity.Move(
                        pt.X + (float)(_rand.NextDouble() * 2 - 1) * 5,
                        pt.Y + (float)(_rand.NextDouble() * 2 - 1) * 5);
                    (entity as Enemy).Region = region;
                    _world.EnterWorld(entity);
                    ret++;
                }

                return ret;
            }

            do
            {
                pt.X = _rand.Next(0, w);
                pt.Y = _rand.Next(0, h);
            } while (_world.Map[pt.X, pt.Y].Region != region ||
                     !_world.IsPassable(pt.X, pt.Y) ||
                     _world.AnyPlayerNearby(pt.X, pt.Y));

            entity = Entity.Resolve(_world.Manager, desc.ObjectType);
            entity.Move(pt.X, pt.Y);
            (entity as Enemy).Region = region;
            _world.EnterWorld(entity);
            ret++;
            return ret;
        }

        public void Tick(RealmTime time)
        {
            if (time.TotalElapsedMs - _prevTick <= 10000)
                return;

            if (_tenSecondTick % 2 == 0 && _tauntData != null)
                HandleAnnouncements();

            if (_tenSecondTick % 6 == 0 && _spawnData != null)
                EnsurePopulation();

            _tenSecondTick++;
            _prevTick = time.TotalElapsedMs;
        }

        private void EnsurePopulation()
        {
            // Log.Info($"Overseer is controlling population at {_world.SBName + $"({_world.Id})"}...");

            RecalculateEnemyCount();

            var state = new int[15];
            var diff = new int[15];
            var c = 0;

            for (var i = 0; i < state.Length; i++)
            {
                if (_enemyCounts[i] > _enemyMaxCounts[i] * 1.5) //Kill some
                {
                    state[i] = 1;
                    diff[i] = _enemyCounts[i] - _enemyMaxCounts[i];
                    c++;
                    continue;
                }

                if (_enemyCounts[i] < _enemyMaxCounts[i] * 0.75) //Add some
                {
                    state[i] = 2;
                    diff[i] = _enemyMaxCounts[i] - _enemyCounts[i];
                    continue;
                }

                state[i] = 0;
            }

            foreach (var i in _world.Enemies) //Kill
            {
                var idx = (int)i.Value.Region - 26;

                if (idx <= -1 || state[idx] == 0 ||
                    i.Value.GetNearestEntity(10, true) != null ||
                    diff[idx] == 0)
                    continue;

                if (state[idx] == 1)
                {
                    _world.LeaveWorld(i.Value);
                    diff[idx]--;
                    if (diff[idx] == 0)
                        c--;
                }

                if (c == 0)
                    break;
            }

            var w = _world.Map.Width;
            var h = _world.Map.Height;

            for (var i = 0; i < state.Length; i++) //Add
            {
                if (state[i] != 2)
                    continue;

                var x = diff[i];
                var t = (TileRegion)(i + 26);
                for (var j = 0; j < x;)
                {
                    var sd = _spawnData.FirstOrDefault(k => k.Region == t);
                    if (sd == null) continue;
                    var objType = GetRandomObjType(sd.Spawns);

                    if (objType == 0)
                        continue;

                    j += Spawn(_gameData.ObjectDescs[objType], t, w, h);
                }
            }

            RecalculateEnemyCount();

            // Log.Info($"Overseer is back to sleep at {_world.SBName + $"({_world.Id})"}.");
        }

        private void RecalculateEnemyCount()
        {
            for (var i = 0; i < _enemyCounts.Length; i++)
                _enemyCounts[i] = 0;

            foreach (var i in _world.Enemies)
            {
                if (i.Value.Region == TileRegion.None ||
                    i.Value.Region == TileRegion.Spawn)
                    continue;

                _enemyCounts[(int)i.Value.Region - 26]++;
            }
        }

        private void HandleAnnouncements()
        {
            if (_world.Closed)
                return;

            var taunt = _tauntData[_rand.Next(0, _tauntData.Length)];
            var count = 0;
            foreach (var i in _world.Enemies)
            {
                var desc = i.Value.ObjectDesc;
                if (desc == null || desc.ObjectId != taunt.Name)
                    continue;
                count++;
            }

            if (count == 0)
                return;

            if (count == 1 && taunt.Final != null ||
                taunt.Final != null && taunt.EnemyCount == null)
            {
                var arr = taunt.Final;
                var msg = arr[_rand.Next(0, arr.Length)];
                BroadcastMsg(msg);
            }
            else
            {
                var arr = taunt.EnemyCount;
                if (arr == null)
                    return;

                var msg = arr[_rand.Next(0, arr.Length)];
                msg = msg.Replace("{COUNT}", count.ToString());
                BroadcastMsg(msg);
            }
        }

        private void BroadcastMsg(string message)
        {
            _world.Manager.Chat.Oryx(_world, message);
        }

        private void BroadcastEnemyMsg(string name, string message)
        {
            _world.Manager.Chat.Enemy(_world, name, message);
        }

        public void OnPlayerEntered(Player player)
        {
            player.SendInfo("Welcome to Realm of the Mad God");
            player.SendEnemy("Oryx the Mad God", "You are food for my minions!");
            player.SendInfo("Use [WASDQE] to move; click to shoot!");
            player.SendInfo("Type \"/help\" for more help");
        }

        private void SpawnEvent(string name, string setpiece)
        {
            var pt = new IntPoint();
            var world = _world;
            do
            {
                pt.X = _rand.Next(0, world.Map.Width);
                pt.Y = _rand.Next(0, world.Map.Height);
            } while (!world.IsPassable(pt.X, pt.Y, true) ||
                     world.AnyPlayerNearby(pt.X, pt.Y));

            var sp = world.Manager.Resources.Worlds[setpiece];
            var wmap = SetPieces.GetWmap(world.Manager, sp);
            var size = wmap.Height > wmap.Width ? wmap.Height : wmap.Width;
            pt.X -= (size - 1) / 2;
            pt.Y -= (size - 1) / 2;
            SetPieces.RenderFromProto(world, pt, sp);
            Log.Info("{0}: {1} has been spawned at ({2}, {3})", _world.GetDisplayName(), name, pt.X, pt.Y);
        }

        public void OnEnemyKilled(Enemy enemy, Player killer)
        {
            // enemy is quest?
            if (enemy.ObjectDesc == null || !enemy.ObjectDesc.Quest)
                return;

            // is a critical quest?
            TauntData dat = null;
            foreach (var i in _tauntData)
                if (enemy.ObjectDesc.ObjectId == i.Name)
                {
                    dat = i;
                    break;
                }

            if (dat == null)
                return;

            foreach (var i in _tauntData)
            {
                // announce locally & globally
                if (enemy.ObjectDesc.ObjectId != i.Name)
                {
                    continue;
                }

                foreach (var w in enemy.Manager.Worlds.Values)
                foreach (var p in w.Players.Values)
                    p.SendEnemy($"{_world.SBName}", $"{i.Name} has been defeated!", 0xDD9090);
            }

            var events = _eventData.ToList();
            // if (_rand.NextDouble() <= 0.05)
            //     events = _rareEvents;

            var evt = events[_rand.Next(0, events.Count)];
            var gameData = _gameData;
            if (gameData.ObjectDescs[gameData.IdToObjectType[evt.ObjectId]].PerRealmMax == 1)
                events.Remove(evt);

            if (!_world.Closed)
            {
                SpawnEvent(evt.ObjectId, evt.Setpiece);

                // new event is critical?
                dat = null;
                foreach (var i in _tauntData)
                    if (evt.ObjectId == i.Name)
                    {
                        dat = i;
                        break;
                    }

                if (dat == null)
                    return;

                // has spawn message?
                if (dat.Spawn != null)
                {
                    var arr = dat.Spawn;
                    string msg = arr[_rand.Next(0, arr.Length)];
                    BroadcastMsg(msg);
                }
            }

            foreach (var player in _world.Players)
            {
                player.Value.HandleQuest(_dummyTime, true);
            }
        }

        public void InitCloseRealm()
        {
            Closing = true;
            _world.Manager.Chat.Announce($"{_world.SBName} closing in 1 minute.", true);
            _world.Timers.Add(new WorldTimer(60000, (_, _) => CloseRealm()));
        }

        private void CloseRealm()
        {
            _world.Closed = true;
            BroadcastEnemyMsg("Thyrr, the Viperous Reaper", "WHO DARES WAKE ME UP?!");
            BroadcastEnemyMsg("Thyrr, the Viperous Reaper",
                "Heroes who defeated my servants, you will not reach the Royal Fortress!");
        }

        public void SendToCastle()
        {
            BroadcastMsg("MY MINIONS HAVE FAILED ME!");
            BroadcastMsg("BUT NOW YOU SHALL FEEL MY WRATH!");
            BroadcastMsg("COME MEET YOUR DOOM AT THE WALLS OF MY CASTLE!");

            if (_world.Players.Count <= 0)
                return;

            var castle = _world.Manager.AddWorld(
                new Castle(_world.Manager.Resources.Worlds.Data["Castle"], playersEntering: _world.Players.Count));
            _world.QuakeToWorld(castle);
        }
        
        public void Init()
        {
            Log.Info($"Overseer is initializing {_world.Id} ({_world.GetDisplayName()}).");

            var w = _world.Map.Width;
            var h = _world.Map.Height;
            var stats = new int[15];

            var random = _rand;
            string[] objects;
            string obj;

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var tile = _world.Map[x, y];
                switch (tile.Region)
                {
                    case TileRegion.None:
                    case TileRegion.Spawn:
                    case TileRegion.Safezone:
                    case TileRegion.Biome_Ocean_Unwalk:
                        continue;
                    case TileRegion.Biome_Valley:
                        if (random.NextDouble() < 1f / 20)
                        {
                            AddEntity(new IntPoint(x, y), "Rock");
                        }

                        break;
                    case TileRegion.Biome_Fungal_Forest:
                        if (random.NextDouble() < 1f / 15)
                        {
                            objects = new[]
                            {
                                "Small Fungus", "Large Fungus", "Sporing Fungi",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Desert:
                        if (random.NextDouble() < 1f / 25)
                        {
                            objects = new[]
                            {
                                "Dead Bush", "Cactus", "Large Cactus", "Flowering Cactus", "Dying Bush", "Tumbleweed",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Elvish_Wood:
                        if (random.NextDouble() < 1f / 15)
                        {
                            objects = new[]
                            {
                                "Flowers", "Golden Tree", "Golden Bush",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Snowy_Peaks:
                        if (random.NextDouble() < 1f / 15)
                        {
                            AddEntity(new IntPoint(x, y), "Rock");
                        }

                        break;
                    case TileRegion.Biome_Scorch:
                        if (random.NextDouble() < 1f / 15)
                        {
                            objects = new[]
                            {
                                "Rock", "Burning Rock", "Ash Pile",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Forest:
                        if (random.NextDouble() < 1f / 15)
                        {
                            objects = new[]
                            {
                                "Tree", "Bush", "Dead Bush",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Rainforest:
                        if (random.NextDouble() < 1f / 15)
                        {
                            objects = new[]
                            {
                                "Tree", "Bush", "Dead Bush", "Tall Tree",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Coastal_Forest:
                        if (random.NextDouble() < 1f / 25)
                        {
                            objects = new[]
                            {
                                "Tree", "Bush", "Dead Bush",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Swamp:
                        if (random.NextDouble() < 1f / 20)
                        {
                            objects = new[]
                            {
                                "Lily Pads", "Mud Pile",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                    case TileRegion.Biome_Deep_Forest:
                        if (random.NextDouble() < 1f / 10)
                        {
                            objects = new[]
                            {
                                "Tree", "Bush", "Dead Bush",
                            };
                            obj = objects[random.Next(objects.Length)];
                            AddEntity(new IntPoint(x, y), obj);
                        }

                        break;
                }
                stats[(int)tile.Region - 26]++;
            }

            foreach (var i in _spawnData)
            {
                var region = i.Region;
                var idx = (int)region - 26;
                var enemyCount = stats[idx] / i.Divider;
                _enemyMaxCounts[idx] = enemyCount;
                _enemyCounts[idx] = 0;

                for (var j = 0; j < enemyCount; j++)
                {
                    var objType = GetRandomObjType(i.Spawns);

                    if (objType == 0)
                        continue;

                    _enemyCounts[idx] += Spawn(_gameData.ObjectDescs[objType], region, w, h);

                    if (_enemyCounts[idx] >= enemyCount)
                        break;
                }
            }

            Log.Info($"Overseer has finished initializing {_world.Id} ({_world.GetDisplayName()}).");
        }
        
        private void AddEntity(IntPoint position, string obj)
        {
            Entity en;
            en = Entity.Resolve(_world.Manager, obj);
            en.Move(position.X + 0.5f, position.Y + 0.5f);
            _world.EnterWorld(en);
        }
    }
}