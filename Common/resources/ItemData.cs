using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace common.resources
{
    public class ItemData
    {
        public Item Item;
        [Description("-2")] public ushort ObjectType = ushort.MaxValue;
        [Description("Server 0")] public ulong UIID;
        [Description("1")] public bool Soulbound;
        [Description("2")] public float Quality;
        [Description("3")] public int Quantity;
        [Description("4")] public int MaxQuantity;
        [Description("5")] public ushort[] Runes;
        [Description("6")] public int[] DamageBoosts;
        [Description("7")] public KeyValuePair<byte, short>[] StatBoosts;
        [Description("8")] public string TexFile;
        [Description("9")] public int TexIndex;
        [Description("10")] public string MaskFile;
        [Description("11")] public int MaskIndex;
        [Description("12")] public int Tex1;
        [Description("13")] public int Tex2;
        
        /*
         Server 0 - Unique Item Identifier (server only data)
         6 - When set this has 8 length, from 0 to 7 (from physical to holy)
         12 - Mask clothing color
         13 - Mask accessory color
        */

        public static ItemData GenerateData(ushort objType)
        {
            Database.Resources.GameData.Items.TryGetValue(objType, out var item);
            return GenerateData(item);
        }

        public static ItemData GenerateData(Item item)
        {
            return new ItemData()
            {
                Item = item,
                ObjectType = item.ObjectType,
                UIID = MakeUIID(item.ObjectType),
            };
        }

        public static ulong MakeUIID(ushort objType)
        {
            var sb = new StringBuilder();
            sb.Append(objType);
            sb.Append(MathUtils.Next(100000000));
            sb.Append(DateTime.UtcNow.ToString("yyMMdd"));
            return ulong.Parse(sb.ToString());
        }

        public static float MakeQuality(Item item)
        {
            return MathUtils.Next((int)(item.MinQuality * 100), (int)(item.MaxQuality * 100)) / 100f;
        }

        public static ushort[] GetRuneSlots(float quality)
        {
            return quality switch
            {
                < 0.85f => new ushort[1],
                < 0.90f => new ushort[2],
                < 0.95f => new ushort[3],
                < 1.00f => new ushort[4],
                < 1.05f => new ushort[5],
                < 1.10f => new ushort[6],
                < 1.15f => new ushort[7],
                >= 1.15f => new ushort[8],
                _ => new ushort[1]
            };
        }

        public ItemData() {}
        
        public ItemData(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var rdr = new NReader(ms))
            {
                var key = rdr.ReadUInt32();
                var key2 = rdr.ReadUInt32();
                Utils.ByteArrayImport(this, key, rdr, 0);
                Database.Resources.GameData.Items.TryGetValue(ObjectType, out Item);
            }
        }

        public byte[] Export(bool server)
        {
            return Utils.ByteArrayExport(this, server);
        }
    }
}