using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace zedcrest.api.Models.Shared
{
    public class EmailConfigOptions
    {
        public string Server { get; set; }
        public string Sender { get; set; }
        public string From { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool UsSSL { get; set; }
    }
}
