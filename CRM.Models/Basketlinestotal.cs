using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Models
{
    public class Basketlinestotal
    {
        public decimal totalwithtax { get; set; }
        public int totallines { get; set; }
        public decimal totaltransportwithtax { get; set; }
        public decimal totaltopay { get; set; }
    }
}
