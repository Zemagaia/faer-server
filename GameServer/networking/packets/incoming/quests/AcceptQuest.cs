using common;

namespace GameServer.networking.packets.incoming.quests
{
    public class AcceptQuest : IncomingMessage
    {
        public int Id { get; set; }
        public int Type { get; set; }

        public const int Accept = 0;
        public const int Dismiss = 1;
        public const int Delete = 2;
        public const int Deliver = 3;
        public const int Scout = 4;
        public const int Delete_Account = 5;
        public const int Deliver_Account = 6;
        public const int Scout_Account = 7;

        public override PacketId ID => PacketId.ACCEPT_QUEST;

        public override Packet CreateInstance()
        {
            return new AcceptQuest();
        }

        protected override void Read(NReader rdr)
        {
            Id = rdr.ReadInt32();
            Type = rdr.ReadInt32();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Id);
            wtr.Write(Type);
        }
    }
}