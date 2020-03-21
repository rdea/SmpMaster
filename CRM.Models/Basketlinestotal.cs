using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Models
{
    public class BasketlinestotalRecibe
    {
        public string explained { get; set; }
        public string status { get; set; }

        public string totalwithtax { get; set; }
        public string totallines { get; set; }
        public string totaltransportwithtax { get; set; }
        public string totaltopay { get; set; }
    }
}
