using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.forge;
using GameServer.networking.packets.outgoing.forge;
using GameServer.realm;
using GameServer.realm.entities.player;

namespace GameServer.networking.handlers.forge
{
    class CraftItemHandler : PacketHandlerBase<CraftItem>
    {
        public override PacketId ID => PacketId.CRAFT_ITEM;

        protected override void HandlePacket(Client client, CraftItem packet)
        {
            client.Manager.Logic.AddPendingAction(_ => { Handle(client, packet); });
        }

        private void Handle(Client client, CraftItem packet)
        {
            var player = client.Player;
            if (player == null || IsTest(client) || packet.Slots.Length > 8)
                return;

            ItemData item;
            int i;
            var gameData = player.Manager.Resources.GameData;
            var inventory = player.Inventory;
            switch (packet.Id)
            {
                case 0:
                    var objTypes = new int[packet.Slots.Length];
                    var objIds = new string[packet.Slots.Length];
                    for (i = 0; i < objTypes.Length; i++)
                    {
                        objTypes[i] = inventory[packet.Slots[i]].ObjectType;
                        objIds[i] = gameData.ObjectTypeToId[(ushort)objTypes[i]];
                    }

                    var items = ForgeRecipes.GetSorted(objIds);

                    if (!gameData.Forge.Recipes.ContainsKey(items))
                    {
                        player.Client.SendPacket(new CraftAnimation()
                        {
                            ObjectType = -1,
                            Active = new int[0],
                        });
                        break;
                    }

                    // give out crafted item
                    if (gameData.Forge.Recipes.TryGetValue(items, out var objectId))
                    {
                        for (i = 0; i < packet.Slots.Length; i++)
                            inventory[packet.Slots[i]] = new ItemData();

                        GiveCraftedItem(gameData, player, objTypes, objectId);
                    }

                    // if player gets here it means they have already crafted
                    // and the items don't exist anymore
                    break;
                // For inserting a rune on an item.
                // Requires for packet.ItemSlot and packet.RuneSlot to be a value above 3 and below 20
                case 1:
                    // do a lot of checks to make sure we don't mess up here
                    if (packet.ItemSlot < 6 || packet.ItemSlot > player.Inventory.Length - 1)
                    {
                        player.SendError("Invalid item slot.");
                        return;
                    }

                    if (packet.RuneSlot < 6 || packet.RuneSlot > player.Inventory.Length - 1)
                    {
                        player.SendError("Invalid rune slot.");
                        return;
                    }

                    item = inventory[packet.ItemSlot];
                    if (item is null)
                    {
                        player.SendError("Target item not found.");
                        return;
                    }

                    var rune = inventory[packet.RuneSlot];
                    if (rune is null)
                    {
                        player.SendError("Item in rune slot not found.");
                        return;
                    }

                    if (!rune.Item.Rune)
                    {
                        player.SendError("Select a rune to insert on your item.");
                        return;
                    }

                    var itemData = inventory[packet.ItemSlot];
                    var runes = itemData.Runes;
                    // this shouldn't happen, so...
                    if (runes is null)
                    {
                        player.SendError("Error, please contact staff to resolve this issue.");
                        return;
                    }

                    // finally try to insert rune on first empty slot
                    for (i = 0; i < runes.Length; i++)
                    {
                        // 0 is empty rune slot
                        if (runes[i] != 0)
                        {
                            continue;
                        }

                        // set rune and add boosts
                        runes[i] = rune.ObjectType;
                        player.AddRuneBoost(rune.ObjectType, packet.ItemSlot);
                        // clear rune slot
                        inventory[packet.RuneSlot] = new ItemData();
                        // update runes on item slot and make item soulbound
                        inventory[packet.ItemSlot].Runes = runes;
                        inventory[packet.ItemSlot].Soulbound = true;
                        player.ForceUpdate(packet.ItemSlot);
                        player.SendInfo($"Successfully inserted \"{rune.Item.ObjectId}\" into \"{item.Item.ObjectId}\"");
                        return;
                    }

                    player.SendError($"You cannot insert any more runes into \"{item.Item.ObjectId}\"");
                    break;
            }
        }

        private void GiveCraftedItem(XmlData gameData, Player player, int[] selectedItems, string id)
        {
            var objType = gameData.IdToObjectType[id];
            var item = gameData.Items[objType];
            var inventory = player.Inventory;
            var slot = inventory.GetAvailableInventorySlot(item);

            inventory[slot] = ItemData.GenerateData(item);
            if (item.SlotType != 10 && item.SlotType != 26)
            {
                var quality = ItemData.MakeQuality(item);
                inventory[slot].Quality = quality;
                inventory[slot].Runes = ItemData.GetRuneSlots(quality);
            }

            player.ForceUpdate(slot);
            player.Client.SendPacket(new CraftAnimation()
            {
                ObjectType = item.ObjectType,
                Active = selectedItems
            });
        }
    }
}