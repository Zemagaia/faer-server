using System.Collections.Specialized;
using Anna.Request;
using common;
using GameServer.networking;

namespace AppEngine.account
{
    class verifyage : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            DbAccount acc;
            var password = query["password"];
            var status = Database.Verify(query["guid"], password, out acc);
            if (status == LoginStatus.OK)
            {
                if (query["isAgeVerified"].Equals("1"))
                    Database.ChangeAgeVerified(acc, true);
                else
                    Database.ChangeAgeVerified(acc, false);

                Write(context, "<Success />");
            }
            else
                Write(context, "<Error>" + status.GetInfo() + "</Error>");
        }
    }
}