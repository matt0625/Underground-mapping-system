using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUBETREKWPFV1.Classes
{
    // another global class with static properties from which I can emulate the use of cookies
    // on login we set these static properties which can then be used for querying the database/if needed
    class UserSession
    {
        public static int UserID { get; set; }
        public static string Username { get; set;}
    }
}
