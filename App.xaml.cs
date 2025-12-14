using MVVMtutorial.Pathfinding;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;
using System.IO;
using TUBETREKWPFV1.Classes;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.EntityFrameworkCore;

namespace TUBETREKWPFV1
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // the startup method is overriden so i can prepare my graph and station data on startup but retain other nexeccary processes from the base method
            base.OnStartup(e);
            Task.Run(PreloadWV2);

            // ensuring the database for the existing context exists 
            DatabaseFacade Facade = new DatabaseFacade(new UserDataContext());
            Facade.EnsureCreated();

            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            await PrepData();
        }

        // create the WV2 environment on startup to prevent long waiting times in future processes 
        private async Task PreloadWV2()
        {
            var env = await CoreWebView2Environment.CreateAsync();
        }
        private async Task PrepData()
        {
            // create a DataHandler to parse the london.json file into the POCO structure used to build the graph (see DataHandler.cs)
            DataHandler Handler = new DataHandler();
            string ldnstring = File.ReadAllText("london.json");
            ConnectionList v = Handler.PrepJSON(ldnstring);

            // set global graph object's static fields for access anywhere in program 
            GlobalGraph.ConnectionList = v;
            var Graph = Handler.BuildGraphWithLine(v);

            GlobalGraph.Graph = Graph;

            // parse naptan.json file to load zones into dictionary (see TransferTime.cs)
            // done on startup as is a relatively long asynchronous process due to the use of batches and waiting 
            string naptanstring = File.ReadAllText("naptan.json");
            JArray jarr = JArray.Parse(naptanstring);
            await TransferTime.GetZoneInfo(jarr);
            Debug.WriteLine("Zones loaded");
        }
    }
}

