using System.Collections.Specialized;
using System.Text;
using Anna.Request;
using Anna.Responses;
using AppEngine.account;
using AppEngine.app;
using AppEngine.@char;
using AppEngine.credits;
using AppEngine.guild;
using AppEngine.picture;
using common;
using common.resources;
using MimeMapping;

namespace AppEngine
{
    abstract class RequestHandler
    {
        public abstract void HandleRequest(RequestContext context, NameValueCollection query);

        public virtual void InitHandler(Resources resources)
        {
        }

        protected Database Database => Program.Database;

        internal void Write(RequestContext req, string val, bool zip = true)
        {
            if (zip)
            {
                var zipped = Utils.Deflate(Encoding.UTF8.GetBytes(val));
                Write(req, zipped, true);
                return;
            }

            Write(req.Response(val), "text/plain");
        }

        internal void Write(RequestContext req, byte[] val, bool zipped = false)
        {
            Write(req.Response(val), "text/plain", zipped);
        }

        internal void WriteXml(RequestContext req, string val, bool zip = true)
        {
            if (zip)
            {
                var zippedXml = Utils.Deflate(Encoding.UTF8.GetBytes(val));
                WriteXml(req, zippedXml, true);
                return;
            }

            Write(req.Response(val), "application/xml");
        }

        internal void WriteXml(RequestContext req, byte[] val, bool zipped)
        {
            Write(req.Response(val), "application/xml", zipped);
        }

        internal void WriteImg(RequestContext req, byte[] val)
        {
            Write(req.Response(val), "image/png");
        }

        internal void WriteSnd(RequestContext req, byte[] val)
        {
            Write(req.Response(val), "*/*");
        }

        internal void Write(Response r, string type, bool zipped = false)
        {
            if (zipped)
                r.Headers["Content-Encoding"] = "deflate";

            r.Headers["Content-Type"] = type;
            r.Send();
        }
    }

    internal static class RequestHandlers
    {
        public static void Initialize(Resources resources)
        {
            foreach (var h in Get)
                h.Value.InitHandler(resources);
            foreach (var h in Post)
                h.Value.InitHandler(resources);

            InitWebFiles(resources);
        }

        private static void InitWebFiles(Resources resources)
        {
            if (Get.ContainsKey("/"))
                throw new InvalidOperationException("Get handlers have already been initialized.");

            Get["/"] = new StaticFile(resources.WebFiles["/index.html"], "text/html");

            foreach (var f in resources.WebFiles)
                Get[f.Key] = new StaticFile(f.Value, MimeUtility.GetMimeMapping(f.Key));
        }

        public static readonly Dictionary<string, RequestHandler> Get = new()
        {
            { "/account/rp", new resetPassword() }
        };

        public static readonly Dictionary<string, RequestHandler> Post = new()
        {
            { "/char/list", new list() },
            { "/char/delete", new delete() },
            { "/char/fame", new @char.fame() },
            { "/account/register", new register() },
            { "/account/verify", new verify() },
            { "/account/fp", new forgotPassword() },
            { "/account/rp", new resetPassword() },
            { "/account/sve", new sendVerifyEmail() },
            { "/account/cpass", new changePassword() },
            { "/account/pcharS", new purchaseCharSlot() },
            { "/account/setName", new setName() },
            { "/credits/goffers", new getoffers() },
            { "/credits/add", new add() },
            { "/fame/list", new fame.list() },
            { "/picture/get", new get() },
            { "/app/glangs", new getLanguageStrings() },
            { "/app/init", new init() },
            { "/account/verAge", new verifyage() },
            { "/app/gSXmls", new getServerXmls() },
            { "/char/pcu", new purchaseClassUnlock() },
            { "/account/pskin", new purchaseSkin() },
            { "/app/getTexs", new getTextures() },
            { "/guild/listMems", new listMembers() },
            { "/guild/getBoard", new getBoard() },
            { "/guild/setBoard", new setBoard() },
            { "/account/rank", new rank() },
            { "/account/registerDiscord", new registerDiscord() },
            { "/account/unregisterDiscord", new unregisterDiscord() },
            { "/blank", new blank() }, // temp
        };
    }
}