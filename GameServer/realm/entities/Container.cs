using System.Xml.Linq;
using common;
using common.resources;
using wServer.realm;

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
                Inventory = new Inventory(this, new Item[0]);

            BagOwners = new int[0];
            DbLink = dbLink;

            var node = Manager.Resources.GameData.ObjectTypeToElement[ObjectType];
            SlotTypes = Utils.ResizeArray(node.Element("SlotTypes").Value.CommaToArray<int>(), Inventory.Length);
            XElement eq = node.Element("Equipment");
            if (eq != null)
            {
                var inv = eq.Value.CommaToArray<ushort>().ToArray();
                Array.Resize(ref inv, Inventory.Length);
                var items = new Item[inv.Length];
                for (var i = 0; i < items.Length; i++)
                    items[i] = inv[i] == ushort.MaxValue ? null : Manager.Resources.GameData.Items[inv[i]];
                Inventory.SetItems(items);
            }
        }

        public RInventory DbLink { get; private set; }
        public int[] SlotTypes { get; private set; }
        public Inventory Inventory { get; private set; }
        public int[] BagOwners { get; set; }

        protected override void ImportStats(StatsType stats, object val)
        {
            if (Inventory == null) return;

            // todo: inventory work
            switch (stats)
            {
                case StatsType.Inventory:
                    var items = (Item[])val;
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

            // todo: inventory work
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