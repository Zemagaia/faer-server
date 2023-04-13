using System.Collections.Specialized;
using Anna.Request;
using common;
using GameServer.networking;

namespace AppEngine.@char
{
    class delete : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            DbAccount acc;
            var password = query["password"];
            var status = Database.Verify(query["guid"], password, out acc);
            if (status == LoginStatus.OK)
            {
                using (var l = Database.Lock(acc))
                    if (Database.LockOk(l))
                    {
                        Database.DeleteCharacter(acc, int.Parse(query["charId"]));
                        Write(context, "<Success />");
                    }
                    else
                        Write(context, "<Error>Account in Use</Error>");
            }
            else
                Write(context, "<Error>" + status.GetInfo() + "</Error>");
        }
    }
}