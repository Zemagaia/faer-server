using common;

namespace GameServer.realm.entities
{
    class GiftChest : OneWayContainer
    {
        public int AssignedGiftId;
        public GiftChest(RealmManager manager, ushort objType, int? life, bool dying, RInventory dbLink = null)
            : base(manager, objType, life, dying, dbLink)
        {
        }

        public GiftChest(RealmManager manager, ushort id)
            : base(manager, id)
        {
        }
    }
}