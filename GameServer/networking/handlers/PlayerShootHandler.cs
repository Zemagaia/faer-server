using common;
using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class PlayerShootHandler : PacketHandlerBase<PlayerShoot>
    {
        public override PacketId ID => PacketId.PLAYERSHOOT;

        protected override void HandlePacket(Client client, PlayerShoot packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, packet, t));
            //Handle(client.Player, packet);
        }

        void Handle(Player player, PlayerShoot packet, RealmTime time)
        {
            if (player?.Owner == null) return;

            Item item;
            if (!player.Manager.Resources.GameData.Items.TryGetValue(packet.ContainerType, out item)) return;

            // if not shooting main weapon do nothing (ability shoot is handled with useItem)
            if (player.Inventory[1].Item != item)
                return;
            var itemData = player.Inventory[1];

            var tenMod = 1d - (double)player.Stats[13] / 100;
            if (!player.HasConditionEffect(ConditionEffects.Suppressed))
                if (item.Power == "Unstable Mind") // make this a switch when this gets big
                {
                    if (MathUtils.NextDouble() <= 0.01)
                        player.ApplyConditionEffect(ConditionEffectIndex.Unsteady, (int)(2000 * tenMod));
                }

            if (player.NextAttackMpRefill != 0)
            {
                player.MP += player.NextAttackMpRefill;
                player.NextAttackMpRefill = 0;
            }

            // create projectile and show other players
            var prjDesc = item.Projectiles[0]; //Assume only one
            var nextShotMs = 1 / player.DexRateOfFire() * 1 / item.RateOfFire;

            if (player.IsInvalidTime(time.TotalElapsedMs, packet.Time))
            {
                // number of times random is called on projectile creation
                player.DropNextRandom(2);
                return;
            }

            // reset shot counter
            if (packet.Time != player.AcClientLastShot && player.AcShotNum >= item.NumProjectiles)
                player.AcShotNum = 0;

            var arcGap = item.ArcGap * Math.PI / 180;
            var startAngle = packet.Angle - (item.NumProjectiles - 1) / 2 * arcGap;
            // validate shots, number of shots and etc
            if ((packet.Time > player.AcClientLastShot + nextShotMs || player.AcClientLastShot == 0 || packet.Time == player.AcClientLastShot) &&
                player.AcShotNum < item.NumProjectiles && !player.HasConditionEffect(ConditionEffects.Stunned))
            {
                if (player.AcClientLastShot == 0) player.AcClientLastShot = packet.Time;

                var prj = player.PlayerShootProjectile(
                    packet.BulletId, prjDesc, item.ObjectType,
                    packet.Time, packet.StartingPos, (float)(startAngle + arcGap * player.AcShotNum), itemData,
                    player.AcShotNum);
                player.Owner?.EnterWorld(prj);
                player.Owner?.BroadcastPacketNearby(new AllyShoot()
                {
                    OwnerId = player.Id,
                    Angle = prj.Angle,
                    ContainerType = item.ObjectType,
                    BulletId = packet.BulletId
                }, player, player);
                player.FameCounter.Shoot(prj);
                player.AcShotNum++;
                player.AcClientLastShot = packet.Time;
                return;
            }
            
            player.DropNextRandom(2);
        }
    }
}