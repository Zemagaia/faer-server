namespace GameServer.realm.entities
{
    class Placeholder : StaticObject
    {
        public Placeholder(RealmManager manager, int life)
            : base(manager, 0x0456, life, true, true, false)
        {
            SetDefaultSize(0);
        }
    }
}