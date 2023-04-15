using System.Reflection;
using Shared;
using Shared.resources;
using GameServer.logic.loot;
using GameServer.realm;
using GameServer.realm.entities;
using NLog;

namespace GameServer.logic
{
    public partial class BehaviorDb
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public RealmManager Manager { get; }

        private static int _initializing;
        internal static BehaviorDb InitDb;
        internal static XmlData InitGameData => InitDb.Manager.Resources.GameData;

        public BehaviorDb(RealmManager manager)
        {
            Log.Info("Initializing Behavior Database...");

            Manager = manager;

            Definitions = new Dictionary<ushort, Tuple<State, Loot>>();

            if (Interlocked.Exchange(ref _initializing, 1) == 1)
            {
                Log.Error("Attempted to initialize multiple BehaviorDb at the same time.");
                throw new InvalidOperationException("Attempted to initialize multiple BehaviorDb at the same time.");
            }

            InitDb = this;

            var fields = GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.FieldType == typeof(_))
                .ToArray();
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                Log.Info("Loading behavior for '{0}'({1}/{2})...", field.Name, i + 1, fields.Length);
                ((_)field.GetValue(this))();
                field.SetValue(this, null);
            }

            InitXmlBehaviors();
            _initializing = 0;
            Log.Info("Behavior Database initialized...");
        }

        public void InitXmlBehaviors(bool loaded = false)
        {
            var dat = InitDb.Manager.Resources;
            var id2ObjType = dat.GameData.IdToObjectType;
            foreach (var xmlBehavior in dat.RawXmlBehaviors)
            {
                var entry = new XmlBehaviorEntry(xmlBehavior, xmlBehavior.GetAttribute<string>("id"));
                var rootState = entry.Behaviors.Where(x => x is State).Select(x => (State)x)
                    .FirstOrDefault(x => x.Name == "root");
                if (rootState == null)
                {
                    Log.Error($"Error when adding \"{entry.Id}\": no root state.");
                    continue;
                }
                
                var d = new Dictionary<string, State>();
                rootState.Resolve(d);
                rootState.ResolveChildren(d);
                if (!id2ObjType.ContainsKey(entry.Id))
                {
                    Log.Error($"Error when adding \"{entry.Id}\": entity not found.");
                    continue;
                }

                if (entry.Loots.Length > 0)
                {
                    var loot = new Loot(entry.Loots);
                    rootState.Death += (_, e) => loot.Handle((Enemy)e.Host);
                    if (loaded)
                    {
                        Definitions[id2ObjType[entry.Id]] = new Tuple<State, Loot>(rootState, loot);
                        continue;
                    }
                    Definitions.Add(id2ObjType[entry.Id], new Tuple<State, Loot>(rootState, loot));
                }
                else
                {
                    if (loaded)
                    {
                        Definitions[id2ObjType[entry.Id]] = new Tuple<State, Loot>(rootState, null);
                        continue;
                    }
                    
                    Definitions.Add(id2ObjType[entry.Id], new Tuple<State, Loot>(rootState, null));
                }
            }
            
            Log.Info($"Loaded {dat.RawXmlBehaviors.Count()} XML Behaviors");
        }

        public static void SendItem(string item)
        {
            var dat = InitDb.Manager.Resources.GameData;
            if (!dat.IdToObjectType.ContainsKey(item))
            {
                Log.Error($"Item \"{item}\" not found!");
            }
        }

        public void ResolveBehavior(Entity entity)
        {
            if (Definitions.TryGetValue(entity.ObjectType, out var def))
                entity.SwitchTo(def.Item1);
        }

        private delegate ctor _();

        private struct ctor
        {
            public ctor Init(string objType, State rootState, params ILootDef[] defs)
            {
                var d = new Dictionary<string, State>();
                rootState.Resolve(d);
                rootState.ResolveChildren(d);
                var dat = InitDb.Manager.Resources.GameData;

                if (!dat.IdToObjectType.ContainsKey(objType))
                {
                    Log.Error($"Failed to add behavior: {objType}. Xml data not found.");
                    return this;
                }

                if (defs.Length > 0)
                {
                    var loot = new Loot(defs);
                    rootState.Death += (_, e) => loot.Handle((Enemy)e.Host);
                    if (dat.IdToObjectType.ContainsKey(objType))
                        InitDb.Definitions.Add(dat.IdToObjectType[objType], new Tuple<State, Loot>(rootState, loot));
                }
                else
                {
                    if (dat.IdToObjectType.ContainsKey(objType))
                        InitDb.Definitions.Add(dat.IdToObjectType[objType], new Tuple<State, Loot>(rootState, null));
                }

                return this;
            }
        }

        private static ctor Behav()
        {
            return new ctor();
        }

        public Dictionary<ushort, Tuple<State, Loot>> Definitions { get; }
    }
}