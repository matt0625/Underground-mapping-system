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
using Microsoft.Data.Sqlite;
using System.Windows.Controls.Primitives;
using MVVMtutorial.Pathfinding;
using TUBETREKWPFV1.Resources.Helpers;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace TUBETREKWPFV1.Views
{
    public partial class RecentJourneyWindow : Window, INotifyPropertyChanged
    {
        // using ObservableCollection as the property is regularly updated
        // ObservableCollection is a dynamic structure native to WPF and tailored for XAML UI visibility 
        // in this case it stores a collection of UserJourneyRetrieved objects 
        private ObservableCollection<UserJourneyRetrieved> _userJourneys;
        public ObservableCollection<UserJourneyRetrieved> UserJourneys
        {
            get { return _userJourneys; }
            set
            {
                _userJourneys = value;
                OnPropertyRaised(nameof(UserJourneys));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyRaised(string propertyname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
        public List<string> PathList { get; set; }
        public RecentJourneyWindow()
        {

            InitializeComponent();
            LoadPlainMap();

            this.UserJourneys = new ObservableCollection<UserJourneyRetrieved>();
            QueryRecent();

            SortJourneysByRecent();

            DataContext = this;

        }

        private async void LoadPlainMap()
        {
            // see PathFoundWindow for explanation of following logic
            // gets the html path by appending the name of the OSM map html file to the base domain of the app
            string htmlpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecentJourneyMap.html");

            // starts a uri with said path
            MapWebView.Source = new Uri(htmlpath);

            // asynchronously starts the background process to load the tiles
            await MapWebView.EnsureCoreWebView2Async();
        }

        private void QueryRecent()
        {
            string dbFilePath = "Data Source=TubeTrekker.db";

            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();

                // this query joins the Links and UserJourneys tables on the JourneyID columns
                // allows for seamless querying of all journeys where the UserID matches the session's
                string SQL = "SELECT DateTime, StartStation, Destination, Journey FROM UserJourneys JOIN Links USING (JourneyID) WHERE Links.UserID = (@UserID)";

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@UserID", UserSession.UserID);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        // getting journey in the specific format i want to display it in the XAML
                        PathList = JsonConvert.DeserializeObject<List<string>>(reader.GetValue(3) as string);
                        string JourneyStr = string.Join("->", PathList);
                        this.UserJourneys.Add(new UserJourneyRetrieved() { DateTime = Convert.ToDateTime(reader.GetValue(0)), StartStation = (string)reader.GetValue(1), Destination = (string)reader.GetValue(2), Journey = JourneyStr, PathList = this.PathList });
                    }

                }
                conn.Close();
            }

        }

        // converts ObservableCollection to list and sorts, then creates a new ObservableCollection with list contents 
        // final output: the observable collection of recent journeys with the most recent first 
        private void SortJourneysByRecent()
        {
            var SortedList = MergeSort.Mergesort(this.UserJourneys.ToList());
            SortedList.Reverse();
            UserJourneys.Clear();
            foreach (var journey in SortedList)
            {
                UserJourneys.Add(journey);
            }
            OnPropertyRaised($"{nameof(UserJourneys)}");

        }

        private async void JourneyDisplay_Expanded(object sender, RoutedEventArgs e)
        {

            List<string> JourneyAsList = new List<string>();
            // we can't directly access the expander from its x:Name as it is inside a listview
            // luckily, as the expander is the sender of the event we can grab its contents with a simple type cast
            var expander = (Expander)sender;
            // the content of the expander is an itemscontrol with X items
            // i need this as a serialised json list to access and plot the data

            // will always be true as that is the structure of the xaml (expanders content is an itemscontrol, whose source is the list)
            if (expander.Content is ItemsControl itemsControl)
            {
                JourneyAsList = (List<string>)itemsControl.ItemsSource;
            }

            var ConnectionList = GlobalGraph.ConnectionList;
            var stationCoords = AStar.GetPathStationCoordinates(ConnectionList, JourneyAsList);
            List<StationCoords> FinalResult = ConvertToStationObject(stationCoords);

            string SerialisedJourney = JsonConvert.SerializeObject(FinalResult);
            // ensuring escape characters in the JSONstring are handled accordingly
            string safeJson = SerialisedJourney.Replace("\\", "\\\\").Replace("'", "\\'");
            // Call the plotRoute function with the station data
            // need quotation marks around the parameter as the JS function handles it as a string
            await MapWebView.CoreWebView2.ExecuteScriptAsync($"plotExpandedRoute('{safeJson}');");


        }

        // converts tuple type to station coords object for more intuitive accessing and storing
        private List<StationCoords> ConvertToStationObject(List<Tuple<string, string, string>> coords)
        {
            var result = new List<StationCoords>();
            foreach (var TupEntry in coords)
            {
                StationCoords tmp = new StationCoords();
                tmp.Name = TupEntry.Item1;
                tmp.Latitude = Convert.ToDouble(TupEntry.Item2);
                tmp.Longitude = Convert.ToDouble(TupEntry.Item3);
                result.Add(tmp);
            }
            return result;
        }

        // reverses the most recent sorting of the journeys 
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var reversedList = UserJourneys.Reverse().ToList();
            UserJourneys = new ObservableCollection<UserJourneyRetrieved>(reversedList);
            OnPropertyRaised(nameof(UserJourneys));
        }


        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
    
}
