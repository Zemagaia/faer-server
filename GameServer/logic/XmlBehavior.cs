using System.Reflection;
using System.Xml.Linq;
using Shared;
using GameServer.logic.loot;

namespace GameServer.logic
{
    public class XmlBehaviorEntry
    {
        public readonly string Id;
        public readonly IStateChildren[] Behaviors;
        public readonly ILootDef[] Loots;

        public XmlBehaviorEntry(XElement e, string id)
        {
            Id = id;
            var behaviorTypes = Assembly.GetCallingAssembly().GetTypes()
                .Where(type => typeof(IStateChildren).IsAssignableFrom(type) && !type.IsInterface)
                .Select(type => type).ToArray();
            var behaviorTemplateTypes = typeof(BehaviorTemplates).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var lootTypes = Assembly.GetCallingAssembly().GetTypes()
                .Where(type => typeof(ILootDef).IsAssignableFrom(type) && !type.IsInterface)
                .Select(type => type).ToArray();
            var behaviors = new List<IStateChildren>();
            var loots = new List<ILootDef>();
            ParseStates(e, behaviorTypes, behaviorTemplateTypes, ref behaviors);
            if (e.Elements("LootDef").Count() + e.Elements("LootTemplate").Count() > 0)
                ParseLoot(e, lootTypes, ref loots);
            Behaviors = behaviors.ToArray();
            Loots = loots.ToArray();
        }

        private static void ParseStates(XElement e, Type[] results, MethodInfo[] templates, ref List<IStateChildren> behaviors)
        {
            var children = new List<IStateChildren>();
            foreach (var i in e.Elements("State"))
            {
                if (i.Elements("State").Count() > 0)
                    ParseStates(i, results, templates, ref children);
                foreach (var j in i.Elements("BehaviorTemplate"))
                {
                    var method = templates.FirstOrDefault(x => x.Name == j.Value);
                    if (method != null)
                        children.AddRange((IStateChildren[])method.Invoke(null, new[] { j }));
                }

                if (i.Elements("Behavior").Count() > 0)
                    ParseBehaviors(i, results, ref children);

                var state = (IStateChildren)Activator.CreateInstance(results.Single(x => x.Name == "State"),
                    i.GetAttribute<string>("id"), children.ToArray());
                behaviors.Add(state);
                children.Clear();
            }
        }

        private static void ParseBehaviors(XElement e, Type[] results, ref List<IStateChildren> behaviors)
        {
            var children = new List<IStateChildren>();
            foreach (var i in e.Elements("Behavior"))
            {
                if (i.Elements("Behavior").Count() > 0)
                    ParseBehaviors(i, results, ref children);

                var name = i.Attribute("behavior") != null ? i.GetAttribute<string>("behavior") : i.Value;
                IStateChildren behavior;
                if (children.Count > 0)
                    behavior = (IStateChildren)Activator.CreateInstance(results.Single(x => x.Name == name), i,
                        children.ToArray());
                else
                    behavior = (IStateChildren)Activator.CreateInstance(results.Single(x => x.Name == name), i);
                behaviors.Add(behavior);
                children.Clear();
            }
        }

        private static void ParseLoot(XElement e, Type[] results, ref List<ILootDef> loots)
        {
            var children = new List<ILootDef>();
            foreach (var j in e.Elements("LootTemplate"))
            {
                var method = typeof(LootTemplates).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(x => x.Name == j.Name);
                if (method != null)
                    loots.AddRange((ILootDef[])method.Invoke(null, null));
            }

            foreach (var i in e.Elements("LootDef"))
            {
                if (i.Elements("LootDef").Count() > 0)
                    ParseLoot(i, results, ref children);

                var name = i.Attribute("behavior") != null ? i.GetAttribute<string>("behavior") : i.Value;
                ILootDef behavior;
                if (children.Count > 0)
                    behavior = (ILootDef)Activator.CreateInstance(results.Single(x => x.Name == name), i,
                        children.ToArray());
                else
                    behavior = (ILootDef)Activator.CreateInstance(results.Single(x => x.Name == name), i);
                if (behavior is null)
                {
                    continue;
                }

                loots.Add(behavior);
                children.Clear();
            }
        }
    }
}