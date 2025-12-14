using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUBETREKWPFV1.Classes
{
    public class StationCoords
    {
        // this class is the structure of the path returned by A*
        // we will serialize each station into json and retreive it in the javascript to plot the coordinates of the stations in the route on the map
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }
}
