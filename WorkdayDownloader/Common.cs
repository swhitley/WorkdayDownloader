using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Configuration;


namespace WorkdayDownloader
{
    public static class Common
    {
        public static void EmailSend(string subject, string body)
        {
            string host = ConfigurationManager.AppSettings["SMTPServer"];
            int port = int.Parse(ConfigurationManager.AppSettings["SMTPPort"].ToString());
            string from = ConfigurationManager.AppSettings["SMTPFrom"];
            string to = ConfigurationManager.AppSettings["SMTPTo"];

            SmtpClient mail = new SmtpClient(host, port);

            mail.Send(new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = true
            });
        }
    }
}
