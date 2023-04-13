using common;
using common.resources;
using GameServer.realm.entities.player;

namespace GameServer.realm
{
    public interface IContainer
    {
        int[] SlotTypes { get; }
        Inventory Inventory { get; }
        RInventory DbLink { get; }
    }

    public class InventoryChangedEventArgs : EventArgs
    {
        //index = -1 -> reset
        public InventoryChangedEventArgs(ItemData[] old, ItemData[] @new)
        {
            OldItems = old;
            NewItems = @new;
        }

        public ItemData[] OldItems { get; private set; }
        public ItemData[] NewItems { get; private set; }
    }

    public class InventoryTransaction : IEnumerable<ItemData>
    {
        private readonly IContainer _parent;
        private readonly ItemData[] _originalItems;
        private readonly ItemData[] _changedItems;

        public int Length => _originalItems.Length;

        public InventoryTransaction(IContainer parent)
        {
            _parent = parent;
            _originalItems = parent.Inventory.GetItems();
            _changedItems = (ItemData[])_originalItems.Clone();
        }

        public bool Validate(bool revert = false)
        {
            if (_parent == null)
                return false;

            var items = revert ? _changedItems : _originalItems;

            for (var i = 0; i < items.Length; i++)
                if (items[i] != _parent.Inventory[i])
                    return false;

            return true;
        }

        public void Execute()
        {
            var inv = _parent.Inventory;
            for (var i = 0; i < inv.Length; i++)
                if (_originalItems[i] != _changedItems[i])
                    inv[i] = _changedItems[i];
        }

        public void Revert()
        {
            var inv = _parent.Inventory;
            for (var i = 0; i < inv.Length; i++)
                if (_originalItems[i] != _changedItems[i])
                    inv[i] = _originalItems[i];
        }

        public IEnumerator<ItemData> GetEnumerator()
        {
            return ((IEnumerable<ItemData>)_changedItems).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _changedItems.GetEnumerator();
        }

        public ItemData this[int index]
        {
            get => _changedItems[index];
            set => _changedItems[index] = value;
        }
    }

    public class InventoryItems
    {
        private readonly SV<ItemData[]> _itemTypes;
        private ItemData[] _items;

        public int Length => _items.Length;

        public InventoryItems(IContainer container, ItemData[] items)
        {
            _itemTypes = new SV<ItemData[]>(container as Entity, StatsType.Inventory, items);
            _items = items;
        }

        public void SetItems(ItemData[] items)
        {
            if (items.Length > Length && Length > 0)
                throw new InvalidOperationException("Item array must be <= the size of the initialized array.");

            _itemTypes.SetValue(items);
            _items = items;
        }

        public ItemData[] GetItems()
        {
            return (ItemData[])_items.Clone();
        }

        public ItemData this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
                _itemTypes.SetValue(_items);
            }
        }
    }

    public class Inventory : IEnumerable<ItemData>
    {
        private readonly object _invLock = new();
        private readonly IContainer _parent;

        private readonly InventoryItems _items;

        public event EventHandler<InventoryChangedEventArgs> InventoryChanged;

        public IContainer Parent => _parent;
        public int Length => _items.Length;

        public Inventory(IContainer parent)
            : this(parent, new ItemData[Program.Resources.Settings.InventorySize])
        {
        }

        public Inventory(IContainer parent, ItemData[] items)
        {
            _parent = parent;
            _items = new InventoryItems(parent, items);
        }

        public void SetItems(ItemData[] items)
        {
            lock (_invLock)
            {
                var oItems = _items.GetItems();
                _items.SetItems(items);
                InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(oItems, _items.GetItems()));
            }
        }

        public void SetItems(IEnumerable<ItemData> items)
        {
            lock (_invLock)
            {
                var oItems = _items.GetItems();
                _items.SetItems(ConvertToItemArray(items));
                InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(oItems, _items.GetItems()));
            }
        }

        public ItemData[] GetItems()
        {
            lock (TrySaveLock)
            {
                lock (_invLock)
                {
                    return _items.GetItems();
                }
            }
        }

        public ushort[] GetItemTypes()
        {
            lock (_invLock)
            {
                return _items.GetItems().Select(_ => _?.ObjectType ?? 0xffff).ToArray();
            }
        }

        public ItemData[] GetItemDatas()
        {
            lock (_invLock)
            {
                return _items.GetItems().Select(_ => _ ?? new ItemData()).ToArray();
            }
        }

        public ItemData this[int index]
        {
            get
            {
                lock (_invLock)
                {
                    return _items[index];
                }
            }
            set
            {
                lock (_invLock)
                {
                    if (_items[index] != value)
                    {
                        var oItems = _items.GetItems();
                        _items[index] = value;
                        InventoryChanged?.Invoke(this, new InventoryChangedEventArgs(oItems, _items.GetItems()));
                    }
                }
            }
        }

        public InventoryTransaction CreateTransaction()
        {
            return new InventoryTransaction(Parent);
        }

        private static readonly object TrySaveLock = new();

        public static bool Execute(params InventoryTransaction[] transactions)
        {
            lock (TrySaveLock)
            {
                if (transactions.Any(transaction => !transaction.Validate()))
                    return false;

                foreach (var transaction in transactions)
                    transaction.Execute();

                return true;
            }
        }

        public static bool Revert(params InventoryTransaction[] transactions)
        {
            lock (TrySaveLock)
            {
                if (transactions.Any(transaction => !transaction.Validate(true)))
                    return false;

                foreach (var transaction in transactions)
                    transaction.Revert();
                return true;
            }
        }

        public int GetAvailableInventorySlot(Item item)
        {
            lock (_invLock)
            {
                var plr = _parent as Player;
                if (plr != null)
                {
                    var playerDesc = plr.Manager.Resources.GameData
                        .Classes[plr.ObjectDesc.ObjectType];
                    for (var i = 0; i < 6; i++)
                        if (_items[i].Item == null && playerDesc.SlotTypes[i] == item.SlotType)
                            return i;

                    for (var i = 6; i < 18 || (plr.HasBackpack && i < plr.Inventory.Length); i++)
                        if (_items[i].Item == null)
                            return i;
                }
                else
                {
                    for (var i = 0; i < _parent.Inventory.Length; i++)
                        if (_items[i].Item == null)
                            return i;
                }

                return -1;
            }
        }

        private static ItemData[] ConvertToItemArray(IEnumerable<ItemData> a)
        {
            return a.Select(_ => _ == null ? new ItemData() : _).ToArray();
        }

        public IEnumerator<ItemData> GetEnumerator()
        {
            return ((IEnumerable<ItemData>)_items.GetItems()).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetItems().GetEnumerator();
        }
    }
}