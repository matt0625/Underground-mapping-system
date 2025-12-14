using MVVMtutorial.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUBETREKWPFV1.Classes
{
    class GlobalGraph
    {
        // this is the global class I am using to store these data structures that were loaded on startup
        // this is because many features in the app use these structures so they should be initialised just once on startup and then referenced rather than prepared every time
        public static ConnectionList? ConnectionList { get; set; }
        public static Dictionary<string, List<Tuple<string, int, string>>> Graph {  get; set; }
    }
}
