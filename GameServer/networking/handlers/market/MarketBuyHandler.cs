using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.market;
using GameServer.networking.packets.outgoing.market;

namespace GameServer.networking.handlers.market
{
    class MarketBuyHandler : PacketHandlerBase<MarketBuy>
    {
        public override PacketId ID => PacketId.MARKET_BUY;

        protected override void HandlePacket(Client client, MarketBuy packet)
        {
            client.Manager.Logic.AddPendingAction(t =>
            {
                var acc = client.Account;
                if (client.Player == null || IsTest(client) ||
                    (client.Player.Rank > 50 && client.Player.Rank < 100) // Same here...
                    || !client.Manager.Config.serverSettings.enableMarket)
                {
                    return;
                }

                DbMarketData data = client.Manager.Database.GetMarketData(packet.Id);
                if (data == null) /* Make sure the item exist before buying it */
                {
                    client.SendPacket(new MarketBuyResult
                    {
                        Code = MarketBuyResult.ITEM_DOESNT_EXIST,
                        Description = "Item was taken down or bought."
                    });
                    return;
                }

                if (data.SellerId == client.Player.AccountId) /* If we somehow try to buy our own item */
                {
                    client.SendPacket(new MarketBuyResult
                    {
                        Code = MarketBuyResult.MY_ITEM,
                        Description = "You cannot buy your own item."
                    });
                    return;
                }

                int currencyAmount = data.Currency == CurrencyType.Fame ? acc.Fame : acc.Credits;
                if (currencyAmount < data.Price) /* Make sure we have enough to buy the item */
                {
                    client.SendPacket(new MarketBuyResult
                    {
                        Code = MarketBuyResult.CANT_AFFORD,
                        Description = "You cannot afford this item."
                    });
                    return;
                }

                string currency = data.Currency == CurrencyType.Fame ? "fame" : "gold";
                var item = data.ItemData;

                /* Update the sellers currency */
                var sellerAccount = client.Manager.Database.GetAccount(data.SellerId);
                client.Manager.Database.UpdateCurrency(sellerAccount, data.Price, data.Currency).ContinueWith(r =>
                {
                    /* Incase he is online, we let him know someone bought his item */
                    var seller = client.Manager.Clients.Keys.SingleOrDefault(_ =>
                        _.Account != null && _.Account.AccountId == data.SellerId);
                    if (seller != null)
                    {
                        seller.Player.SendInfo(
                            $"{client.Player.Name} has just bought your {item.Item.ObjectId} for {data.Price} {currency}!");

                        Log.Warn(
                            $"{client.Player.Name} bought a {item.Item.ObjectId} for {data.Price} {currency} from {seller.Player.Name}");

                        /* Dynamically update his currency if he's online */
                        seller.Player.CurrentFame = sellerAccount.Fame;
                        seller.Player.Credits = sellerAccount.Credits;
                    }

                    client.Manager.Database.ReloadAccount(sellerAccount);
                });
                client.Manager.Database.RemoveMarketData(sellerAccount, data.Id);


                /* Update the buyers currency */
                client.Manager.Database.UpdateCurrency(acc, -data.Price, data.Currency).ContinueWith(_ =>
                {
                    client.Player.CurrentFame = acc.Fame;
                    client.Player.Credits = acc.Credits;
                    client.Manager.Database.ReloadAccount(acc);
                });
                client.Manager.Database.AddGift(acc, item);

                client.SendPacket(new MarketBuyResult
                {
                    Code = -1,
                    Description = $"Successfully bought {item.Item.ObjectId} for {data.Price} {currency}!",
                    OfferId = data.Id /* We send back the ID we bought, so we can remove it from the list */
                });
            });
        }
    }
}