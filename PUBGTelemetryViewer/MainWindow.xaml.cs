﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using BruTile;
using BruTile.Cache;
using BruTile.UI;
using BruTile.UI.Windows;

//PubgLibary Using
using PUBGLibrary;
using PUBGLibrary.API;

namespace PUBGTelemetryViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static APITelemetry telemetryData;
        public static string apiKey;
        public TileSource tileSource;
        string appdir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            LoadMap(0);




        }

        public void LoadMap(int mapid)
        {
            if (mapid == 0)
                tileSource = new TileSource(new FileTileProvider(new FileCache(appdir + "\\maps\\Erangel", "png")), new TileSchema(mapid));
            else if (mapid == 1)
                tileSource = new TileSource(new FileTileProvider(new FileCache(appdir + "\\maps\\Miramar", "png")), new TileSchema(mapid));
            map.RootLayer = new TileLayer(tileSource);
            InitializeTransform(tileSource.Schema);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("./apikey.txt"))
                apiKey = File.ReadAllText("./apikey.txt");
            else
            {
                MessageBoxResult response = MessageBox.Show(Directory.GetCurrentDirectory() + @"\apikey.txt was not found!" + Environment.NewLine + "Create one?", "Error!", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.Yes)
                {
                    Settings addAPIKey = new Settings();
                    addAPIKey.ShowDialog();
                    apiKey = File.ReadAllText(Directory.GetCurrentDirectory() + @"\apikey.txt");
                }
                else
                {
                    loadButton_Copy.IsEnabled = false;
                }
            }
            LoadIcons();
            map.Refresh();

        }

        private void LoadIcons()
        {
            AddIcon(map.MarkerImages, -1, "noicon");
            AddIcon(map.MarkerImages, 0, "black");
            AddIcon(map.MarkerImages, 1, "plane");
            AddIcon(map.MarkerImages, 2, "leave-car");
            AddIcon(map.MarkerImages, 3, "leave-parachute");
            AddIcon(map.MarkerImages, 4, "boost");
            AddIcon(map.MarkerImages, 5, "carepackage");
            AddIcon(map.MarkerImages, 6, "death");
            AddIcon(map.MarkerImages, 7, "heal");
            AddIcon(map.MarkerImages, 8, "kill");
            AddIcon(map.MarkerImages, 9, "take-dmg");
        }

        private void InitializeTransform(TileSchema schema)
        {
            map.Transform.Center = new Point(16384d, -16384d);
            map.Transform.Resolution = schema.Resolutions[2];
            schema.Resolutions.Add(32);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (map.Transform.Resolution < 512)
            {
                map.Transform.Resolution *= 2;
                map.Refresh();
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (map.Transform.Resolution > 0.125)
            {
                map.Transform.Resolution /= 2;
                map.Refresh();
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               // BruTile.UI.Ellipse ellipse = new BruTile.UI.Ellipse(0, 17525, -17584, 2002, true, BruTile.UI.Ellipse.ZoneType.Red_Zone, 200);
              //  map.RootLayer.ellipsesCache.Add(ellipse);
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "PUBG Telemetry JSON file (.json)|*.json";
                if (dlg.ShowDialog() == true)
                {
                    loadSavedInfo(dlg.FileName);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            map.Refresh();
        }

        private void loadSavedInfo(string path)
        {
            map.ClearMarkers();
            
            List<string> iconNames = new List<string>();

            try
            {
                string json = File.ReadAllText(path);
                APIRequest pIRequest = new APIRequest();

                telemetryData = pIRequest.TelemetryPhraser(json);
                LoadMap(findMap(telemetryData.LogMatchStart.MapName));
                LoadIcons();
                DisplayName(telemetryData.LogMatchStart.PlayerList);
               

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            map.Refresh();

        }

        private int findMap(string mapname)
        {
            if (mapname == "Erangel_Main")
                return 0;
            else if (mapname == "Desert_Main")
                return 1;
            return -1;

        }

        private void LoadEllipses(List<LogGameStatePeriodic> logGameStatePeriodicList)
        {
            
            foreach (var i in logGameStatePeriodicList)
            {
                //if (!i.GameState.BlueZone.Exists() && !i.GameState.RedZone.Exists())
                //    continue;
                BruTile.UI.Ellipse Bellipse = new BruTile.UI.Ellipse(i.GameState.ElapsedTime,i.GameState.BlueZone.X, i.GameState.BlueZone.Y, i.GameState.BlueZone.Radius, true, BruTile.UI.Ellipse.ZoneType.Blue_Zone, 200);
                BruTile.UI.Ellipse Rellipse = new BruTile.UI.Ellipse(i.GameState.ElapsedTime, i.GameState.RedZone.X, i.GameState.RedZone.Y, i.GameState.RedZone.Radius, true, BruTile.UI.Ellipse.ZoneType.Red_Zone, 200);
                map.RootLayer.ellipsesCache.Add(Bellipse);
                map.RootLayer.ellipsesCache.Add(Rellipse);
            }
        }

        private void DisplayName(List<Player> player)
        {
            playerlist.Items.Clear();
            var player2 = player.OrderBy(x => x.PUBGName);
            foreach(Player p in player2)
            {
                playerlist.Items.Add(p.PUBGName);
            }
        }

        private void loadMarkers(PlayerSpecificLog list, List<Marker> marker)
        {
            try
            {
                foreach(var pos in list.LogPlayerPositionList)
                {
                    Marker m = new Marker(pos.LoggedPlayer.Location.X, pos.LoggedPlayer.Location.Y, true, -1, pos.DateTimeOffset, pos.ElapsedTime, "Position", "test", 200);
                    marker.Add(m);
                }

                foreach (var pos in list.LogVehicleLeaveList)
                {
                    int picture = -1;
                    switch (pos.Vehicle.vehicleID)
                    {
                        case VehicleId.DummyTransportAircraft_C:
                            picture = 1;
                            break;
                        case VehicleId.ParachutePlayer_C:
                            picture = 3;
                            break;

                        default:
                            picture = 2;
                            break;
                    }
                    if (picture == -1)
                        continue;
                    Marker m = null;
                    if(picture == 1)
                       m = new Marker(pos.Player.Location.X, pos.Player.Location.Y, true, picture, pos.DateTimeOffset, "Leaving Plane", "user left plane for parachuting", 200, Marker.Eventtype.Plane_leaving);
                    else if(picture == 3)
                        m = new Marker(pos.Player.Location.X, pos.Player.Location.Y, true, picture, pos.DateTimeOffset, "Leaving Parachuting", "user left Parachute for walking", 200, Marker.Eventtype.Plane_leaving);
                    else if(picture == 2)
                        m = new Marker(pos.Player.Location.X, pos.Player.Location.Y, true, picture, pos.DateTimeOffset, "Leaving Vehicle", "user left vehicle for walking", 200, Marker.Eventtype.Plane_leaving);
                    marker.Add(m);
                }

                foreach (var item in list.LogItemUseList)
                {
                    if (item.UsedItem.ItemID.Contains("Item_Boost_"))
                    {
                        Marker m = new Marker(item.Player.Location.X, item.Player.Location.Y, true, 4, item.DateTimeOffset, "Player used boost item" + item.UsedItem.ItemID, "player used boost item", 200, Marker.Eventtype.Boosting);
                        marker.Add(m);
                    }
                    else if(item.UsedItem.ItemID.Contains("Item_Heal_"))
                    {
                        Marker m = new Marker(item.Player.Location.X, item.Player.Location.Y, true, 7, item.DateTimeOffset, "Player used heal item : " + item.UsedItem.ItemID, "player used boost item", 200, Marker.Eventtype.Heal);
                        marker.Add(m);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void loadEvents(PlayerSpecificLog list, List<Marker> markers)
        {
            try
            {

                foreach (var pos in list.LogVehicleLeaveList)
                {
                    int picture = -1;
                    switch (pos.Vehicle.vehicleID)
                    {
                        case VehicleId.DummyTransportAircraft_C:
                            picture = 1;
                            break;
                        case VehicleId.ParachutePlayer_C:
                            picture = 3;
                            break;

                        default:
                            picture = 2;
                            break;
                    }
                    if (picture == -1)
                        continue;
                    Marker m = null;
                    if (picture == 1)
                        m = new Marker(pos.Player.Location.X, pos.Player.Location.Y, true, picture, pos.DateTimeOffset, "Leaving Plane", "user left plane for parachuting", 200, Marker.Eventtype.Plane_leaving);
                    else if (picture == 3)
                        m = new Marker(pos.Player.Location.X, pos.Player.Location.Y, true, picture, pos.DateTimeOffset, "Leaving Parachuting", "user left Parachute for walking", 200, Marker.Eventtype.Plane_leaving);
                    else if (picture == 2)
                        m = new Marker(pos.Player.Location.X, pos.Player.Location.Y, true, picture, pos.DateTimeOffset, "Leaving Vehicle", "user left vehicle for walking", 200, Marker.Eventtype.Plane_leaving);
                    markers.Add(m);
                }

                foreach (var item in list.LogItemUseList)
                {
                    if (item.UsedItem.ItemID.Contains("Item_Boost_"))
                    {
                        Marker m = new Marker(item.Player.Location.X, item.Player.Location.Y, true, 4, item.DateTimeOffset, "Player used boost item" + item.UsedItem.ItemID, "player used boost item", 200, Marker.Eventtype.Boosting);
                        markers.Add(m);
                    }
                    else if (item.UsedItem.ItemID.Contains("Item_Heal_"))
                    {
                        Marker m = new Marker(item.Player.Location.X, item.Player.Location.Y, true, 7, item.DateTimeOffset, "Player used heal item : " + item.UsedItem.ItemID, "player used boost item", 200, Marker.Eventtype.Heal);
                        markers.Add(m);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void AddIcon(Dictionary<int, ImageSource> list, int index, string name)
        {
            string appdir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            string url = System.IO.Path.Combine(System.IO.Path.Combine(appdir, "Icons"), string.Format("{0}.png", name));
            bi.UriSource = new Uri(url);
            bi.EndInit();
            list.Add(index, bi);
        }

        private void playerlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            map.ClearMarkers();
            if (playerlist.SelectedValue == null)
                return;
            var selectedplayer = playerlist.SelectedValue.ToString();
            var playerdat = telemetryData.GetPlayerSpecificLog(selectedplayer, SearchType.PUBGName);

            ConstructTimeline(playerdat);
            loadMarkers(playerdat, map.RootLayer.MarkerCache);
            loadEvents(playerdat, map.RootLayer.EventCache);
            map.Refresh();
        }


        private void ConstructTimeline(PlayerSpecificLog player)
        {

            slider_time.Maximum = Convert.ToInt32(player.LogPlayerLogoutList[0].DateTimeOffset.Subtract(player.LogPlayerCreateList[0].Date).TotalSeconds);
            slider_time.Value = slider_time.Maximum;
            MapControl.matchStartTime = GetMatchStartTime(player.LogPlayerCreateList);
            MapControl.PlaneDepartureTime = GetPlaneDeparture(telemetryData);
        }

        private DateTimeOffset GetMatchStartTime(List<LogPlayerCreate> playerCreate)
        {
            return playerCreate[0].Date;
        }

        private DateTimeOffset GetPlaneDeparture(APITelemetry logMatches)
        {
            return logMatches.LogMatchStart.DateTimeOffset;
        }

        private void cb_warmuplog_Checked(object sender, RoutedEventArgs e)
        {
                MapControl.warmuplog = true;
                map.Refresh();
        }

        private void cb_warmuplog_Unchecked(object sender, RoutedEventArgs e)
        {
            MapControl.warmuplog = false;
            map.Refresh();
        }

        private void btn_settings_Click(object sender, RoutedEventArgs e)
        {
            Settings set = new Settings();
            set.Show();
        }

        private void loadButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            TelemetrySelect telemetrySelect = new TelemetrySelect();
            telemetrySelect.ShowDialog();

            if(telemetrySelect.DialogResult.HasValue && telemetrySelect.DialogResult.Value)
            {
                map.ClearMarkers();
                LoadMap(findMap(telemetryData.LogMatchStart.MapName));
                DisplayName(telemetryData.LogMatchStart.PlayerList);
                LoadIcons();
                map.Refresh();
            }
            else
            {
                Console.WriteLine("Abandoned");
            }
                
        }

        private void slider_time_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double newvalue = Convert.ToInt32(e.NewValue);
            if(timeline_label != null)
                timeline_label.Content = newvalue + " / " + slider_time.Maximum;
            MapControl.currentMatchTime = MapControl.matchStartTime.AddSeconds(newvalue);
            map.Refresh();
            
        }
    }
}
