using common;
using common.resources;

namespace GameServer.networking.packets.outgoing.quests
{
    public class FetchAvailableQuestsResult : OutgoingMessage
    {
        public override Packet CreateInstance() => new FetchAvailableQuestsResult();

        public override PacketId ID => PacketId.FETCH_AVAILABLE_QUESTS_RESULT;

        public QuestData[] Results;
        public string Description;

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Results.ToBytes(false));
            wtr.WriteUTF(Description);
        }
    }
}