using System.Net.Mail;

namespace Linxens.Core.Service
{
    public class MailService
    {
        private readonly SmtpClient _smtpClient;

        public MailService(string host, int port)
        {
            this._smtpClient = new SmtpClient(host, port);
            this._smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            this._smtpClient.UseDefaultCredentials = true;
        }

        public void Send(string from, string to, string subject, string body)
        {
            MailMessage email = new MailMessage(from, to);
            email.Subject = subject;
            email.Body = body;

            this._smtpClient.Send(email);
        }
    }
}