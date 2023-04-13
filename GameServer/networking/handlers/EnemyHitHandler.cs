using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class EnemyHitHandler : PacketHandlerBase<EnemyHit>
    {
        public override PacketId ID => PacketId.ENEMYHIT;

        protected override void HandlePacket(Client client, EnemyHit packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet));
            //Handle(client.Player, DummyTime, packet);
        }

        void Handle(Player player, RealmTime time, EnemyHit pkt)
        {
            var entity = player?.Owner?.GetEntity(pkt.TargetId);
            if (entity?.Owner == null)
                return;

            if (player.Client.IsLagging || player.HasConditionEffect(ConditionEffects.Hidden))
                return;

            if (player.IsInvalidTime(time.TotalElapsedMs, pkt.Time))
                return;
            
            var prj = (player as IProjectileOwner).Projectiles[pkt.BulletId];
            // Console.WriteLine($"hit:  {pkt.BulletId} {prj}");
            prj?.ForceHit(entity, time, pkt.Time);
            if ((entity as Enemy)?.HP < 0)
                player.ClientKilledEntity.Enqueue(entity);
        }
    }
}