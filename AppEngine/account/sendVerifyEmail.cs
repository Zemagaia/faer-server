using System.Collections.Specialized;
using Anna.Request;

namespace AppEngine.account
{
    class sendVerifyEmail : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            Write(context, "<Error>Nope.</Error>");
        }
    }
}