using System.ComponentModel;
using System.IO;

namespace common.resources
{
    public class QuestData
    {
        [Description("0")] public int Id;
        [Description("1")] public int AddTime;
        [Description("2")] public int EndTime;
        [Description("3")] public byte Icon;
        [Description("4")] public string Title;
        [Description("5")] public string Description;
        [Description("6")] public ushort[] Rewards;
        [Description("7")] public ushort[] Slay;
        [Description("8")] public int[] SlayAmounts;
        [Description("9")] public string[] Dungeons;
        [Description("10")] public int[] DungeonAmounts;
        [Description("11")] public int Experience;
        [Description("12")] public int ExpReward;
        [Description("13")] public ushort[] Deliver;
        [Description("14")] public ItemData[] DeliverDatas;
        [Description("15")] public string Scout;

        public QuestData() { }

        public QuestData(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var rdr = new NReader(stream))
            {
                var key = rdr.ReadUInt32();
                var key2 = rdr.ReadUInt32();
                Utils.ByteArrayImport(this, key, rdr, 0);
            }
        }

        public virtual byte[] Export(bool server)
        {
            return Utils.ByteArrayExport(this, server);
        }
    }

    public class AcceptedQuestData : QuestData
    {
        [Description("16")] public int[] SlainAmounts;
        [Description("17")] public ushort[] DungeonsCompleted;
        [Description("18")] public int ExpGained;
        [Description("19")] public bool[] Delivered;
        [Description("20")] public bool Scouted;
        [Description("21")] public bool DailyQuest;
        [Description("Server 31")] public byte[] Goals;
        
        public AcceptedQuestData() { }
        
        public AcceptedQuestData(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var rdr = new NReader(stream))
            {
                var key = rdr.ReadUInt32();
                var key2 = rdr.ReadUInt32();
                Utils.ByteArrayImport(this, key, rdr, 0);
            }
        }
        
        public override byte[] Export(bool server)
        {
            return Utils.ByteArrayExport(this, server);
        }
    }
}