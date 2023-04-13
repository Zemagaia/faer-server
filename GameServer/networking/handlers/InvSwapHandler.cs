using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.networking.packets.outgoing;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers
{
    class InvSwapHandler : PacketHandlerBase<InvSwap>
    {
        private static readonly Random Rand = new();

        public override PacketId ID => PacketId.INVSWAP;

        protected override void HandlePacket(Client client, InvSwap packet)
        {
            client.Manager.Logic.AddPendingAction(t =>
                Handle(
                    client.Player,
                    client.Player.Owner.GetEntity(packet.SlotObj1.ObjectId),
                    client.Player.Owner.GetEntity(packet.SlotObj2.ObjectId),
                    packet.SlotObj1.SlotId, packet.SlotObj2.SlotId));

            /*Handle(
                client.Player,
                client.Player.Owner.GetEntity(packet.SlotObj1.ObjectId),
                client.Player.Owner.GetEntity(packet.SlotObj2.ObjectId),
                packet.SlotObj1.SlotId, packet.SlotObj2.SlotId);*/
        }

        private void Handle(
            Player player,
            Entity a, Entity b,
            int slotA, int slotB)
        {
            if (player?.Owner == null)
                return;

            if (!HandleLevelReq(player, a, b, slotA, slotB)) return;

            if (!ValidateEntities(player, a, b) || player.tradeTarget != null)
            {
                a.ForceUpdate(slotA);
                if (a != b)
                    b.ForceUpdate(slotB);
                player.Client.SendPacket(new InvResult() { Result = 1 });
                return;
            }

            var conA = (IContainer)a;
            var conB = (IContainer)b;

            // check if stacking operation
            if (b == player)
                foreach (var stack in player.Stacks)
                {
                    if (stack.Slot != slotB)
                    {
                        continue;
                    }

                    var stackTrans = conA.Inventory.CreateTransaction();
                    var item = stack.Put(stackTrans[slotA].Item);
                    // if able to stack, item will be null
                    if (item != null)
                    {
                        continue;
                    }

                    stackTrans[slotA] = new ItemData();
                    Inventory.Execute(stackTrans);
                    player.Client.SendPacket(new InvResult() { Result = 0 });
                    return;
                }

            // not stacking operation, continue on with normal swap

            // validate slot types
            if (!ValidateSlotSwap(player, conA, conB, slotA, slotB))
            {
                a.ForceUpdate(slotA);
                if (a != b)
                    b.ForceUpdate(slotB);
                player.Client.SendPacket(new InvResult() { Result = 1 });
                return;
            }

            // setup swap
            var queue = new Queue<Action>();
            var conATrans = conA.Inventory.CreateTransaction();
            var conBTrans = conB.Inventory.CreateTransaction();
            var itemA = conATrans[slotA];
            var itemB = conBTrans[slotB];

            // validate level req when swapping items from bag,
            // soulbound items are not placed in public bags (includes any swapped item from admins)...
            void RevertSwaps()
            {
                conBTrans[slotB] = itemB;
                conATrans[slotA] = itemA;
                b.ForceUpdate(slotB);
                if (a != b)
                    a.ForceUpdate(slotA);
                player.Client.SendPacket(new InvResult() { Result = 1 });
            }

            if (a != b && slotB < 6 && b is Player p && p.Level < itemA.Item.LevelRequirement)
            {
                RevertSwaps();
                return;
            }

            if (!ValidateItemSwap(player, a, itemB))
            {
                RevertSwaps();
                return;
            }

            if (!ValidateItemSwap(player, b, itemA))
            {
                RevertSwaps();
                return;
            }

            var forceUpdate = false;
            var mQuantityA = 0;
            var mQuantityB = 0;
            if (itemA.Item != null)
            {
                mQuantityA = itemA.MaxQuantity == 0 ? itemA.Item.MaxQuantity : itemA.MaxQuantity;
            }

            if (itemB.Item != null)
            {
                mQuantityB = itemB.MaxQuantity == 0 ? itemB.Item.MaxQuantity : itemB.MaxQuantity;
            }

            if (b is not GiftChest && itemA.ObjectType == itemB.ObjectType && mQuantityA > 0 &&
                itemB.Quantity < mQuantityB && itemA.Quantity < mQuantityA && slotA != slotB)
            {
                var quantityA = itemA.Quantity == 0 ? itemB.Item.Quantity : itemA.Quantity;
                var quantityB = itemB.Quantity == 0 ? itemB.Item.Quantity : itemB.Quantity;
                var stackAVal = quantityB + quantityA;
                var diff = 0;

                if (stackAVal > mQuantityA)
                {
                    itemA.Quantity = mQuantityA;
                    diff = stackAVal - itemA.Quantity;
                }
                else
                    itemA.Quantity = stackAVal;

                if (diff == 0)
                    itemB = new ItemData();
                else
                    itemB.Quantity = diff;

                forceUpdate = true;
            }

            int i;
            // do swap check if it's not stacking operation
            if (!forceUpdate)
            {
                var uiids = new HashSet<ulong>();
                var lengthA = conA.Inventory.Length;
                var lengthB = conB.Inventory.Length;
                if (itemA.UIID != 0 && itemA.UIID == itemB.UIID && (slotA != slotB || a != b))
                {
                    Log.Error(
                        $"Inventory A (slot {slotA}) and Inventory B (slot {slotB}) have the same UIID ({itemA.UIID})... {player.Name} ({player.AccountId})");
                    conATrans[slotA] = new ItemData();
                }
                
                for (i = 0; i < lengthA; i++)
                {
                    // UIID should never be null, but if it is... We should actually log it instead...
                    if (conATrans[i] == new ItemData() || conATrans[i].UIID == 0) continue;
                    if (uiids.Add(conATrans[i].UIID)) continue;

                    Log.Error(
                        $"UIIDs already contain UIID ({conATrans[i].UIID}) during check... {player.Name} ({player.AccountId})");
                    conATrans[i] = new ItemData();
                }

                for (i = 0; i < lengthB; i++)
                {
                    if (a == b) break; // inventory a is b and inventory a has been checked already
                    if (conBTrans[i] == new ItemData() || conBTrans[i].UIID == 0) continue;
                    if (uiids.Add(conBTrans[i].UIID)) continue;

                    Log.Error(
                        $"UIIDs already contain UIID ({conBTrans[i].UIID}) during check... {player.Name} ({player.AccountId})");
                    conBTrans[i] = new ItemData();
                }
            }

            conBTrans[slotB] = itemA;
            conATrans[slotA] = itemB;

            // swap items
            if (Inventory.Execute(conATrans, conBTrans))
            {
                while (queue.Count > 0)
                    queue.Dequeue()();

                // update itemdata if stacking operation ocurred
                if (forceUpdate)
                {
                    a.ForceUpdate(slotA);
                    if (a != b)
                        b.ForceUpdate(slotB);
                }

                if (a is Player plrA)
                    plrA.Stats.ReCalculateValues();
                if (b is Player plrB)
                    plrB.Stats.ReCalculateValues();

                player.Client.SendPacket(new InvResult() { Result = 0 });
                return;
            }

            a.ForceUpdate(slotA);
            if (a != b)
                b.ForceUpdate(slotB);
            player.Client.SendPacket(new InvResult() { Result = 1 });
        }

        private bool HandleLevelReq(Player player, Entity a, Entity b, int slotA, int slotB)
        {
            if (b == player && slotB < 6)
            {
                var swappeditem = player.Inventory[slotA].Item;
                if (swappeditem != null)
                {
                    var levelreq = swappeditem.LevelRequirement;

                    if (!ValidateLevel(levelreq, player))
                    {
                        a.ForceUpdate(slotA);
                        if (a != b)
                            b.ForceUpdate(slotB);
                        player.Client.SendPacket(new InvResult() { Result = 1 });
                        return false;
                    }
                }
            }
            
            if (a == player && slotA < 6)
            {
                var swappeditem = player.Inventory[slotB].Item;
                if (swappeditem != null)
                {
                    var levelreq = swappeditem.LevelRequirement;

                    if (!ValidateLevel(levelreq, player))
                    {
                        a.ForceUpdate(slotA);
                        if (a != b)
                            b.ForceUpdate(slotB);
                        player.Client.SendPacket(new InvResult() { Result = 1 });
                        return false;
                    }
                }
            }

            return true;
        }

        bool ValidateEntities(Player p, Entity a, Entity b)
        {
            // returns false if bad input
            if (a == null || b == null)
                return false;

            if ((a as IContainer) == null ||
                (b as IContainer) == null)
                return false;

            if (a is Player && a != p ||
                b is Player && b != p)
                return false;

            if (a is Container &&
                (a as Container).BagOwners.Length > 0 &&
                !(a as Container).BagOwners.Contains(p.AccountId))
                return false;

            if (b is Container &&
                (b as Container).BagOwners.Length > 0 &&
                !(b as Container).BagOwners.Contains(p.AccountId))
                return false;

            if (a is OneWayContainer && b != p ||
                b is OneWayContainer && a != p)
                return false;

            var aPos = new Vector2(a.X, a.Y);
            var bPos = new Vector2(b.X, b.Y);
            if (Vector2.DistanceSquared(aPos, bPos) > 1)
                return false;

            return true;
        }

        bool ValidateLevel(int levelreq, Player player)
        {
            if (levelreq > player.Level)
                return false;
            return true;
        }

        private bool ValidateSlotSwap(Player player, IContainer conA, IContainer conB, int slotA, int slotB)
        {
            return
                (slotA < 18 && slotB < 18 || player.HasBackpack) &&
                conB.AuditItem(conA.Inventory[slotA].Item, slotB) &&
                conA.AuditItem(conB.Inventory[slotB].Item, slotA);
        }

        private bool ValidateItemSwap(Player player, Entity c, ItemData data)
        {
            var item = data.Item;
            return c == player || item == null ||
                   !item.Soulbound && !data.Soulbound && !player.Client.Account.Admin ||
                   IsSoleContainerOwner(player, c as IContainer);
        }

        private bool IsSoleContainerOwner(Player player, IContainer con)
        {
            int[] owners = null;
            var container = con as Container;
            if (container != null)
                owners = container.BagOwners;

            return owners != null && owners.Length == 1 && owners.Contains(player.AccountId);
        }
    }
}