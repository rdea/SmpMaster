using System.Collections.Generic;
using System.Linq;

namespace CRM.Models
{
    public class DataCollection<T> where T : class
    {
        public bool HasItems
        {
            get
            {
                return result != null && result.Any();
            }
        }
        public string explained { get; set; }
        public List<resultbrand> resultbrands { get; set; }

        public List<T> result { get; set; }
        public string status { get; set; }

    }
    public class DataCollectionSingle<T> where T : class
    {

        public string explained { get; set; }
        public T result { get; set; }
        public List<resultbrand> resultbrands { get; set; }

        public string status { get; set; }

    }

}
