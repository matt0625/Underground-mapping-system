using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TUBETREKWPFV1.Classes;
using System.Net.WebSockets;
using TUBETREKWPFV1.Resources.Helpers;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.RegularExpressions;
using FuzzySharp;

namespace TUBETREKWPFV1.Pathfinding
{
    public class IRLtrains
    {
        private const string APPID = "84688913ad014de0914dfe4801e9f73f";
        private const string APPKEY = "e27247e88c364d1c930415fd7ae91838";

        // returns a NextTrain object storing necessary data to display on UI 
        // e.g. line, time
        public static async Task<NextTrain> FindNextJourneyOnLine(string StartStation, string NextStation, List<string> Lines)
        {
            string baseurl = "https://api.tfl.gov.uk/StopPoint/"; // add stationID/Arrivals
            string lineurl = "https://api.tfl.gov.uk/Line/"; // add lineid/Route/Sequence/All

            string ThisNaptanID = GetNaptanID(StartStation);
            string NextNaptanID = GetNaptanID(NextStation);

            baseurl += (ThisNaptanID + "/Arrivals");

            // normalising lines to allow for simple API querying
            for (int i = 0; i < Lines.Count; i++)
            {
                Lines[i] = NormaliseLine(Lines[i]);
            }
            

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(baseurl);
                
                // get response as JObj and sort by time to arrive at given station
                var Obj = JArray.Parse(response);
                var SortedObj = MergeSortJObj.Mergesort(Obj);

                // select the next line going in the correct direction if there is more than one option
                // e.g. in zone 1 H&C, metropolitan and circle lines all follow the same station and use the same trains so take the same time
                // we just select the one coming next and alter the path accordingly 
                string Selected = SelectLine(SortedObj, Lines);

                // outbound is inherently north/eastbound in the route sequence 
                lineurl += (Selected + "/Route/Sequence/outbound");
                var lineresponse = await client.GetStringAsync(lineurl);

                var obj1 = JObject.Parse(lineresponse);
                var routes = obj1["orderedLineRoutes"];

                // using the routes above to find the sequence (normal or reverse)
                string Sequence = GetSequence(routes, ThisNaptanID, NextNaptanID);

                // get the direction of travel based on sequence (NESW bound)
                string Direction = GetDirection(Sequence, Selected.ToLower());
                int IndexForRec = 0;

                // iterates over sorted api response to identify the index of the next train going the right direction 
                var k = Selected;
                foreach (var ob in SortedObj)
                {
                    if (ob["platformName"].ToString().Contains(Direction) && ob["lineId"].ToString().Contains(Selected))
                    {
                        break;
                    }
                    else
                    {
                        IndexForRec++;
                    }
                }

                var TrainInfo = SortedObj[IndexForRec];

                // set next train with relevant info
                NextTrain nextTrain = new NextTrain();
                nextTrain.time = SetNextTrainTime(TrainInfo);
                nextTrain.fromstation = StartStation;
                nextTrain.line = TrainInfo["lineName"].ToString();
                nextTrain.selected = Selected;

                return nextTrain;

            }
        }

        // gets the sequence (normal or reverse) by comparing the index of the naptanIDs of the station to the ordered line route
        // if the next stations index is higher, it is further along on the ordered route and therefore sequence is normal (vice versa)
        private static string GetSequence(JToken routes, string ThisNaptanID, string NextNaptanID)
        {
            var k = routes;
            for (int i = 0; i < routes.Count(); i++)
            {
                var x = routes[i]["naptanIds"];
                List<string> naptanIDlist = x.Values<string>().ToList();
                if (naptanIDlist.Contains(ThisNaptanID) && naptanIDlist.Contains(NextNaptanID))
                {
                    int thisindex = naptanIDlist.IndexOf(ThisNaptanID);
                    int nextindex = naptanIDlist.IndexOf(NextNaptanID);
                    if (thisindex < nextindex)
                    {
                        return "normal";
                    }
                    else
                    {
                        return "reverse";
                    }
                }
                if (i == routes.Count() - 1)
                {
                    Debug.WriteLine("Error");
                }
            }
            return null;
        }

        // builds line path with selected line
        public static List<string> BuildLinePath(Dictionary<string, List<string>> StationLineMap, string SelectedLine)
        {
            List<string> Path = new List<string>();
            foreach(var entry in StationLineMap)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    entry.Value[i] = NormaliseLine(entry.Value[i]);
                }
                if (entry.Value.Contains(SelectedLine))
                {
                    var LookNice = char.ToUpper(SelectedLine[0]) + SelectedLine.Substring(1);
                    Path.Add(LookNice);
                }
                else
                {
                    var LookNice = char.ToUpper(entry.Value[0][0]) + entry.Value[0].Substring(1);
                    Path.Add(LookNice);
                }
            }
            return Path;    
        }

        private static DateTime SetNextTrainTime(JToken TrainInfo)
        {
            return DateTime.Parse(TrainInfo["expectedArrival"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();
        }

        // takes the nearest line going in correct direction
        private static string SelectLine(JArray SortedArrivals, List<string> Lines)
        {
            for (int i = 0; i < SortedArrivals.Count; i++)
            {
                string nearestarrivalline = SortedArrivals[i]["lineId"].ToString();
                if (Lines.Contains(nearestarrivalline))
                {
                    return nearestarrivalline; break;
                }
            }
            return null;
        }

        // retrieves naptanIDs by parsing naptan.json
        public static string GetNaptanID(string Station)
        {
            string jsonstring = System.IO.File.ReadAllText("naptan.json");

            var NaptanParse = JsonConvert.DeserializeObject<List<Naptan>>(jsonstring);

            return GetIDByName(NaptanParse, Station);
        }

        // gets direction by compaing to the standard directions returned by the API for each line
        private static string GetDirection(string sequence, string line)
        {
            Dictionary<string, List<string>> DirectionDict = new Dictionary<string, List<string>>
            {
                {"Northbound", new List<string> {"bakerloo", "northern", "victoria"} },
                {"Southbound", new List<string> {"jubilee", "waterloo-city"} },
                {"Eastbound", new List<string> {"central", "district", "hammersmith-city", "metropolitan", "piccadilly", "circle" } }
            };

            foreach (var entry in DirectionDict)
            {
                if (entry.Value.Contains(line))
                {
                    if (sequence == "normal")
                    {
                        return entry.Key;
                    }
                    else
                    {
                        if (entry.Key == "Northbound")
                        {
                            return "Southbound";
                        }
                        return "Westbound";
                    }
                }
            }
            return "";
        }

        // gets ID given a string of the station name
        private static string GetIDByName(List<Naptan> NaptanParse, string station)
        {
            foreach (var naptan in NaptanParse)
            {
                string NormalisedNaptan = NormaliseName(naptan.commonName);
                string NormalisedInput = NormaliseName(station);

                if (NormalisedNaptan == NormalisedInput)
                {
                    return naptan.naptanID;
                }
            }
            return "";
        }

        // normalises all input strings by removing 'underground station' and bracketed contents
        public static string NormaliseName(string s)
        {
            string[] removewords = { "Underground", "Station" };
            foreach (var word in removewords)
            {
                s = s.Replace(word, "", StringComparison.OrdinalIgnoreCase);
            }

            // removes any bracketed strings and the brackets themselves 
            s = Regex.Replace(s, @"\([^\)]*\)", "");

            return s.Trim();
        }

        // normalises lines by handling special cases and generalising the rest 
        public static string NormaliseLine(string s)
        {
            s = s.ToLower();
            if (s == "hammersmith & city line")
            {
                return "hammersmith-city";
            }
            else if (s == "waterloo & city line")
            {
                return "waterloo-city";
            }

            string[] removewords = { "line" };
            foreach (var word in removewords)
            {
                s = s.Replace(word, "", StringComparison.OrdinalIgnoreCase);
            }
            return s.Trim();

        }
    }
}
