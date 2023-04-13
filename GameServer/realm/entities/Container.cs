using System.Xml.Linq;
using common;
using common.resources;

namespace GameServer.realm.entities
{
    public class Container : StaticObject, IContainer
    {

        public Container(RealmManager manager, ushort objType, int? life, bool dying, RInventory dbLink = null)
            : base(manager, objType, life, false, dying, false)
        {
            Initialize(dbLink);
        }

        public Container(RealmManager manager, ushort id)
            : base(manager, id, null, false, false, false)
        {
            Initialize(null);
        }

        private void Initialize(RInventory dbLink)
        {
            if (dbLink != null)
                Inventory = new Inventory(this, dbLink.Items);
            else
                Inventory = new Inventory(this, new ItemData[0]);

            var uiids = new HashSet<ulong>();
            for (var i = 0; i < Inventory.Length; i++)
            {
                var inventory = Inventory;
                var uiid = inventory[i].UIID;
                if (uiid == 0) continue;
                if (uiids.Add(uiid)) continue;

                Log.Error(
                    $"Container had duplicate UIID ({uiid}) on inventory slot {i}...");
                inventory[i] = new ItemData();
            }

            BagOwners = new int[0];
            DbLink = dbLink;

            var node = Manager.Resources.GameData.ObjectTypeToElement[ObjectType];
            SlotTypes = Utils.ResizeArray(node.Element("SlotTypes").Value.CommaToArray<int>(), Inventory.Length);
            XElement eq = node.Element("Equipment");
            if (eq != null)
            {
                var inv = eq.Value.CommaToArray<ushort>().ToArray();
                Array.Resize(ref inv, Inventory.Length);
                var itemDatas = new ItemData[inv.Length];
                for (var i = 0; i < itemDatas.Length; i++)
                    itemDatas[i] = inv[i] == ushort.MaxValue ? new ItemData() : ItemData.GenerateData(inv[i]);
                Inventory.SetItems(itemDatas);
            }
        }

        public RInventory DbLink { get; private set; }
        public int[] SlotTypes { get; private set; }
        public Inventory Inventory { get; private set; }
        public int[] BagOwners { get; set; }

        protected override void ImportStats(StatsType stats, object val)
        {
            if (Inventory == null) return;

            switch (stats)
            {
                case StatsType.Inventory:
                    var items = (ItemData[])val;
                    for (var i = 0; i < items.Length; i++)
                        Inventory[i] = items[i];
                    break;
                case StatsType.OwnerAccountId: break; // BagOwner = (int)val == -1 ? (int?)null : (int)val; break;
            }

            base.ImportStats(stats, val);
        }

        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            if (Inventory == null) return;

            stats[StatsType.Inventory] = Inventory.GetItems();
            stats[StatsType.OwnerAccountId] = (BagOwners.Length == 1 ? BagOwners[0] : -1).ToString();
            base.ExportStats(stats);
        }

        public override void Tick(RealmTime time)
        {
            if (Inventory == null)
                return;

            if (ObjectType == 0x0403) //Vault chest
                return;
            
            if (Inventory.Count(i => i.ObjectType != ushort.MaxValue) == 0)
                Owner.LeaveWorld(this);

            base.Tick(time);
        }

        public override bool HitByProjectile(Projectile projectile, RealmTime time)
        {
            return false;
        }
    }
}