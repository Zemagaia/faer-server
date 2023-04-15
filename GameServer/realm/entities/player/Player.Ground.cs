using Shared;
using Shared.resources;
using GameServer.realm.worlds;

namespace GameServer.realm.entities.player
{
    public partial class Player
    {
        long l;

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

       public void DamagePlayerGround(Position pos, int damage)
        {
            WmapTile tile = Owner.Map[(int)pos.X, (int)pos.Y];
            TileDesc tileDesc = Manager.Resources.GameData.Tiles[tile.TileId];

            var limit = (int)Math.Min(ShieldMax + 100, ShieldMax * 1.3);
            if (Shield > 0)
                ShieldDamage += damage;
            else if (ShieldDamage + damage <= limit)
            {
                // more accurate... maybe
                ShieldDamage += damage;
                HP -= damage;
            }
            else
                HP -= damage;
            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(p.X, Y, X, Y) < 16 * 16)
                        p.Client.SendDamage(Id, 0, (ushort)damage, HP <= 0, 0, 0);
            if (HP <= 0)
            {
                Death(tileDesc.ObjectId, tile: tile);
            }
        }
    }
}