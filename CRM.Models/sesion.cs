
using System;

namespace CRM.Models
{
    public class sesion
    {
        public DateTime limit { get; set; }
        public DateTime sessionInit { get; set; }
        public string explained { get; set; }
        public string publicallowed { get; set; }
        public string status { get; set; }
        public string activeToken { get; set; }

    }

}
