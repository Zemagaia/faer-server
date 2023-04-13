using common;

namespace GameServer.networking.packets.incoming
{
    public abstract class IncomingMessage : Packet
    {
        protected override void Write(NWriter wtr)
        {
        }
    }
}