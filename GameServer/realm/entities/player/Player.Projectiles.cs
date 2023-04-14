using common.resources;

namespace GameServer.realm.entities.player
{
    public partial class Player
    {
        internal Projectile PlayerShootProjectile(
            byte id, ProjectileDesc desc, ushort objType,
            long time, Position position, float angle, int projectileId)
        {
            bulletId = id;
            return CreateProjectile(desc, objType, (int)Stats.GetAttackDamage(desc),
                time, position, angle, projectileId);
        }
    }
}