using common.resources;
using GameServer.logic.loot;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class InvDropHandler : PacketHandlerBase<InvDrop>
    {
        static readonly Random InvRand = new();

        public override PacketId ID => PacketId.INVDROP;

        protected override void HandlePacket(Client client, InvDrop packet)
        {
            //client.Manager.Logic.AddPendingAction(t => Handle(client.Player, packet.SlotObject.SlotId));
            //Handle(client.Player, packet.SlotObject);
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, packet.SlotObject));
        }

        private void Handle(Player player, ObjectSlot slot)
        {
            if (player?.Owner == null || player.tradeTarget != null)
                return;

            IContainer con;

            // container isn't always the player's inventory, it's given by the SlotObject's ObjectId
            if (slot.ObjectId != player.Id)
            {
                if (player.Owner.GetEntity(slot.ObjectId) is Player)
                {
                    player.Client.SendPacket(new InvResult() { Result = 1 });
                    return;
                }

                con = player.Owner.GetEntity(slot.ObjectId) as IContainer;
            }
            else
            {
                con = player as IContainer;
            }

            // disallow dropping stacked items
            if (slot.ObjectId == player.Id && player.Stacks.Any(stack => stack.Slot == slot.SlotId))
            {
                player.Client.SendPacket(new InvResult() { Result = 1 });
                return;
            }

            // give proper error
            if (con?.Inventory[slot.SlotId].Item == null)
            {
                player.Client.SendPacket(new InvResult() { Result = 1 });
                return;
            }

            // disallow removing from gift chest
            if (con is GiftChest)
            {
                player.Client.SendPacket(new InvResult() { Result = 1 });
                return;
            }

            var item = con.Inventory[slot.SlotId];
            con.Inventory[slot.SlotId] = new ItemData();

            // create new container for item to be placed in
            Container container;
            if (item.Item.Soulbound || item.Soulbound || player.Client.Account.Admin)
            {
                container = new Container(player.Manager, Loot.BlackBag, 1000 * 60, true);
                container.BagOwners = new[] { player.AccountId };
            }
            else
            {
                container = new Container(player.Manager, Loot.BrownBag, 1000 * 60, true);
            }

            // init container
            container.Inventory.SetItems(new[] { item });
            container.Move(player.X + (float)((InvRand.NextDouble() * 2 - 1) * 0.5),
                player.Y + (float)((InvRand.NextDouble() * 2 - 1) * 0.5));
            container.SetDefaultSize(75);
            player.Owner.EnterWorld(container);

            // send success
            player.Client.SendPacket(new InvResult() { Result = 0 });
        }
    }
}