using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MVVMtutorial.Pathfinding
{
    class DataHandler
    {
        private const string BASEURL = "https://api.tfl.gov.uk/line/";
        private List<string> LineQueryIDs = new List<string>() { "bakerloo", "central", "circle", "district", "hammersmith-city", "jubilee", "metropolitan", "northern", "piccadilly", "victoria", "waterloo-city" };
        protected static Dictionary<string, List<string>> LineStoppoints = new Dictionary<string, List<string>>();

        // this subroutine queries the TFL api to return the stations on each line
        // it then matches each stop with each line it is on, so we can proceed to request the API again and get the transfer time between different lines on a stop
        public async Task GetLinesStops(HttpClient httpClient)
        {
            foreach (var line in LineQueryIDs)
            {
                List<string> Stations = new List<string>();
                using HttpResponseMessage response = await httpClient.GetAsync(BASEURL + line + "/stoppoints");
                response.EnsureSuccessStatusCode();

                var jsonstring = await response.Content.ReadAsStringAsync();
                JArray array = JArray.Parse(jsonstring);

                foreach (var item in array)
                {
                    Stations.Add(item["commonName"].ToString().Replace(" Underground Station", ""));
                }
                LineStoppoints[line] = Stations;
            }
        }

        // overall method to parse JSON
        public ConnectionList? PrepJSON(string json)
        {
            LondonDeserialiser deserialiser = new LondonDeserialiser();
            var v = deserialiser.Deserialize(json);
            return v;
        }


        // this version of build graph attempts to include the lines of each connection by using a tuple for the adjacency list
        public Dictionary<string, List<Tuple<string, int, string>>> BuildGraphWithLine(ConnectionList v)
        {
            // this structure of dictionary fits the structure i have implemented in my A* algorithm; adjacency list
            var ConnectGraph = new Dictionary<string, List<Tuple<string, int, string>>>();
            // though this nested iteration is O(n^2), the length of stations in the json is unchanging so the operation -> O(1) as we know what n is
            foreach (var station in v.Stations)
            {
                string id = station.id;
                string name_key = station.name;
                // creating an empty value tuple with default values as at this point there are no connections to add manually
                List<Tuple<string, int, string>> tmpConnections = new List<Tuple<string, int, string>>();
                // iterating over connections and building the 2 way adjacency list by storing station name, travel cost and line ID
                for (int i = 0; i < v.Connections.Count; i++)
                {
                    var conn = v.Connections[i];
                    if (conn.station1 == id)
                    {
                        string connectedstationid = conn.station2;
                        string connectedstationname = ""; string lineofconnection = "";
                        foreach (var station2 in v.Stations)
                        {
                            if (station2.id == connectedstationid)
                            {
                                connectedstationname = station2.name;
                                lineofconnection = conn.line;
                            }
                        }
                        Tuple<string, int, string> ConnInfo = new Tuple<string, int, string>(connectedstationname, Convert.ToInt32(conn.time), lineofconnection);
                        tmpConnections.Add(ConnInfo);
                    }
                }
                ConnectGraph[name_key] = tmpConnections;


            }

            // the json file used only has one-way connections, depending on which station is alphabetically first
            // i.e. 'Baker Street' will point to 'Bond Street', but the json contains no connection for the reverse, although they are connected
            // as my A* algorithm is made for an adjacency list, I must manually add the 'reverse connection' to the list each station points to (you can go in any direction on the underground, after all)
            // ^ this is done below
            for (int i = 0; i < ConnectGraph.Count; i++)
            {
                var CurrentConns = ConnectGraph.ElementAt(i).Value;
                string CurrentStation = ConnectGraph.ElementAt(i).Key;
                foreach (var conn in CurrentConns)
                {
                    int time = conn.Item2;
                    string line = conn.Item3;
                    var PendingConn = new Tuple<string, int, string>(CurrentStation, Convert.ToInt16(time), line);
                    if (!ConnectGraph[conn.Item1].Contains(PendingConn))
                    {
                        ConnectGraph[conn.Item1].Add(PendingConn);
                    }

                }
            }

            return ConnectGraph;
        }

    }

    class AStarDataReqs : DataHandler
    {
        public static bool IsLineSwitch(string CurrentStation, (string, int) neighbour)
        {
            foreach (var kvp in LineStoppoints)
            {
                if (kvp.Value.Contains(CurrentStation) && kvp.Value.Contains(neighbour.Item1))
                {
                    return false;
                }
            }
            return true;
        }
    }

    class LondonDeserialiser
    {
        // auxiliary class and method to deserialize json into custom structure defined below
        public ConnectionList? Deserialize(string json)
        {
            var ConnectionList = JsonSerializer.Deserialize<ConnectionList>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return ConnectionList;
        }
    }

    // the POCO (plain old CLR object) class needed to deserialize the london.json complex json file
    // the instance variables align with the structure of the complex json, which is divided into 'connections', 'lines' and 'stations' objects, each with their own sub-objects (hence the need for further classes)
    // i.e. this class is custom to deserialising this json file
    class ConnectionList
    {
        public List<Connections> Connections { get; set; }

        public List<Line> Lines { get; set; }

        public List<Station> Stations { get; set; }
    }

    // for each connection in the POCO list, we define its instance variables according to the structure of the json
    // 'station1' and 'station2' are connected via the 'line' detailed, with a transfer time of 'time' (needed for A*)
    class Connections
    {
        public string? station1 { get; set; }
        public string? station2 { get; set; }
        public string? line { get; set; }
        public string? time { get; set; }
    }

    //for each line in the POCO list, define the 'line' variable which matches the connections instance var. 
    // each line maps to a name, with a unique colour and stripe a la the tube map
    class Line
    {
        public string? line { get; set; }

        public string? name { get; set; }

        public string? colour { get; set; }

        public string? stripe { get; set; }
    }

    // the required info for each station
    // note display_name contains a html <br/> tag for those stations with longer names (e.g. King's Cross St. Pancras), and is null otherwise (e.g. Victoria)
    class Station
    {
        public string? id { get; set; }
        public string? latitude { get; set; }
        public string? longitude { get; set; }
        public string? name { get; set; }
        public string? display_name { get; set; }
    }
}

