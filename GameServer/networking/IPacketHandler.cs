using GameServer.networking.packets;
using GameServer.networking.packets.incoming;
using GameServer.realm.worlds.logic;
using NLog;

namespace GameServer.networking
{
    interface IPacketHandler
    {
        PacketId ID { get; }
        void Handle(Client client, IncomingMessage packet);
    }

    abstract class PacketHandlerBase<T> : IPacketHandler where T : IncomingMessage
    {
        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        protected abstract void HandlePacket(Client client, T packet);

        public abstract PacketId ID { get; }

        public void Handle(Client client, IncomingMessage packet)
        {
            HandlePacket(client, (T)packet);
        }

        protected bool IsTest(Client cli)
        {
            return cli?.Player?.Owner is Test;
        }
    }

    class PacketHandlers
    {
        public static Dictionary<PacketId, IPacketHandler> Handlers = new();

        static PacketHandlers()
        {
            foreach (var i in typeof(Packet).Assembly.GetTypes())
                if (typeof(IPacketHandler).IsAssignableFrom(i) &&
                    !i.IsAbstract && !i.IsInterface)
                {
                    IPacketHandler pkt = (IPacketHandler)Activator.CreateInstance(i);
                    Handlers.Add(pkt.ID, pkt);
                }
        }
    }
}