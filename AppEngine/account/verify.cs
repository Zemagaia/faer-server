using System.Collections.Specialized;
using Anna.Request;
using common;
using GameServer.networking;

namespace AppEngine.account
{
    class verify : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            DbAccount acc;
            var password = query["password"];
            var status = Database.Verify(query["guid"], password, out acc);
            if (status == LoginStatus.OK)
                Write(context, Account.FromDb(acc).ToXml().ToString());
            else
                Write(context, "<Error>" + status.GetInfo() + "</Error>");
        }
    }
}