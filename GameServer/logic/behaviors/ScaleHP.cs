using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

// ScaleHP behavior by Sanusei MPGH
namespace GameServer.logic.behaviors; 

internal class ScaleHP : Behavior
{
    //State storage: scalehp state
    private class ScaleHPState
    {
        public IList<string> pNamesCounted;
        public int initialScaleAmount = 0;
        public int maxHP;
        public bool hitMaxHP;
        public int cooldown;
    }


    private readonly int amountPerPlayer;
    private readonly int maxAdditional; // leave as 0 for no limit
    private readonly bool healAfterMax;
    private readonly int dist; // leave as 0 for all players
    private readonly int scaleAfter;
        
    public ScaleHP(XElement e)
    {
        amountPerPlayer = e.ParseInt("@amountPerPlayer");
        maxAdditional = e.ParseInt("@maxAdditional");
        healAfterMax = e.ParseBool("@healAfterMax");
        dist = e.ParseInt("@dist");
        scaleAfter = e.ParseInt("@scaleAfter", 1);
    }

    public ScaleHP(int amountPerPlayer, int maxAdditional, bool healAfterMax = true, int dist = 0, int scaleAfter = 1)
    {
        this.amountPerPlayer = amountPerPlayer;
        this.maxAdditional = maxAdditional;
        this.healAfterMax = healAfterMax;
        this.dist = dist;
        this.scaleAfter = scaleAfter;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        state = new ScaleHPState
        {
            pNamesCounted = new List<string>(),
            initialScaleAmount = scaleAfter,
            maxHP = 0,
            hitMaxHP = false,
            cooldown = 0
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
        var scstate = (ScaleHPState)state;
        if (scstate.cooldown <= 0)
        {
            scstate.cooldown = 1000;
            if (!(host is Enemy)) return;

            if (scstate.maxHP == 0)
                scstate.maxHP = (host as Enemy).MaximumHP + maxAdditional;

            var plrCount = 0;
            foreach (var i in host.Owner.Players)
            {
                if (scstate.pNamesCounted.Contains(i.Value.Name)) continue;
                if (dist > 0)
                {
                    if (host.Dist(i.Value) < dist)
                        scstate.pNamesCounted.Add(i.Value.Name);
                }
                else
                    scstate.pNamesCounted.Add(i.Value.Name);
            }
            plrCount = scstate.pNamesCounted.Count;
            if (plrCount > scstate.initialScaleAmount)
            {
                var amountInc = (plrCount - scstate.initialScaleAmount) * amountPerPlayer;
                scstate.initialScaleAmount += (plrCount - scstate.initialScaleAmount);

                if (maxAdditional != 0)
                    amountInc = Math.Min(maxAdditional, amountInc);

                // ex: Enemy with 4000HP / 8000HP, being increased by 1200
                var curHp = (host as Enemy).HP;                             // ex: current hp was 4000HP
                var hpMaximum = (host as Enemy).MaximumHP;                  // ex: max hp was 8000HP
                var curHpPercent = ((double)curHp / hpMaximum);          // ex: 0.5
                var newHpMaximum = (host as Enemy).MaximumHP + amountInc;   // ex: max hp is now 9200HP
                var newHp = Convert.ToInt32(newHpMaximum * curHpPercent);   // ex: current has is now 4600HP

                if (!scstate.hitMaxHP || healAfterMax)
                {
                    (host as Enemy).HP = newHp;
                    (host as Enemy).MaximumHP = newHpMaximum;
                }
                if ((host as Enemy).MaximumHP >= scstate.maxHP && maxAdditional != 0)
                {
                    (host as Enemy).MaximumHP = scstate.maxHP;
                    scstate.hitMaxHP = true;
                }

                if ((host as Enemy).HP > (host as Enemy).MaximumHP)
                    (host as Enemy).HP = (host as Enemy).MaximumHP;
            }
        }
        else
            scstate.cooldown -= time.ElapsedMsDelta;

        state = scstate;
    }
}