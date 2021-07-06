using zedcrest.api.Models.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MimeKit;

namespace zedcrest.api.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfigOptions _emailConfigOptions;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _env;
        public EmailService(
            EmailConfigOptions emailConfigOptions,
            ILogger<EmailService> logger,
            IWebHostEnvironment env
            )
        {
            _emailConfigOptions = emailConfigOptions;
            _logger = logger;
            _env = env;
        }

        public async void SendEmail(EmailMessage message)
        {
            var emailMessage = CreateEmailMessage(message);
            var result = await SendEmailAsyn(emailMessage);
            string resultText = result == true ? "YES" : "NO";
            _logger.LogInformation($"SendEmail: email sent to {string.Join(",", message.To)} : Successful::{resultText}");
        }
        private MailMessage CreateEmailMessage(EmailMessage message)
        {
            var mMessage = new MailMessage();
            mMessage.From = new MailAddress(_emailConfigOptions.UserName, _emailConfigOptions.Sender);
            foreach (var itemMessageTo in message.To)
            {
                mMessage.To.Add(itemMessageTo.ToString());
            }
            mMessage.Subject = message.Subject;
            mMessage.IsBodyHtml = true;
            mMessage.BodyEncoding = System.Text.Encoding.GetEncoding("utf-8");
            var bodyBuilder = new BodyBuilder { HtmlBody = string.Format("<div style=''>{0}<p>Regards</p></div>", message.Content) };
            mMessage.Body = bodyBuilder.HtmlBody;
            if (message.Attachments != null && message.Attachments.Any())
            {
                foreach (var attachment in message.Attachments)
                {
                    var filePath = Path.Join(_env.WebRootPath, "Uploads", attachment);
                    mMessage.Attachments.Add(new Attachment(filePath));
                }
            }
            return mMessage;
        }

        private System.Net.Mail.SmtpClient getConnection()
        {
            var smtp = new System.Net.Mail.SmtpClient(_emailConfigOptions.Server, _emailConfigOptions.Port)
            {
                EnableSsl = _emailConfigOptions.UsSSL,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailConfigOptions.UserName, _emailConfigOptions.Password)
            };
            return smtp;
        }
        public async Task<bool> SendEmailAsyn(MailMessage message)
        {
            var result = false;
            using (var client = getConnection())
            {
                try
                {
                    await client.SendMailAsync(message);
                    result = true;
                }
                catch (Exception e)
                {
                    _logger.LogError("Error sending email", e.Message);
                }
                return result;

            }
        }
    }
}
