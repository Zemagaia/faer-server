using System.Collections.Specialized;
using System.Web;
using Anna.Request;
using common;
using GameServer.networking;

namespace AppEngine.guild
{
    class setBoard : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            DbAccount acc;
            var password = query["password"];
            var status = Database.Verify(query["guid"], password, out acc);
            if (status == LoginStatus.OK)
            {
                if (acc.GuildId <= 0 || acc.GuildRank < 20)
                {
                    Write(context, "<Error>No permission</Error>");
                    return;
                }

                var guild = Database.GetGuild(acc.GuildId);
                var text = HttpUtility.UrlDecode(query["board"]);
                if (Database.SetGuildBoard(guild, text))
                {
                    Write(context, text);
                    return;
                }

                Write(context, "<Error>Failed to set board</Error>");
            }
            else
                Write(context, "<Error>" + status.GetInfo() + "</Error>");
        }
    }
}