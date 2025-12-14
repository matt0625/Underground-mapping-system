using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TUBETREKWPFV1.Pathfinding;
using FuzzySharp;
using TUBETREKWPFV1.Classes;

namespace MVVMtutorial.Pathfinding
{

    internal class TransferTime
    {
        // prerequisites to access TFL API
        private const string APPID = "84688913ad014de0914dfe4801e9f73f";
        private const string APPKEY = "e27247e88c364d1c930415fd7ae91838";
        
        // intialising static map of station to zone
        // e.g. "Upminster": "6"
        private static Dictionary<string, string> StationZonePair = new Dictionary<string, string>();
        // approximate transfer times given the zone of the station - just needs to be approximate as combined with more precise heurstic to estimate cost 
        private static Dictionary<int, int> ZoneTransferTimes = new Dictionary<int, int>
        {
            {1, 2 },
            {2, 3 },
            {3, 4 },
            {4, 5},
            {5, 5},
            {6, 5},
            {7, 6},
            {8, 7},
            {9, 7},
        };
        // certain notable station transfer times for those stations which frequently pop up in journeys through london
        private static Dictionary<string, int> NotableTransfers = new Dictionary<string, int>
        {
            { "Bank", 7 },
            { "Waterloo", 8 },
            { "King's Cross St Pancras", 10 },
            { "Paddington", 6 },
            { "Oxford Circus", 5 },
            { "Tottenham Court Road", 4 },
            { "Liverpool Street", 7 },
            { "Euston", 6 },
            { "Victoria", 8 },
            { "Canary Wharf", 9 }
        };

        // asynchronously populates StationZonePair
        public static async Task GetZoneInfo(JArray naptanarr)
        {
            HttpClient client = new HttpClient();
            string url = "https://api.tfl.gov.uk/StopPoint/";
            // i am using batching and Task.WhenAll to avoid flooding the api with requests
            // with this method i can send batchsize amounts of requests as one request and receive each response
            // we also control the delay between batches to avoid receiving code 429 (too many requests)
            int batchsize = 5;

            for (int i = 0; i < naptanarr.Count; i += batchsize)
            {
                // in each iteration, take 5 elements of the array and create a new enumerable of each token in those 5 elements
                // we can now treat this enumerable as one batch of requests and handle them concurrently using threading
                var batch = naptanarr.Skip(i).Take(batchsize).Select(async token =>
                {
                    HttpResponseMessage response = await client.GetAsync($"{url}{token["naptanID"].ToString()}?app_id={APPID}&app_key={APPKEY}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responsestring = await response.Content.ReadAsStringAsync();
                        var stationdata = JObject.Parse(responsestring);

                        // get the first appearance of the zone property from the JSON
                        var zoneinfo = stationdata["additionalProperties"].FirstOrDefault(p => p["key"]?.ToString() == "Zone");

                        // return new allows me to create an anonymous object to encapsulate the data into one section of memory
                        // this data is also immutable thereafter
                        // also, since we are working with API requests, having type inference by the compiler (basically like using var) makes things more reliable should the response format change
                        return new
                        {
                            // return anonymous object type which corresponds to the KVP in StationZonePair
                            StationName = stationdata["commonName"]?.ToString().Replace(" Underground Station", ""),
                            Zone = zoneinfo != null ? zoneinfo["value"].ToString() : "Zone info not found"
                        };
                    }

                    else
                    {
                        // return null object if response code not 200
                        return new
                        {
                            StationName = token["naptanID"]?.ToString(),
                            Zone = $"Error: {response.StatusCode}"
                        };
                    }

                });

                // store the objects created by each batch to a temporary variable to write into dict
                var results = await Task.WhenAll(batch);
                foreach (var result in results)
                {
                    StationZonePair[result.StationName] = result.Zone;
                }
            }

            Debug.WriteLine("Zones Loaded");

        }

        // gets the zone of a given input
        public static int GetZone(string Station)
        {
            // finds best match of the given input to a known station
            string match = FindBestMatch(Station);
            // accesses zone of station
            string TravelledZone = StationZonePair[match];

            int TravelledZoneInt = 0;
            if (TravelledZone.Length > 1)
            {
                // in the case the station is on a boundary e.g. "2+3" we take the higher zone number - 2
                // this is because A* must not underestimate costs of visiting nodes, so safer to take the larger zone number
                TravelledZoneInt = (int)char.GetNumericValue(TravelledZone[0]);
            }
            else
            {
                TravelledZoneInt = Convert.ToInt32(TravelledZone);
            }
            return TravelledZoneInt;
        }

        // use fuzzy string matching to find the closest station to the given input
        public static string FindBestMatch(string input)
        {
            var BestMatch = FuzzySharp.Process.ExtractOne(input, StationZonePair.Keys.ToList());
            return BestMatch.Value.ToString();
        }

        // returns the weight to add (transfer time approximation)
        public static int AddTransferTime(string TravelledStation)
        {
            // checks if input station is a notable one with custom transfer time
            if (NotableTransfers.ContainsKey(TravelledStation))
            {
                return NotableTransfers[TravelledStation];

            }
            // else return the approximate transfer time
            try
            {
                string TravelledZone = StationZonePair[TravelledStation];
                int TravelledZoneInt = 0;
                if (TravelledZone.Length > 1)
                {
                    TravelledZoneInt = (int)char.GetNumericValue(TravelledZone[0]);
                }
                else
                {
                    TravelledZoneInt = Convert.ToInt32(TravelledZone);
                }
                return ZoneTransferTimes[TravelledZoneInt];
            }
            catch
            {
                return 0;
            }
        }
    }
}


