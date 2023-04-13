using common;

namespace GameServer.networking.packets.outgoing
{
    public abstract class OutgoingMessage : Packet
    {
        protected override void Read(NReader rdr)
        {
        }
    }
}