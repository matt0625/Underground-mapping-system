using MVVMtutorial.Pathfinding;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using TUBETREKWPFV1.Classes;
using System.Diagnostics;
using TUBETREKWPFV1.Views;
using System.Windows.Media.Animation;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using TUBETREKWPFV1.Pathfinding;
using FuzzySharp;

namespace TUBETREKWPFV1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> Path {  get; set; }
        private List<Tuple<string ,string, string>> PathWithCoords { get; set; }

        private List<StationCoords> FinalPath { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }


        private async void JourneyFinder_Click(object sender, RoutedEventArgs e)
        {
            var Graph = GlobalGraph.Graph;
            var ConnListStructure = GlobalGraph.ConnectionList;


            string Start = StartingBox.Text;
            string End = DestinationBox.Text;

            // Attempt to match user inputs to keys in the graph (list of acceptable stations)
            string StartStation = FindBestMatch(Start, Graph.Keys.ToList());
            string Destination = FindBestMatch(End, Graph.Keys.ToList());

            if (StartStation == null || Destination == null)
            {
                StartingBox.Clear();
                DestinationBox.Clear();

                StartingBox.Focus();

                MessageBox.Show("We can't find one or more of your stations. Check spelling", "Matching Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                StartingBox.BorderBrush = Brushes.Red;
                DestinationBox.BorderBrush = Brushes.Red;
                return;
            }
            //ComboBoxItem FilterBoxContent = (ComboBoxItem)FilterBox.SelectedItem;
            //string Filter = FilterBoxContent.Content.ToString();


            

            Path = AStar.FindPath(Graph, StartStation, Destination);
            PathWithCoords = AStar.GetPathStationCoordinates(ConnListStructure, Path);
            FinalPath = ConvertToStationObject(PathWithCoords);
            var LinePath = AStar.PairPathToLine(ConnListStructure, Path);
            var TimePath = AStar.PairPathToTime(ConnListStructure, Path);
            TimePath.Add($"{TimePath.Sum(int.Parse)} mins");

            int FareID = await FareGetter.CalculateFare(StartStation, Destination);

            // log journey in userjourney table
            LogJourney(StartStation, Destination, FareID);

            // closing current window after path found to show it on map
            var pathFoundWin = await PathFoundWindow.AsyncFactory(FinalPath, LinePath, TimePath);
            pathFoundWin.Show();

            this.Close();
            
        }

        private static string? FindBestMatch(string input, List<string> parameter)
        {
            // the threshold by which the program determines whether or not the input should be matched to a valid station
            int Threshold = 80;
            var BestMatch = FuzzySharp.Process.ExtractOne(input, parameter);

            if (BestMatch.Score > Threshold)
            {
                return BestMatch.Value.ToString();
            }
            return null;
        }

        // convert the awkward tuple typing into objects that can be serialized for access on the js side (to display these stations on the map)
        private List<StationCoords> ConvertToStationObject(List<Tuple<string ,string,string>> coords)
        {
            var result = new List<StationCoords>();
            foreach (var TupEntry in coords)
            {
                StationCoords tmp = new StationCoords();
                tmp.Name = TupEntry.Item1;
                tmp.Latitude = Convert.ToDouble(TupEntry.Item2);
                tmp.Longitude = Convert.ToDouble(TupEntry.Item3);
                result .Add(tmp);
            }
            return result;
        }

        private void LogJourney(string Start, string Destination, int FareID)
        {
            string dbFilePath = "Data Source=TubeTrekker.db";
            
            DateTime dateTime = DateTime.Now;
            int ThisJourneyID = 0;
            // inserting the journey details into journey table
            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();

                string SQL = "INSERT INTO UserJourneys(DateTime, StartStation, Destination, Journey, FareID) VALUES (@Time, @Start, @Destination, @Journey, @FareID)";
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    string SerialisedPath = JsonSerializer.Serialize(Path);

                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@Time", dateTime);
                    cmd.Parameters.AddWithValue("@Start", Start);
                    cmd.Parameters.AddWithValue("@Destination", Destination);
                    cmd.Parameters.AddWithValue("@Journey", SerialisedPath);
                    cmd.Parameters.AddWithValue("@FareID", FareID);

                    cmd.ExecuteNonQuery();

                }


                // a further command in the same connection to retrieve the most recently used journeyID, needed for the adjoining table 
                // must be in same connection as last_insert_rowid is connection specific
                using (SqliteCommand cmd =conn.CreateCommand())
                {
                    
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    ThisJourneyID = Convert.ToInt32(cmd.ExecuteScalar());
                }
                
                conn.Close();
            }

            // inserting necessary fields into Links table to avoid many to many relationships
            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();

                string SQL = "INSERT INTO Links(UserID, JourneyID) VALUES (@UserID, @JourneyID)";
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@UserID", UserSession.UserID);
                    cmd.Parameters.AddWithValue("@JourneyID", ThisJourneyID);

                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }

        }
              
        private void RecentJourneysButton_Click(object sender, RoutedEventArgs e)
        {
            RecentJourneyWindow recentJourneyWindow = new RecentJourneyWindow();
            recentJourneyWindow.Show();

            this.Close();
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            UserSession.UserID = 0;
            UserSession.Username = null;

            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            this.Close();
        }
    }
}