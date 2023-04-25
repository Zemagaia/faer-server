using Shared.resources;

namespace GameServer.realm.entities.player; 

public partial class Player
{
    internal Projectile PlayerShootProjectile(
        byte id, ProjectileDesc desc, ushort objType,
        long time, float x, float y, float angle, int projectileId)
    {
        bulletId = id;
        return CreateProjectile(desc, objType,
            time, x, y, angle, projectileId);
    }
}