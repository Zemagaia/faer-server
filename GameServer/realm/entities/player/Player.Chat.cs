using System.Text.RegularExpressions;

namespace GameServer.realm.entities.player; 

partial class Player
{
    private static Regex nonAlphaNum = new("[^a-zA-Z0-9 ]", RegexOptions.CultureInvariant);

    private static Regex repetition = new("(.)(?<=\\1\\1)",
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

        var strippedMessage = nonAlphaNum.Replace(message, "").ToLower();
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
            var deviation = LevenshteinDistance(LastMessage, strippedMessage);
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
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (var i = 0; i <= n; d[i, 0] = i++) ;

        for (var j = 0; j <= m; d[0, j] = j++) ;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public void SendInfo(string text)
    {
        _client.SendText("", 0, 0, "", text, 0, 0xFFD700);
    }
    
    public void SendError(string text)
    {
        _client.SendText("", 0, 0, "", text, 0, 0xFF0000);
    }

    internal void TellReceived(int objId, int stars, int admin, string from, string to, string text)
    {
        Client.SendText(from, objId, 10, to, text, 0x00E6FF, 0x00E6FF);
    }
        
    internal void AnnouncementReceived(string text)
    {
        _client.Player.SendInfo("<Announcement> " + text);
    }

    internal void GuildReceived(int objId, int stars, int admin, string from, string text)
    {
        Client.SendText(from, 0, 10, "", text, 0x97C688, 0x97C688);
    }
}