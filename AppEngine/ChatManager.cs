using common;
using NLog;

namespace AppEngine
{
    public class ChatManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly InterServerChannel _interServer;

        public ChatManager(InterServerChannel interServer)
        {
            _interServer = interServer;

            // listen to chat communications
            _interServer.AddHandler<ChatMsg>(Channel.Chat, HandleChat);
        }

        private void HandleChat(object sender, InterServerEventArgs<ChatMsg> e)
        {
            switch (e.Content.Type)
            {
                case ChatType.Tell:
                {
                    var from = _interServer.Database.ResolveIgn(e.Content.From);
                    var to = _interServer.Database.ResolveIgn(e.Content.To);
                    bool filtered = Program.Resources.FilterList.Any(r => r.IsMatch(e.Content.Text));
                    Log.Info("<{0} -> {1}{2}> {3}", from, to, filtered ? " *filtered*" : "", e.Content.Text);
                    break;
                }
                case ChatType.Guild:
                {
                    var from = _interServer.Database.ResolveIgn(e.Content.From);
                    Log.Info("<{0} -> Guild> {1}", from, e.Content.Text);
                    break;
                }
                case ChatType.Announce:
                    Log.Info("<Announcement> {0}", e.Content.Text);
                    break;
            }
        }
    }
}