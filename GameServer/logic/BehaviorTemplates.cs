using System.Xml.Linq;
using common;
using GameServer.logic.behaviors;

namespace GameServer.logic
{
    public static class BehaviorTemplates
    {
        public static IStateChildren[] CrazyShotgun(XElement e)
        {
            return new []
            {
                new Shoot(10, 10, 10, coolDown: e.ParseInt("@coolDown", 1000))
            };
        }
    }
}