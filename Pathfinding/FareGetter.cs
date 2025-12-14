using Microsoft.Data.Sqlite;
using MVVMtutorial.Pathfinding;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace TUBETREKWPFV1.Pathfinding
{
    public class FareGetter
    {
        // returns an integer value correlating to the ID of the fares calculated in the DB 
        public static async Task<int> CalculateFare(string Start, string Destination)
        {
            // retrieve naptanIDs as TFL API uses naptanID in query
            string StartNaptan = IRLtrains.GetNaptanID(Start);
            string EndNaptan = IRLtrains.GetNaptanID(Destination);

            string revisedurl = "https://api.tfl.gov.uk/Stoppoint/"; 
            revisedurl += (StartNaptan + "/FareTo/" + EndNaptan);

            using (HttpClient  client = new HttpClient())
            {
                var response = await client.GetStringAsync(revisedurl);
                JArray obj = JArray.Parse(response);
                
                // access relevant area of JObj and parse into JArr
                var AllFares = obj[0]["rows"][0]["ticketsAvailable"] as JArray;

                // extract OffPeakFare and PeakFare by KVP reference
                var OffPeakFare = $"£{AllFares.FirstOrDefault(t => t["ticketType"]["type"].ToString().Trim() == "Pay as you go" && t["ticketTime"]["type"].ToString().Trim() == "Off Peak")["cost"].ToString()}";
                var PeakFare = $"£{AllFares.FirstOrDefault(t => t["ticketType"]["type"].ToString().Trim() == "Pay as you go" && t["ticketTime"]["type"].ToString().Trim() == "Peak")["cost"].ToString()}";

                return InsertFare(Start, Destination, PeakFare, OffPeakFare);

            }
        }

        private static int InsertFare(string Start, string Destination, string Peak, string OffPeak)
        {
            string datasource = "Data Source = TubeTrekker.db";
            // fareID = -1 if no fare found
            int FareId = -1;
                
            using (var conn = new SqliteConnection(datasource))
            {
                conn.Open();
                // inserts fare calculated for given zone pairing if it doesn't exist already
                string SQL = @"INSERT INTO Fare (ZoneStart, ZoneEnd, PeakFare, OffPeakFare) SELECT @Start, @End, @Peak, @Off WHERE NOT EXISTS (SELECT 1 FROM Fare WHERE ZoneStart = @Start AND ZoneEnd = @End AND PeakFare = @Peak AND OffPeakFare = @Off); 
                                SELECT FareID FROM Fare WHERE ZoneStart = @Start AND ZoneEnd = @End AND PeakFare = @Peak AND OffPeakFare = @Off;";


                using (var cmd =  conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@Start", TransferTime.GetZone(Start));
                    cmd.Parameters.AddWithValue("@End", TransferTime.GetZone(Destination));
                    cmd.Parameters.AddWithValue("@Peak", Peak);
                    cmd.Parameters.AddWithValue("@Off", OffPeak);

                    object result = cmd.ExecuteScalar();
                    // get FareID
                    if (result != null)
                    {
                        FareId = (int)(long)result;
                    }
                }
            }
            return FareId;
        }
    }
}
