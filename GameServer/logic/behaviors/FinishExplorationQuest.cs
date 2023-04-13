using System.Xml.Linq;
using GameServer.realm;

namespace GameServer.logic.behaviors
{
    class FinishExplorationQuest : Behavior
    {
        //State storage: none

        public FinishExplorationQuest(XElement e)
        {
        }
        
        public FinishExplorationQuest()
        {
        }
        
        protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
        {
            if (host.AttackTarget == null || host.AttackTarget.CharacterQuests == null) return;

            int i;
            var quests = host.AttackTarget.CharacterQuests;
            for (i = 0; i < quests.Length; i++)
            {
                if (quests[i].Scout != null &&
                    quests[i].Scout == host.Owner.Name ||
                    quests[i].Scout == host.Owner.SBName)
                {
                    quests[i].Scouted = true;
                    quests[i].Goals[0]++;
                    break;
                }
            }
            
            quests = host.AttackTarget.Client.Account.AccountQuests;
            for (i = 0; i < quests.Length; i++)
            {
                if (quests[i].Scout != null &&
                    quests[i].Scout == host.Owner.Name ||
                    quests[i].Scout == host.Owner.SBName)
                {
                    quests[i].Scouted = true;
                    quests[i].Goals[0]++;
                    break;
                }
            }

            host.AttackTarget.Client.Account.AccountQuests = quests;
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
        }
    }
}