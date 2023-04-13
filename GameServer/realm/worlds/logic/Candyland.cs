using common.resources;
using GameServer.networking;

namespace GameServer.realm.worlds.logic
{
    class Candyland : World
    {
        private IEnumerable<Entity> _candySpawners;
        private Entity _candyBossSpawner;

        public Candyland(ProtoWorld proto, Client client = null) : base(proto)
        {
        }

        protected override void Init()
        {
            base.Init();

            if (IsLimbo) return;

            _candySpawners = Enemies.Values.Where(e => e.ObjectType == 0x006b);
            _candyBossSpawner = Enemies.Values.SingleOrDefault(e => e.ObjectType == 0x005e);

            foreach (var cs in _candySpawners)
                cs.TickStateManually = true;

            if (_candyBossSpawner != null)
                _candyBossSpawner.TickStateManually = true;
        }

        public override void Tick(RealmTime time)
        {
            if (IsLimbo || Deleted || _candySpawners == null || _candyBossSpawner == null)
                return;

            foreach (var cs in _candySpawners)
                cs.TickState(time);
            _candyBossSpawner.TickState(time);

            base.Tick(time);
        }
    }
}