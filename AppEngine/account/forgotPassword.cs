using System.Collections.Specialized;
using Anna.Request;
using common;
using NLog;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AppEngine.account
{
    class forgotPassword : RequestHandler
    {
        private static readonly Logger PassLog = LogManager.GetLogger("PassLog");

        public override void HandleRequest(RequestContext context, NameValueCollection query)
        {
            // get query
            var accEmail = query["guid"];

            // verify email exist
            DbAccount acc;
            var status = Database.Verify(accEmail, "", out acc);
            if (!Utils.IsValidEmail(accEmail) || status == LoginStatus.AccountNotExists)
            {
                Write(context, "<Error>Email not recognized</Error>");
                return;
            }

            // save reset token
            acc = Database.GetAccount(accEmail);
            acc.PassResetToken = Guid.NewGuid().ToString().Replace("-", "");
            acc.FlushAsync();

            // send email
            var resetLink = "http://test.nillysrealm.com/account/rp?b=${b}&a=${a}"
                .Replace("${b}", acc.PassResetToken)
                .Replace("${a}", acc.AccountId.ToString());

            var apikey = Program.Config.serverSettings.sendGridApiKey;
            var sg = new SendGridClient(apikey);
            var from = new EmailAddress("noreply@nillysrealm.com", "Nilly's Realm");
            var subject = "Password Reset Request on Nilly's Realm";
            var to = new EmailAddress(accEmail);
            var content = new Content("text/plain", Program.Resources.ChangePass.GetRequestEmail(resetLink));
            var mail = new SendGridMessage {
                From = from,
                Subject = subject,
                ReplyTo = to,
                Contents = new List<Content> {content}
            };
            sg.SendEmailAsync(mail);

            Write(context, "<Success />");
            PassLog.Info(
                $"Password reset requested. IP: {context.Request.ClientIP()}, Account: {acc.Name} ({acc.AccountId})");
        }
    }
}