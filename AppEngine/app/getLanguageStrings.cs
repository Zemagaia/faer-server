using System.Collections.Specialized;
using Anna.Request;

namespace AppEngine.app
{
    class getLanguageStrings : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            Write(context, Program.Resources.Languages[query["languageType"]], true);
        }
    }
}