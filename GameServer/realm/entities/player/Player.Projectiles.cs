using common.resources;

namespace GameServer.realm.entities.player
{
    public partial class Player
    {
        internal Projectile PlayerShootProjectile(
            byte id, ProjectileDesc desc, ushort objType,
            long time, Position position, float angle, ItemData itemData, int projectileId)
        {
            bulletId = id;
            int dmg;
            if (itemData.Quality > 0)
                dmg = (int)(Stats.GetAttackDamage(desc) * itemData.Quality);
            else
                dmg = (int)Stats.GetAttackDamage(desc);
            return CreateProjectile(desc, objType, dmg,
                time, position, angle, projectileId);
        }
    }
}