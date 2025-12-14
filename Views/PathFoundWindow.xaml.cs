using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using System.IO;
using TUBETREKWPFV1.Classes;
using Newtonsoft.Json;
using System.Diagnostics;
using TUBETREKWPFV1.Pathfinding;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

namespace TUBETREKWPFV1.Views
{

    public partial class PathFoundWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer UpdateTimer;
        public List<StationCoords> StationPath {  get; set; }

        public string PeakFare { get; set; }
        public string OffPeakFare {  get; set; }

        public List<string> TimePath {  get; set; } 
        public Dictionary<string, List<string>> StationLineMap { get; set; }

        // private field to store the list of line paths
        private List<string> _linepath;

        // public property to expose linepath list with getter and setter
        public List<string> LinePath
        {
            get { return _linepath; }
            set
            {
                _linepath = value;
                // notifies any UI bindings that the property has changed, triggering updates
                OnPropertyRaised(nameof(LinePath));
            }
        }

        private string StationCoordsJson { get; set; }

        private NextTrain _nextTrain;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyRaised(string propertyname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }

        public NextTrain nextTrain
        {
            get { return _nextTrain; }
            set
            {
                _nextTrain = value;
                // notifies UI of property changing
                OnPropertyRaised(nameof(nextTrain));
            }
        }

        private PathFoundWindow(List<StationCoords> Path, Dictionary<string, List<string>> LinePath, List<string> TimePath) // changed this from just list string
        {
            // setting all routes as instance variables 
            this.StationLineMap = LinePath;
            this.LinePath = new List<string>();
            this.StationPath = Path;
            this.TimePath = TimePath;
            this.nextTrain = new NextTrain();

            this.PeakFare = GetFare(Path.First().Name, Path.Last().Name, "Peak");
            this.OffPeakFare = GetFare(Path.First().Name, Path.Last().Name, "Off Peak");

            InitializeComponent();
            DataContext = this;

            StartTrainUpdateTimer();

            // converting the list to a json string
            StationCoordsJson = JsonConvert.SerializeObject(Path);
            
        }


        // creates and initialises PathFoundWindow (not the constructor as must wait for async operation) 
        public static async Task<PathFoundWindow> AsyncFactory(List<StationCoords> Path, Dictionary<string, List<string>> LinePath, List<string> TimePath)
        {
            PathFoundWindow pathFoundWindow = new PathFoundWindow(Path, LinePath, TimePath);

            await pathFoundWindow.GetNextTrain();
            pathFoundWindow.LinePath = IRLtrains.BuildLinePath(pathFoundWindow.StationLineMap, pathFoundWindow.nextTrain.selected);

            pathFoundWindow.LoadMap();
            return pathFoundWindow;

        }

        // updates the NextTrain when the time at which the previous one arrives has passed
        private void StartTrainUpdateTimer()
        {
            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Interval = TimeSpan.FromSeconds(10);
            UpdateTimer.Tick += (sender, args) =>
            {
                if (nextTrain != null && DateTime.Now >= nextTrain.time)
                {
                    GetNextTrain();
                }
            };
            UpdateTimer.Start();
        }

        // selects fare from db given 'peak' or 'off peak' classification
        private static string GetFare(string Start, string End, string Classification)
        {
            string dbFilePath = "Data Source=TubeTrekker.db";
            string SQL = "";
            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();
                if (Classification == "Peak")
                {
                    SQL = "SELECT Fare.PeakFare FROM UserJourneys JOIN Fare ON UserJourneys.FareID = Fare.FareID WHERE UserJourneys.StartStation = @Start AND UserJourneys.Destination = @End";
                }
                else
                {
                    SQL = "SELECT Fare.OffPeakFare FROM UserJourneys JOIN Fare ON UserJourneys.FareID = Fare.FareID WHERE UserJourneys.StartStation = @Start AND UserJourneys.Destination = @End";
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@Start", Start);
                    cmd.Parameters.AddWithValue("@End", End);

                    return cmd.ExecuteScalar() as string;
                }
            }
        }
        private async Task GetNextTrain()
        {
            // first station name, second station name, list of lines serving the connection
            nextTrain = await IRLtrains.FindNextJourneyOnLine(StationPath[0].Name, StationPath[1].Name, StationLineMap[StationPath[0].Name]);
            OnPropertyRaised($"{nameof(nextTrain)}");
        }
        private async void LoadMap()
        {
            
            // gets the html path by appending the name of the OSM map html file to the base domain of the app
            string htmlpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OSMmap.html");

            // starts a uri with said path
            MapWebView.Source = new Uri(htmlpath);


            
            // asynchronously starts the background process to load the tiles
            await MapWebView.EnsureCoreWebView2Async();

            // navigation completed ensures the html page is fully loaded before attempting to display
            MapWebView.CoreWebView2.NavigationCompleted += async (sender, args) =>
            {
                if (args.IsSuccess)
                {

                    // using the Replace method to handle special characters which would otherwise 
                    string safeJson = StationCoordsJson.Replace("\\", "\\\\").Replace("'", "\\'");

                    // Call the plotRoute function with the station data
                    // need quotation marks around the parameter as the JS function handles it as a string
                    await MapWebView.CoreWebView2.ExecuteScriptAsync($"plotRoute('{safeJson}');");
                }
                else
                {
                    Debug.WriteLine("Failed to load the map.");
                }
            };


        }

        // closes this window and opens a MainWindow instance 
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            this.Close();

        }

    }
}
