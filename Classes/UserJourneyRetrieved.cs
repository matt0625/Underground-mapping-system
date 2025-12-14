using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUBETREKWPFV1.Classes
{
    public class UserJourneyRetrieved
    {
        // class tailored to handling the retrieval of necessary database data for display in the RecentJourneyWindow view
        public DateTime? DateTime { get; set; }

        public string StartStation { get; set; }

        public string Destination { get; set; }


        public string Journey { get; set; }
        public List<string> PathList { get; set; }
    }
}

