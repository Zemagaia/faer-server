using Shared;
using Shared.resources;
using GameServer.realm.worlds;

namespace GameServer.realm.entities.player; 

public partial class Player
{
    private long l;

    /*  private void HandleOceanTrenchGround(RealmTime time)
     {
         try
         {
             // don't suffocate hidden players
             if (HasConditionEffect(ConditionEffects.Hidden)) return;

             if (time.TotalElapsedMs - l <= 100 || Owner?.Name != "OceanTrench") return;

             if (!(Owner?.StaticObjects.Where(i => i.Value.ObjectType == 0x098e).Count(i =>
                 (X - i.Value.X) * (X - i.Value.X) + (Y - i.Value.Y) * (Y - i.Value.Y) < 1) > 0))
             {
                 if (OxygenBar == 0)
                     HP -= 10;
                 else
                     OxygenBar -= 2;

                 if (HP <= 0)
                     Death("suffocation");
             }
             else
             {
                 if (OxygenBar < 100)
                     OxygenBar += 8;
                 if (OxygenBar > 100)
                     OxygenBar = 100;
             }

             l = time.TotalElapsedMs;
         }
         catch (Exception ex)
         {
             Log.Error(ex);
         }
     } */
}