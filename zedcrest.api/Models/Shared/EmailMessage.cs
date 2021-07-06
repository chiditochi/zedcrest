using Microsoft.AspNetCore.Http;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace zedcrest.api.Models.Shared
{
    public class EmailMessage
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public IEnumerable<string> Attachments { get; set; }

        public EmailMessage(IEnumerable<string> to, string subject, string content, IEnumerable<string> attachments)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => MailboxAddress.Parse(x)));
            Subject = subject;
            Content = content;
            Attachments = attachments;
        }
    }
}
