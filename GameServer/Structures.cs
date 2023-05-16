using GameServer.realm;

namespace GameServer; 

public struct IntPoint : IEquatable<IntPoint>
{
    public int X;
    public int Y;
    public int Type;
    public int Generation;
    public bool Blocking;

    public IntPoint(int x, int y, int type = 8)
    {
        X = x;
        Y = y;
        Type = 8;
        Generation = 0;
        Blocking = false;
    }

    public bool Equals(IntPoint other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        if (obj is IntPoint)
        {
            var p = (IntPoint)obj;
            return Equals(p);
        }

        return false;
    }

    public override int GetHashCode()
    {
        /*unchecked
        {
            int hash = 17;
            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();
            return hash;
        }*/
        return 31 * X + 17 * Y; // could be problem if value is changed...
    }
}

public struct TradeItem
{
    public int Item;
    public int SlotType;
    public bool Tradeable;
    public bool Included;
}

public struct ARGB
{
    public ARGB(uint argb)
    {
        A = (byte)((argb & 0xff000000) >> 24);
        R = (byte)((argb & 0x00ff0000) >> 16);
        G = (byte)((argb & 0x0000ff00) >> 8);
        B = (byte)((argb & 0x000000ff) >> 0);
    }

    public byte A;
    public byte R;
    public byte G;
    public byte B;
}

public struct TimedPosition
{
    public int Time;
    public float X;
    public float Y;
}

public struct Position
{
    public float X;
    public float Y;
}

public struct ObjectDef
{
    public ushort ObjectType;
    public ObjectStats Stats;
}

public struct NewTickObjectDef {
    public ClassType ClassType;
    public ObjectStats Stats;
}

public enum ClassType : byte {
    GameObject = 0,
    Character = 1,
    Player = 2,
    Container = 3,
    Wall = 4,
    Portal = 5,
    Merchant = 6,
    ClosedStashChest = 7,
    FoundingTable = 8,
    GuildManager = 9,
    GuildHallPortal = 10,
    SkinChanger = 11,
    Bazaar = 12,
    Projectile = 13,
}

public struct ObjectStats {
    public int Id;
    public float X;
    public float Y;
    public KeyValuePair<StatsType, object>[] StatTypes;
}

public struct TileData {
    public short X;
    public short Y;
    public ushort Tile;
}