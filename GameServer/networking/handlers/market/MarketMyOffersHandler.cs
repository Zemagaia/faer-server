using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.market;
using GameServer.networking.packets.outgoing.market;

namespace GameServer.networking.handlers.market
{
    class MarketMyOffersHandler : PacketHandlerBase<MarketMyOffers>
    {
        public override PacketId ID => PacketId.MARKET_MY_OFFERS;

        protected override void HandlePacket(Client client, MarketMyOffers packet)
        {
            client.Manager.Logic.AddPendingAction(t =>
            {
                var player = client.Player;
                if (player == null || IsTest(client) || !client.Manager.Config.serverSettings.enableMarket)
                {
                    return;
                }

                List<MarketData> myOffers = new List<MarketData>();
                for (var i = 0; i < client.Account.MarketOffers.Length; i++)
                {
                    DbMarketData result = player.Manager.Database.GetMarketData(client.Account.MarketOffers[i]);
                    if (result == null) /* This will only happend if someone bought our item */
                    {
                        continue;
                    }

                    myOffers.Add(new MarketData
                    {
                        Id = result.Id,
                        ItemData = result.ItemData,
                        SellerName = result.SellerName,
                        SellerId = result.SellerId,
                        Currency = (int)result.Currency,
                        Price = result.Price,
                        StartTime = result.StartTime,
                        TimeLeft = result.TimeLeft
                    });
                }

                client.SendPacket(new MarketMyOffersResult
                {
                    Results = myOffers.ToArray()
                });
            });
        }
    }
}