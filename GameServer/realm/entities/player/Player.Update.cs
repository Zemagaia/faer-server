using System.Collections.Concurrent;
using Shared.terrain;
using GameServer.realm.worlds;

namespace GameServer.realm.entities.player; 

public class UpdatedSet : HashSet<Entity>
{
    private readonly Player _player;
    private readonly object _changeLock = new();

    public UpdatedSet(Player player)
    {
        _player = player;
    }

    public new bool Add(Entity e)
    {
        lock (_changeLock)
        {
            var added = base.Add(e);
            if (added)
                e.StatChanged += _player.HandleStatChanges;

            return added;
        }
    }

    public new bool Remove(Entity e)
    {
        lock (_changeLock)
        {
            e.StatChanged -= _player.HandleStatChanges;
            return base.Remove(e);
        }
    }

    public new void RemoveWhere(Predicate<Entity> match)
    {
        lock (_changeLock)
        {
            foreach (var e in this.Where(match.Invoke))
                e.StatChanged -= _player.HandleStatChanges;

            base.RemoveWhere(match);
        }
    }

    public void Dispose()
    {
        RemoveWhere(e => true);
    }
}

public partial class Player
{
    public HashSet<Entity> clientEntities => _clientEntities;

    public readonly ConcurrentQueue<Entity> ClientKilledEntity = new();

    public const int Radius = 20;
    public const int RadiusSqr = Radius * Radius;
    private const int StaticBoundingBox = Radius * 2;
    private const int AppoxAreaOfSight = (int)(Math.PI * Radius * Radius + 1);

    private readonly HashSet<IntPoint> _clientStatic = new();
    private readonly UpdatedSet _clientEntities;
    private ObjectStats[] _updateStatuses;
    private TileData[] _tiles;
    private ObjectDef[] _newObjects;
    private int[] _removedObjects;

    private readonly object _statUpdateLock = new();

    private readonly Dictionary<Entity, Dictionary<StatsType, object>> _statUpdates =
        new();

    public Sight Sight { get; private set; }

    public int TickId;
    
    public void DisposeUpdate() {
        _clientEntities.Dispose();
        _clientStatic.Clear();
        _newObjects = null;
        _newStatics.Clear();
        _removedObjects = null;
        _statUpdates.Clear();
        _tiles = null;
        _updateStatuses = null;
    }

    public void HandleStatChanges(object entity, StatChangedEventArgs statChange)
    {
        var e = entity as Entity;
        if (e == null || e != this && statChange.UpdateSelfOnly)
            return;

        lock (_statUpdateLock)
        {
            if (e == this && statChange.Stat == StatsType.None)
                return;

            if (!_statUpdates.ContainsKey(e))
                _statUpdates[e] = new Dictionary<StatsType, object>();

            if (statChange.Stat != StatsType.None)
                _statUpdates[e][statChange.Stat] = statChange.Value;

            //Log.Info($"{entity} {statChange.Stat} {statChange.Value}");
        }
    }

    private void SendNewTick(RealmTime time)
    {
        lock (_statUpdateLock)
        {
            _updateStatuses = _statUpdates.Select(_ => new ObjectStats()
            {
                Id = _.Key.Id,
                X = _.Key.RealX, 
                Y = _.Key.RealY,
                StatTypes = _.Value.ToArray()
            }).ToArray();
            _statUpdates.Clear();
        }
            
        _client.SendNewTick((byte) (++TickId % 256), (byte) Manager.TPS, _updateStatuses);
        AwaitMove(TickId);
    }

    private void SendUpdate(RealmTime time)
    {
        // init sight circle
        var sCircle = Sight.GetSightCircle(Owner.Blocking);

        // get list of tiles for update
        var tilesUpdate = new List<TileData>(AppoxAreaOfSight);
        foreach (var point in sCircle)
        {
            var x = point.X;
            var y = point.Y;
            var tile = Owner?.Map[x, y] ?? new MapTile();

            if (tile.TileType == 255 ||
                tiles[x, y] >= tile.UpdateCount)
                continue;

            tilesUpdate.Add(new TileData
            {
                X = (short)x,
                Y = (short)y,
                Tile = tile.TileType
            });
            tiles[x, y] = tile.UpdateCount;
        }
            
        // get list of new static objects to add
        var staticsUpdate = GetNewStatics(sCircle).ToArray();

        // get dropped entities list
        var entitiesRemove = new HashSet<int>(GetRemovedEntities(sCircle));

        // removed stale entities
        _clientEntities.RemoveWhere(e => entitiesRemove.Contains(e.Id));

        // get list of added entities
        var entitiesAdd = GetNewEntities(sCircle).ToArray();

        // get dropped statics list
        var staticsRemove = new HashSet<IntPoint>(GetRemovedStatics());
        _clientStatic.ExceptWith(staticsRemove);

        if (tilesUpdate.Count > 0 || entitiesRemove.Count > 0 || staticsRemove.Count > 0 ||
            entitiesAdd.Length > 0 || staticsUpdate.Length > 0)
        {
            entitiesRemove.UnionWith(
                staticsRemove.Select(s => Owner.Map[s.X, s.Y].ObjId));

            _tiles = tilesUpdate.ToArray();
            _newObjects = entitiesAdd.Select(_ => _.ToDefinition()).Concat(staticsUpdate).ToArray();
            _removedObjects = entitiesRemove.ToArray();
            _client.SendUpdate(_tiles, _newObjects, _removedObjects);
            AwaitUpdateAck(time.TotalElapsedMs);
        }
    }

    private IEnumerable<int> GetRemovedEntities(HashSet<IntPoint> visibleTiles)
    {
        foreach (var e in ClientKilledEntity)
            yield return e.Id;

        foreach (var i in _clientEntities)
        {
            if (i.Owner == null)
                yield return i.Id;

            if (i != this && !i.CanBeSeenBy(this))
                yield return i.Id;

            if (i is StaticObject so && so.Static)
            {
                if (Math.Abs(StaticBoundingBox - ((int)X - i.X)) > 0 &&
                    Math.Abs(StaticBoundingBox - ((int)Y - i.Y)) > 0)
                    continue;
            }

            if (i is Player ||
                i.ObjectDesc.KeepMinimap || /*(i is StaticObject && (i as StaticObject).Static) ||*/
                visibleTiles.Contains(new IntPoint((int)i.X, (int)i.Y)))
                continue;

            yield return i.Id;
        }
    }

    private IEnumerable<Entity> GetNewEntities(HashSet<IntPoint> visibleTiles)
    {
        while (ClientKilledEntity.TryDequeue(out var entity))
            _clientEntities.Remove(entity);

        foreach (var i in Owner.Players)
            if ((i.Value == this || i.Value.Client.Account != null && i.Value.Client.Player.CanBeSeenBy(this)) &&
                _clientEntities.Add(i.Value))
                yield return i.Value;

        var p = new IntPoint(0, 0);
        foreach (var i in Owner.EnemiesCollision.HitTest(X, Y, Radius))
        {
            if (i is Container)
            {
                var owners = (i as Container).BagOwners;
                if (owners.Length > 0 && Array.IndexOf(owners, AccountId) == -1)
                    continue;
            }

            p.X = (int)i.X;
            p.Y = (int)i.Y;
            if (visibleTiles.Contains(p) && _clientEntities.Add(i))
                yield return i;
        }

        foreach (var en in Owner.Enemies.Values)
        {
            if (en != null && en.RealmEvent && _clientEntities.Add(en))
                yield return en;
        }
    }

    private IEnumerable<IntPoint> GetRemovedStatics()
    {
        foreach (var i in _clientStatic)
        {
            var tile = Owner.Map[i.X, i.Y];

            if (
                StaticBoundingBox - ((int)X - i.X) > 0 &&
                StaticBoundingBox - ((int)Y - i.Y) > 0 &&
                tile.ObjType != 0 &&
                tile.ObjId != 0)
                continue;

            yield return i;
        }
    }

    private List<ObjectDef> _newStatics = new(AppoxAreaOfSight);

    private IEnumerable<ObjectDef> GetNewStatics(HashSet<IntPoint> visibleTiles)
    {
        _newStatics.Clear();

        foreach (var i in visibleTiles)
        {
            var tile = Owner?.Map[i.X, i.Y] ?? new MapTile();

            if (tile.ObjId != 0 && tile.ObjType != 0 && _clientStatic.Add(i))
                _newStatics.Add(tile.ToDef(i.X, i.Y));
        }

        return _newStatics;
    }
}