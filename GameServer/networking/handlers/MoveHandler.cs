using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class MoveHandler : PacketHandlerBase<Move>
    {
        public override PacketId ID => PacketId.MOVE;

        protected override void HandlePacket(Client client, Move packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet));
        }

        void Handle(Player player, RealmTime time, Move packet)
        {
            if (player?.Client.State == ProtocolState.Reconnecting || player?.Owner == null)
                return;

            player.MoveReceived(time, packet);

            var newX = packet.NewPosition.X;
            var newY = packet.NewPosition.Y;
            if (player.SpectateTarget == null && player.Id == packet.ObjectId ||
                player.SpectateTarget?.Id == packet.ObjectId)
            {
                if (newX != -1 && newX != player.X ||
                    newY != -1 && newY != player.Y)
                {
                    player.Move(newX, newY);
                    /*player.AcLastMoveId++;
                    player.AcIgnoreLastMove[player.AcLastMoveId] = player.SpectateTarget is not null;
                    player.AcLastMove[player.AcLastMoveId] = new Position() { X = newX, Y = newY };*/
                }
            }
            
            /*if (player.HasConditionEffect(ConditionEffects.Paused)) return;

            var last = (byte)(player.AcLastMoveId - 1);
            if (!player.AcIgnoreLastMove[last] && player.AcLastMove[last].X != 0)
            {
                if (Math.Abs(MathsUtils.DistSqr(newX, newY, player.AcLastMove[last].X, player.AcLastMove[last].Y))
                    > player.Stats.GetSpeed(player.Owner.Map[(int)newX, (int)newY]) * 1.3)
                {
                    player.AcMoveInfractions++;
                    if (player.AcMoveInfractions > 3)
                    {
                        player.Client.Disconnect();
                        Log.Warn($"{player.Name} disconnected for moving too fast.");
                    }
                }
            }*/
        }
    }
}