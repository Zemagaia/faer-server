using System.Collections.Specialized;
using Anna.Request;

namespace AppEngine.app
{
    class getServerXmls : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            Write(context, Program.Resources.GameData.ZippedXmls, true);
        }
    }
}