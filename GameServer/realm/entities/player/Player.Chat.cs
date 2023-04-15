using System.Text.RegularExpressions;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        static Regex nonAlphaNum = new("[^a-zA-Z0-9 ]", RegexOptions.CultureInvariant);

        static Regex repetition = new("(.)(?<=\\1\\1)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private int LastMessageDeviation = Int32.MaxValue;
        private string LastMessage = "";
        private long LastMessageTime = 0;
        private bool Spam = false;

        public bool CompareAndCheckSpam(string message, long time)
        {
            if (time - LastMessageTime < 500)
            {
                LastMessageTime = time;
                if (Spam)
                {
                    return true;
                }
                else
                {
                    Spam = true;
                    return false;
                }
            }

            string strippedMessage = nonAlphaNum.Replace(message, "").ToLower();
            strippedMessage = repetition.Replace(strippedMessage, "");

            if (time - LastMessageTime > 10000)
            {
                LastMessageDeviation = LevenshteinDistance(LastMessage, strippedMessage);
                LastMessageTime = time;
                LastMessage = strippedMessage;
                Spam = false;
                return false;
            }
            else
            {
                int deviation = LevenshteinDistance(LastMessage, strippedMessage);
                LastMessageTime = time;
                LastMessage = strippedMessage;

                if (LastMessageDeviation <= LengthThreshold(LastMessage.Length) &&
                    deviation <= LengthThreshold(message.Length))
                {
                    LastMessageDeviation = deviation;
                    if (Spam)
                    {
                        return true;
                    }
                    else
                    {
                        Spam = true;
                        return false;
                    }
                }
                else
                {
                    LastMessageDeviation = deviation;
                    Spam = false;
                    return false;
                }
            }
        }

        public static int LengthThreshold(int length)
        {
            return length > 4 ? 3 : 0;
        }

        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            for (int i = 0; i <= n; d[i, 0] = i++) ;

            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public void SendInfo(string text)
        {
            _client.SendText($"", 0, 0, "", text, 0, 0x0000FF);
        }

        public void SendInfo(string text, params object[] args)
        {
            _client.SendText($"", 0, 0, "", string.Format(text, args), 0, 0x0000FF);
        }

        public void SendError(string text)
        {
            _client.SendText($"*Error*", 0, 0, "", text, 0, 0xFF0000);
        }

        public void SendErrorFormat(string text, params object[] args)
        {
            _client.SendText($"*Error*", 0, 0, "", string.Format(text, args), 0, 0xFF0000);
        }

        public void SendClientText(string text)
        {
            _client.SendText($"*Client*", 0, 0, "", text, 0, 0);
        }

        public void SendClientTextFormat(string text, params object[] args)
        {
            _client.SendText($"*Client*", 0, 0, "", string.Format(text, args), 0, 0);
        }

        public void SendHelp(string text)
        {
            _client.SendText($"*Help*", 0, 0, "", text, 0, 0x5B3138);
        }

        public void SendHelpFormat(string text, params object[] args)
        {
            _client.SendText($"*Help*", 0, 0, "", string.Format(text, args), 0, 0x5B3138);
        }

        public void SendEnemy(string name, string text, uint nameColor = 0xFF0000)
        {
            _client.SendText(name, 0, 0, $"", text, nameColor, 0);
        }

        public void SendGuildDeath(string text)
        {
            _client.SendText($"", 0, 0, $"", text, 0, 0x97C688);
        
        }

        public void SendEnemyFormat(string name, string text, params object[] args)
        {
            _client.SendText("#" + name, 0, 0, $"", string.Format(text, args), 0, 0x00B300);
           
        }

        public void SendText(string sender, string text, uint nameColor = 0x123456, uint textColor = 0x123456)
        {
            _client.SendText(sender, 0, 0, $"", text, nameColor, textColor);
        }

        internal void TellReceived(int objId, int stars, int admin, string from, string to, string text)
        {
            Client.SendText(from, objId, 10, to, text, 0x00E6FF, 0x00E6FF);
        }
        
        internal void AnnouncementReceived(string text)
        {
            _client.Player.SendInfo(string.Concat("<ANNOUNCEMENT> ", text));

            /*client.SendPacket(new Text()
            {
                BubbleTime = 0,
                NumStars = -1,
                Name = "@ANNOUNCEMENT",
                Txt = text
            });*/
        }

        internal void GuildReceived(int objId, int stars, int admin, string from, string text)
        {
            Client.SendText(from, 0, 10, "*Guild", text, 0x97C688, 0x97C688);
        }
    }
}