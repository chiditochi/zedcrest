using zedcrest.api.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace zedcrest.api.Services
{
    public interface IEmailService
    {
        public void SendEmail(EmailMessage message);
    }
}
