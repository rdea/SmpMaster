
using System.Collections.Generic;

namespace CRM.Models
{
    public class area
    {
        public string areaname { get; set; }
        public string subfamily { get; set; }
        public string family { get; set; }
        public string subsection { get; set; }
        public string section { get; set; }
        public string division { get; set; }
        public string enterprise { get; set; }
        public List<area> hijos = new List<area>();
        public long id { get; set; }
        public string slug { get; set; }
    }
}
