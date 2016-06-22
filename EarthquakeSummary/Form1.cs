using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Timers;
using System.Configuration;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Microsoft.VisualBasic.FileIO;
using KdTree;
using KdTree.Math;

namespace EarthquakeSummary
{
    public partial class Form1 : Form
    {
        string USGSGeoJsonEarthQuakeSummaryFeedUrl;
        string WorldCitiesFilepath;

        public enum StatusEnum
        {
            GET_SUMMARY_FEED_FAILED = 0,
            PARSE_JSON_FAILED,
            SUCCESS
        }

        private delegate void UpdateListDelegate(EarthQuakeSummary alerts);
        private delegate void UpdateStatusDelegate(string status);

        ManualResetEvent mKdTreeDoneEvent = new ManualResetEvent(false);
        System.Timers.Timer mUpdateTimer = new System.Timers.Timer();
        private KdTree<float, string> mWorldCitiesTree;

        public Form1()
        {
            InitializeComponent();
            Init();
            new Thread(GetEarthQuakeFeeds).Start(); //get the first USGS feed
            new Thread(InitWorldCitiesLookup).Start(); //initialize the lookup table (kd-tree)
            mUpdateTimer.Elapsed += new ElapsedEventHandler(OnTimerEvent);
            mUpdateTimer.Interval = 3600000; //one hour interval
            mUpdateTimer.Enabled = true;
        }

        private void Init()
        {
            listBox1.Items.Clear();
            UpdateStatus("Initializing the earthquake update system. Please wait...");
            USGSGeoJsonEarthQuakeSummaryFeedUrl = ConfigurationManager.AppSettings.Get("USGSFeedUrl");
            WorldCitiesFilepath = ConfigurationManager.AppSettings.Get("WorldCitiesFile");
            if (!System.IO.File.Exists(WorldCitiesFilepath))
            {
                string msg = String.Format("{0} does not exist.", WorldCitiesFilepath);
                MessageBox.Show(msg, "USGS Earthquake Summary", MessageBoxButtons.OK);
                Close();
            }
        }

        //Download the USGS Json earthquake summary feeds and deserialize it
        private T DownloadAndDeserializedJsonData<T>(string url, ref StatusEnum status) where T : new()
        {
            status = StatusEnum.SUCCESS;
            using (var wc = new WebClient())
            {
                var jsonData = string.Empty;
                try
                {
                    jsonData = wc.DownloadString(url);
                }
                catch (Exception) 
                {
                    UpdateStatus("Failed getting earthquake feed.");
                    status = StatusEnum.GET_SUMMARY_FEED_FAILED;
                }

                if (!string.IsNullOrEmpty(jsonData))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(jsonData);
                    }
                    catch (Exception)
                    {
                        UpdateStatus("Failed parsing earthquake feed.");
                        status = StatusEnum.PARSE_JSON_FAILED;
                    }
                }
                else
                {
                    status = StatusEnum.GET_SUMMARY_FEED_FAILED;
                }
            }
            return new T();
        }

        private void UpdateEarthquakeList(EarthQuakeSummary alerts)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new UpdateListDelegate(UpdateEarthquakeList), new object[] { alerts });
            }
            else
            {
                AddListboxItems(alerts);
            }
        }

        private void UpdateStatus(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new UpdateStatusDelegate(UpdateStatus), new object[] { status });
            }
            else
            {
                toolStripStatusLabel1.Text = status;
            }
        }

        private void AddListboxItems(EarthQuakeSummary alerts)
        {
            const string header = "Total number of earthquakes in the past hour";
            const string doubleSeparator = "=================================================================";
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            string locs = String.Format(": {0} locations", alerts.features.Length);
            if (alerts.features.Length > 0)
            {
                listBox1.Items.Add(header + locs);
                listBox1.Items.Add("  ");
                listBox1.Items.Add(doubleSeparator);
                int i = 1;
                foreach (Feature feature in alerts.features)
                {
                    string place = feature.properties.place;
                    float mag = feature.properties.mag;
                    long time = feature.properties.time;
                    DateTime t = epoch.AddMilliseconds(time).ToLocalTime();
                    float longitude = feature.geometry.coordinates[0];
                    float latitude = feature.geometry.coordinates[1];
                    float depth = feature.geometry.coordinates[2];
                    List<KdTreeNode<float, string>> cityNodes = new List<KdTreeNode<float, string>>();
                    GetNearestCities(cityNodes, latitude, longitude, 3);
                    string summary = String.Format("Location {3}: {0},  magnitude: {1},  local time: {2}", place, mag, t.ToString(), i++);
                    listBox1.Items.Add(summary);
                    string geomDetails = String.Format("\tlatitude:{1},  longitude:{0},  depth:{2} km", longitude, latitude, depth);
                    listBox1.Items.Add(geomDetails);
                    int j = 1;
                    foreach (KdTreeNode<float, string> node in cityNodes)
                    {
                        string nearbyCity = String.Format("    nearby city {3}:{2},  latitude:{1},  longitude:{0}", node.Point[0], node.Point[1], node.Value, j++);
                        listBox1.Items.Add(nearbyCity);
                    }
                    listBox1.Items.Add("  ");
                    listBox1.Items.Add(doubleSeparator);
                }
            }
            else
            {
                UpdateStatus("No earthquake reported in the past hour.");
            }
        }

        //Get USGS earthquake feeds in a worker thread
        private void GetEarthQuakeFeeds()
        {
            StatusEnum status = StatusEnum.SUCCESS;
            var summary = DownloadAndDeserializedJsonData<EarthQuakeSummary>(USGSGeoJsonEarthQuakeSummaryFeedUrl, ref status);
            if (status != StatusEnum.SUCCESS)
                return;
            mKdTreeDoneEvent.WaitOne(); //Wait until the kd tree is built. Only needs to wait for the first time.
            UpdateEarthquakeList(summary);
            string statusText = String.Format("Done updating earthquake feeds of the past hour. USGS feeds query time: {0}", DateTime.Now.ToString());
            UpdateStatus(statusText);
        }

        //Read world cities csv file and build 2D kd-tree with (longitude, latitude) coordinate pairs in a worker thread
        private bool BuildKdTree(string worldCitiesPath, KdTree<float, string> tree)
        {
            bool result = true;
            TextFieldParser parser = new TextFieldParser(worldCitiesPath);
            string[] fields;

            string line = parser.ReadLine();

            try
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                while (!parser.EndOfData)
                {
                    fields = parser.ReadFields();
                    string language = fields[5];
                    string country = fields[0];
                    string name = fields[6];
                    double latitude, longitude;
                    double value;
                    if (Double.TryParse(fields[7], out value))
                    {
                        latitude = value;
                    }
                    else
                        continue;
                    if (Double.TryParse(fields[8], out value))
                    {
                        longitude = value;
                    }
                    else
                        continue;
                    tree.Add(new float[] { (float)longitude, (float)latitude }, country + "-" + name);
                }

                parser.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                result = false;
            }

            return result;
        }

        //Initialize the world cities lookup table (kd-tree)
        private void InitWorldCitiesLookup()
        {
            mWorldCitiesTree = new KdTree<float, string>(2, new FloatMath(), AddDuplicateBehavior.Update);
            if (BuildKdTree(WorldCitiesFilepath, mWorldCitiesTree))
                mKdTreeDoneEvent.Set();
        }

        //Find the nearest cities by longitude and latitude.
        public void GetNearestCities(List<KdTreeNode<float, string>> cityNodes, float latitude, float longitude, int numCities)
        {
            var neighbourCities = mWorldCitiesTree.GetNearestNeighbours(
                new float[] { longitude, latitude },
                numCities);
            foreach (var city in neighbourCities)
            {
                cityNodes.Add(city);
            }
        }

        //Update earthquake status every one hour
        private void OnTimerEvent(object source, ElapsedEventArgs e)
        {
            GetEarthQuakeFeeds();
        }
    }
}
