using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUBETREKWPFV1.Classes;

namespace MVVMtutorial.Pathfinding
{
        class AStar
        {
            public static List<string> FindPath(Dictionary<string, List<Tuple<string, int, string>>> Graph, string Start, string End)
            {
                var OpenSet = new Dictionary<string, double>();
                var CloseList = new Dictionary<string, string>();
                var Gscores = new Dictionary<string, double>();
                var Fscores = new Dictionary<string, double>();
                var LastUsedLine = new Dictionary<string, string>(); // Store the last used line for each station

                // intialise g(n) and f(n) as max value so these can be overwritten when the actual values are calculated 
                foreach (var node in Graph.Keys)
                {
                    Gscores[node] = double.MaxValue;
                    Fscores[node] = double.MaxValue;
                }

                // set starting node g(n) and f(n) values
                Gscores[Start] = 0;
                Fscores[Start] = Heuristic(Graph, Start, End, GlobalGraph.ConnectionList);
                // appending start to open set to allow exploration of neighbours
                OpenSet.Add(Start, Fscores[Start]);

                LastUsedLine[Start] = ""; // Initialize with no line

                // continue searching while there are nodes to explore
                while (OpenSet.Count > 0)
                {
                    double MinF = double.MaxValue;
                    string CurrentStation = "";

                    // select node with least f score in openset and remove it
                    foreach (var Entry in OpenSet)
                    {
                        if (Entry.Value < MinF)
                        {
                            MinF = Entry.Value;
                            CurrentStation = Entry.Key;
                        }
                    }
                    OpenSet.Remove(CurrentStation);
                    
                    // check if current node is destination 
                    if (CurrentStation == End)
                    {
                        return ReconstructPath(CloseList, CurrentStation);
                    }

                    // set previous line to the stored entry in dict if exists, otherwise null
                    string PrevLine = LastUsedLine.ContainsKey(CurrentStation) ? LastUsedLine[CurrentStation] : "";
    
                    // iterate through all neighbouring stations
                    foreach (var TupEntry in Graph[CurrentStation])
                    {
                        // extract in this order: station name, travel cost, current underground line
                        string neighbour = TupEntry.Item1;
                        int weight = TupEntry.Item2;
                        string currentLine = TupEntry.Item3;

                        if (IsLineSwitch(PrevLine, currentLine))
                        {
                            // add penalty for switching lines
                            weight += TransferTime.AddTransferTime(CurrentStation) + 100; 
                        }

                        double TentativeG = weight + Gscores[CurrentStation];
                        
                        // compare newly calculated Gscore to previous entry 
                        if (TentativeG <= Gscores[neighbour])
                        {
                            // add to path and update f(n), g(n)
                            CloseList[neighbour] = CurrentStation;
                            Gscores[neighbour] = TentativeG;
                            Fscores[neighbour] = Gscores[neighbour] + Heuristic(Graph, neighbour, End, GlobalGraph.ConnectionList);

                            // add neighbour to openset if not already there, otherwise update its fscore 
                            if (!OpenSet.ContainsKey(neighbour))
                            {
                                OpenSet[neighbour] = Fscores[neighbour];
                            }
                            else
                            {
                                OpenSet[neighbour] = Math.Min(OpenSet[neighbour], Fscores[neighbour]);
                            }
                            // update last used line for the neighbour
                            LastUsedLine[neighbour] = currentLine; 
                        }
                    }
                }
                return null;
            }

            // returns true if previous line != current            
            public static bool IsLineSwitch(string PrevLine, string CurrentLine)
            {
                return !string.IsNullOrEmpty(PrevLine) && PrevLine != CurrentLine;
            }


            // this method gets the coordinates of the Station parameter and returns the tuple structure outlined above
            public static List<Tuple<string, string, string>> GetPathStationCoordinates(ConnectionList v, List<string> Stations)
            {
                List<Tuple<string, string, string>> ReturnPath = new List<Tuple<string, string, string>>();
                    foreach (string s in Stations)
                    {
                        foreach (var entry in v.Stations)
                        {
                            if (entry.name == s)
                            {
                                ReturnPath.Add(new Tuple<string, string, string>(s, entry.latitude, entry.longitude));
                            }
                        }
                    }
                    return ReturnPath;
            }

            // backtrack to find path
            public static List<string> ReconstructPath(Dictionary<string, string> CLoseList, string CurrentStation)
            {
                var Path = new List<string>() { CurrentStation };
                while (CLoseList.ContainsKey(CurrentStation))
                {
                    CurrentStation = CLoseList[CurrentStation];
                    // at each step insert the travelled to station at the front of path (working backwards)
                    Path.Insert(0, CurrentStation);
                }
                return Path;
            }

            // checks if a key:value PAIR exists in the given dict 
            public static bool OpenContainsKVP(Dictionary<string, double> OpenSet, string ExpectedKey, double ExpectedVal)
            {
                double ActualValue;
                if (!OpenSet.TryGetValue(ExpectedKey, out ActualValue))
                {
                    return false;
                }
                return ActualValue == ExpectedVal;
            }

            // sets up values for haversine and returns final heuristic computation
            public static double Heuristic(Dictionary<string, List<Tuple<string, int, string>>> Graph, string Start, string End, ConnectionList v)
            {
                var StartStationEntry = v.Stations.FirstOrDefault(s => s.name == Start);
                var EndStationEntry = v.Stations.FirstOrDefault(s => s.name == End);

                double lat1 = double.Parse(StartStationEntry.latitude);
                double lon1 = double.Parse(StartStationEntry.longitude);
                double lat2 = double.Parse(EndStationEntry.latitude);
                double lon2 = double.Parse(EndStationEntry.longitude);

                return HaversineHeuristic(lat1, lon1, lat2, lon2);
            }

            // advanced heuristic using difference between 2 points on a sphere
            public static double HaversineHeuristic(double lat1, double lon1, double lat2, double lon2)
            {
                const double EarthRadius = 6371.0; // in KM

                double LatDelta = ToRadians(lat2 - lat1);
                double LonDelta = ToRadians(lon2 - lon1);

                // see design for clearer display of this equation 
                double a = Math.Pow(Math.Sin(LatDelta / 2), 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(LonDelta), 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return EarthRadius * c;
            }

            private static double ToRadians(double InDegrees)
            {
                return InDegrees * (Math.PI / 180);
            }

            // extracts the time for each connection from the graph and builds as a list
            public static List<string> PairPathToTime(ConnectionList v, List<string> Path)
            {
                var Times = new List<string>();
                for (int i = 0; i < Path.Count - 1; i++)
                {
                    string Station1Name = Path[i];
                    string Station2Name = Path[i + 1];
                    
                    string Station1ID = v.Stations.FirstOrDefault(s => s.name == Station1Name).id;
                    string Station2ID = v.Stations.FirstOrDefault(s => s.name == Station2Name).id;

                    Times.Add(v.Connections.FirstOrDefault(c => (c.station1 == Station1ID && c.station2 == Station2ID) || (c.station1 == Station2ID && c.station2 == Station1ID)).time);
                }
                return Times;

            }

            // extracts the lines for each connection from the graph and builds as a list 
            public static Dictionary<string, List<string>> PairPathToLine(ConnectionList v, List<string> Path)
            {
                List<string> Lines = new List<string>();
                Dictionary<string, List<string>> StationLineMap = new Dictionary<string, List<string>>();
                for (int i = 0; i < Path.Count - 1; i++)
                {
                    // set temp station variables to current and next station in path
                    string station1 = Path[i]; string station2 = Path[i + 1];
                    string id1 = ""; string id2 = "";
                    // iterate over connections, break when a connection's station ids match the temp variables  
                    foreach (var Station in v.Stations)
                    {
                        if (id1 != "" && id2 != "")
                        {
                            break;
                        }
                        else if (Station.name == station1)
                        {
                            id1 = Station.id;
                        }
                        else if (Station.name == station2)
                        {
                            id2 = Station.id;
                        }
                    }
                    // converting the station ids to names by iterating through ConnectionList class structure and extracting the line ID
                    foreach (var conn in v.Connections)
                    {
                        if ((conn.station1 == id1 && conn.station2 == id2) || (conn.station2 == id1 && conn.station1 == id2))
                        {
                            Lines.Add(conn.line);
                            if (StationLineMap.ContainsKey(station1))
                            {
                                StationLineMap[station1].Add(conn.line);
                            }
                            else
                            {
                                StationLineMap.Add(station1, new List<string> { conn.line });
                            }
                        }
                    }
                }

                // converting line IDs to line names
                foreach (var entry in StationLineMap)
                {
                    for (int i = 0; i < v.Lines.Count; i++)
                    {
                        var currentline = v.Lines[i];
                        for (int j = 0; j < entry.Value.Count; j++)
                        {
                            if (currentline.line == entry.Value[j])
                            {
                                entry.Value[j] = currentline.name; break;
                            }

                        }

                    }
                }
                return StationLineMap;
            }

        }
}
