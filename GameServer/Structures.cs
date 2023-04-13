using System;
using System.Collections.Generic;
using common;
using common.resources;
using GameServer.realm;

namespace GameServer
{
    public struct MarketData
    {
        public int Id;
        public ItemData ItemData;
        public string SellerName;
        public int SellerId;
        public int Currency;
        public int Price;
        public int StartTime;
        public int TimeLeft;

        public void Write(NWriter wtr)
        {
            wtr.Write(Id);
            wtr.Write(ItemData.Export(false));
            wtr.WriteUTF(SellerName);
            wtr.Write(SellerId);
            wtr.Write(Currency);
            wtr.Write(Price);
            wtr.Write(StartTime);
            wtr.Write(TimeLeft);
        }
    }

    public struct BitmapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Bytes { get; set; }

        public BitmapData Read(NReader rdr)
        {
            var ret = new BitmapData();
            ret.Width = rdr.ReadInt32();
            ret.Height = rdr.ReadInt32();
            ret.Bytes = new byte[ret.Width * ret.Height * 4];
            ret.Bytes = rdr.ReadBytes(ret.Bytes.Length);
            return ret;
        }

        public void Write(NWriter wtr)
        {
            wtr.Write(Width);
            wtr.Write(Height);
            wtr.Write(Bytes);
        }
    }

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
        public ItemData ItemData;
        public int SlotType;
        public bool Tradeable;
        public bool Included;

        public void Write(NWriter wtr)
        {
            wtr.Write(ItemData.Export(false));
            wtr.Write(SlotType);
            wtr.Write(Tradeable);
            wtr.Write(Included);
        }
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

        public static ARGB Read(NReader rdr)
        {
            return new ARGB
            {
                B = rdr.ReadByte(),
                G = rdr.ReadByte(),
                R = rdr.ReadByte(),
                A = rdr.ReadByte()
            };
        }

        public void Write(NWriter wtr)
        {
            wtr.Write(B);
            wtr.Write(G);
            wtr.Write(R);
            wtr.Write(A);
        }
    }

    public struct ObjectSlot
    {
        public int ObjectId;
        public byte SlotId;
        public int ObjectType;

        public static ObjectSlot Read(NReader rdr)
        {
            ObjectSlot ret = new ObjectSlot();
            ret.ObjectId = rdr.ReadInt32();
            ret.SlotId = rdr.ReadByte();
            ret.ObjectType = rdr.ReadInt32();
            return ret;
        }

        public void Write(NWriter wtr)
        {
            wtr.Write(ObjectId);
            wtr.Write(SlotId);
            wtr.Write(ObjectType);
        }

        public override string ToString()
        {
            return string.Format("{{ObjectId: {0}, SlotId: {1}, ObjectType: {2}}}", ObjectId, SlotId, ObjectType);
        }
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

        public static Position Read(NReader rdr)
        {
            var ret = new Position
            {
                X = rdr.ReadSingle(),
                Y = rdr.ReadSingle()
            };
            return ret;
        }

        public void Write(NWriter wtr)
        {
            wtr.Write(X);
            wtr.Write(Y);
        }

        public override string ToString()
        {
            return string.Format("{{X: {0}, Y: {1}}}", X, Y);
        }
    }

    public struct ObjectDef
    {
        public ushort ObjectType;
        public ObjectStats Stats;

        public static ObjectDef Read(NReader rdr)
        {
            ObjectDef ret = new ObjectDef();
            ret.ObjectType = rdr.ReadUInt16();
            ret.Stats = ObjectStats.Read(rdr);
            return ret;
        }

        public void Write(NWriter wtr)
        {
            wtr.Write(ObjectType);
            Stats.Write(wtr);
        }
    }

    public struct ObjectStats
    {
        public int Id;
        public Position Position;
        public KeyValuePair<StatsType, object>[] Stats;
        public int DamageDealt;

        public static ObjectStats Read(NReader rdr)
        {
            ObjectStats ret = new ObjectStats();
            ret.Id = rdr.ReadInt32();
            ret.Position = Position.Read(rdr);
            ret.Stats = new KeyValuePair<StatsType, object>[rdr.ReadInt16()];
            for (var i = 0; i < ret.Stats.Length; i++)
            {
                StatsType type = (StatsType)rdr.ReadByte();
                if (type == StatsType.Guild || type == StatsType.Name)
                    ret.Stats[i] = new KeyValuePair<StatsType, object>(type, rdr.ReadUTF());
                else
                    ret.Stats[i] = new KeyValuePair<StatsType, object>(type, rdr.ReadInt32());
            }

            ret.DamageDealt = rdr.ReadInt32();

            return ret;
        }

        public void Write(NWriter wtr)
        {
            wtr.Write(Id);
            Position.Write(wtr);

            wtr.Write((short)Stats.Length);
            foreach (var i in Stats)
            {
                // stat type
                wtr.Write((byte)i.Key);

                // int array export
                if (i.Value is int[])
                {
                    var val = (int[])i.Value;
                    wtr.Write((short)val.Length);
                    for (var j = 0; j < val.Length; j++)
                    {
                        wtr.Write(val[j]);
                    }
                    continue;
                }

                // byte array exports
                if (i.Value is PetData)
                {
                    var val = ((PetData)i.Value).Export();
                    wtr.Write((short)val.Length);
                    wtr.Write(val);
                    continue;
                }

                // inventory export
                if (i.Value is ItemData[])
                {
                    wtr.Write(((ItemData[])i.Value).ToBytes(false));
                    continue;
                }

                // int exports
                if (i.Value is int)
                {
                    wtr.Write((int)i.Value);
                    continue;
                }

                if (i.Value is bool)
                {
                    wtr.Write((bool)i.Value ? 1 : 0);
                    continue;
                }

                if (i.Value is ushort)
                {
                    wtr.Write((int)(ushort)i.Value);
                    continue;
                }

                // string export
                if (i.Value is string)
                {
                    wtr.WriteUTF(i.Value as string);
                    continue;
                }

                throw new InvalidOperationException(
                    $"Stat '{i.Key}' of type '{i.Value?.GetType().ToString() ?? "null"}' not supported.");
            }

            wtr.Write(DamageDealt);
        }
    }
}