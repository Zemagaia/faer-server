using common.resources;
using GameServer.networking.packets.outgoing;
using GameServer.realm.entities.player;
using StackExchange.Redis;
using wServer.realm;

namespace GameServer.realm.entities.vendors
{
    public abstract class Merchant : SellableObject
    {
        private readonly SV<ushort> _item;
        private readonly SV<int> _count;
        private readonly SV<int> _timeLeft;

        public ushort Item
        {
            get { return _item.GetValue(); }
            set { _item.SetValue(value); }
        }

        public int Count
        {
            get { return _count.GetValue(); }
            set { _count.SetValue(value); }
        }

        public int TimeLeft
        {
            get { return _timeLeft.GetValue(); }
            set { _timeLeft.SetValue(value); }
        }

        public int ReloadOffset { get; set; }
        public bool Rotate { get; set; }

        protected volatile bool BeingPurchased;
        protected volatile bool AwaitingReload;
        protected volatile bool Reloading;

        protected Merchant(RealmManager manager, ushort objType)
            : base(manager, objType)
        {
            _item = new SV<ushort>(this, StatsType.MerchantMerchandiseType, 0x1400);
            _count = new SV<int>(this, StatsType.MerchantRemainingCount, -1);
            _timeLeft = new SV<int>(this, StatsType.MerchantRemainingMinute, -1);
            Rotate = true;
        }

        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            stats[StatsType.MerchantMerchandiseType] = (int)Item;
            stats[StatsType.MerchantRemainingCount] = Count;
            stats[StatsType.MerchantRemainingMinute] = -1; //(int)(TimeLeft / 60000f);
            base.ExportStats(stats);
        }

        protected override void ImportStats(StatsType stats, object val)
        {
            switch (stats)
            {
                case StatsType.MerchantMerchandiseType:
                    Item = (ushort)val;
                    break;
                case StatsType.MerchantRemainingCount:
                    Count = (int)val;
                    break;
                case StatsType.MerchantRemainingMinute:
                    TimeLeft = (int)val;
                    break;
            }

            base.ImportStats(stats, val);
        }

        /*public override void Tick(RealmTime time)
        {
            base.Tick(time);

            if (TimeLeft == -1)
                return;
            
            TimeLeft = Math.Max(0, TimeLeft - time.ElaspedMsDelta);

            if (this.AnyPlayerNearby(2))
                return;

            if (AwaitingReload || TimeLeft <= 0)
            {
                if (BeingPurchased)
                {
                    AwaitingReload = true;
                    return;
                }
                BeingPurchased = true;

                Reload();
                BeingPurchased = false;
                AwaitingReload = false;
            }
        }*/

        public override void Tick(RealmTime time)
        {
            base.Tick(time);

            var a = time.TotalElapsedMs % 20000;
            if (AwaitingReload ||
                a - time.ElapsedMsDelta <= ReloadOffset && a > ReloadOffset)
            {
                if (!AwaitingReload && !Rotate)
                    return;

                if (this.AnyPlayerNearby(2))
                {
                    AwaitingReload = true;
                    return;
                }

                if (BeingPurchased)
                {
                    AwaitingReload = true;
                    return;
                }

                BeingPurchased = true;

                TimeLeft = -1; // needed for player merchant to function properly with new rotation method
                Reload();
                BeingPurchased = false;
                AwaitingReload = false;
            }
        }

        public virtual void Reload()
        {
        }

        public override void Buy(Player player)
        {
            if (BeingPurchased)
            {
                SendFailed(player, BuyResult.BeingPurchased);
                return;
            }

            BeingPurchased = true;

            var result = ValidateCustomer(player);
            if (result != BuyResult.Ok)
            {
                SendFailed(player, result);
                BeingPurchased = false;
                return;
            }

            PurchaseItem(player);
        }

        private async void PurchaseItem(Player player)
        {
            var db = Manager.Database;
            var trans = db.Conn.CreateTransaction();
            var t1 = db.UpdateCurrency(player.Client.Account, -Price, Currency, trans);
            db.AddToTreasury(Tax, trans);
            var invTrans = TransactionItem(player, trans, out var slot);
            var t2 = trans.ExecuteAsync();
            await Task.WhenAll(t1, t2);

            var success = !t2.IsCanceled && t2.Result;
            TransactionItemComplete(player, invTrans, success, slot);
            if (success && Count != -1 && --Count <= 0)
            {
                Reload();
                AwaitingReload = false;
            }

            BeingPurchased = false;
        }

        private bool _isInvFull;

        protected InventoryTransaction TransactionItem(Player player, ITransaction tran, out int s)
        {
            var invTrans = player.Inventory.CreateTransaction();
            var item = Manager.Resources.GameData.Items[Item];
            var slot = player.Inventory.GetAvailableInventorySlot(item);
            s = slot;
            if (slot == -1)
            {
                player.Manager.Database.AddGift(player.Client.Account, item, tran);
                _isInvFull = true;
                return null;
            }

            invTrans[slot] = item;
            _isInvFull = false;
            return invTrans;
        }

        protected void TransactionItemComplete(Player player, InventoryTransaction invTrans, bool success, int slot)
        {
            if (!success)
            {
                SendFailed(player, BuyResult.TransactionFailed);
                return;
            }

            // update player currency values
            var acc = player.Client.Account;
            player.Credits = acc.Credits;
            player.CurrentFame = acc.Fame;

            // if the item is put in your inv, execute changes immediately
            if (invTrans != null)
            {
                Inventory.Execute(invTrans);
                player.ForceUpdate(slot);
            }

            SendNotifications(player, invTrans == null);
        }

        protected virtual void SendNotifications(Player player, bool gift)
        {
            var item = Manager.Resources.GameData.Items[Item];

            if (_isInvFull)
            {
                player.Client.SendBuyResult(0, $"Your inventory is full, and your {item.DisplayName} has been sent to a gift chest.");
            }
            else
                player.Client.SendBuyResult(0, $"Your purchase was successful.");
            
            Log.Info("[{0}]User {1} has bought {2} for {3} {4}.",
                DateTime.Now, player.Name, item.DisplayName, Price, Currency.ToString());
        }
    }
}