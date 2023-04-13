using common;
using common.resources;

namespace GameServer.networking.packets.outgoing.quests
{
    public class FetchAccountQuestsResult : OutgoingMessage
    {
        public override Packet CreateInstance() => new FetchAccountQuestsResult();

        public override PacketId ID => PacketId.FETCH_ACCOUNT_QUESTS_RESULT;

        public AcceptedQuestData[] Results;
        public string Description;

        protected override void Write(NWriter wtr)
        {
            wtr.Write(Results.ToBytes(false));
            wtr.WriteUTF(Description);
        }
    }
}