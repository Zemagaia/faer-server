using System.Collections.Specialized;
using Anna.Request;
using common;
using GameServer.networking;

namespace AppEngine.account
{
    class register : RequestHandler
    {
        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            if (!Utils.IsValidEmail(query["newGUID"]))
                Write(context, "<Error>Invalid email</Error>");
            else
            {
                var key = Database.REG_LOCK;
                string lockToken = null;
                var password = query["newPassword"];

                if (password.Length < 10)
                {
                    Write(context, "<Error>The password is too short</Error>");
                    return;
                }

                try
                {
                    while ((lockToken = Database.AcquireLock(key)) == null) ;

                    DbAccount acc;
                    var status = Database.Verify(query["guid"], "", out acc);
                    if (status == LoginStatus.OK)
                    {
                        //what? can register in game? kill the account lock
                        if (!Database.RenameUUID(acc, query["newGUID"], lockToken))
                        {
                            Write(context, "<Error>Duplicate Email</Error>");
                            return;
                        }

                        Database.ChangePassword(acc.UUID, password);
                        Database.Guest(acc, false);
                        Write(context, "<Success />");
                    }
                    else
                    {
                        var s = Database.Register(query["newGUID"], password, false, out acc);
                        if (s == RegisterStatus.OK)
                            Write(context, "<Success />");
                        else
                            Write(context, "<Error>" + s.GetInfo() + "</Error>");
                    }
                }
                finally
                {
                    if (lockToken != null)
                        Database.ReleaseLock(key, lockToken);
                }
            }
        }
    }
}