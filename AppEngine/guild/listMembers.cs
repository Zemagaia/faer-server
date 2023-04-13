using System.Collections.Specialized;
using Anna.Request;
using common;
using GameServer.networking;

namespace AppEngine.guild
{
    class listMembers : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            DbAccount acc;
            var password = query["password"];
            var status = Database.Verify(query["guid"], password, out acc);
            if (status == LoginStatus.OK)
            {
                if (acc.GuildId <= 0)
                {
                    Write(context, "<Error>Not in guild</Error>");
                    return;
                }

                var guild = Database.GetGuild(acc.GuildId);
                WriteXml(context, Guild.FromDb(Database, guild).ToXml().ToString());
            }
            else
                Write(context, "<Error>" + status.GetInfo() + "</Error>");
        }
    }
}