using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;
using SQLite;
using Android.Locations;

namespace Parky
{
    [Activity(Label = "MapActivity")]
    public class MapActivity : Activity, IOnMapReadyCallback, ILocationListener
    {
        GoogleMap map;
        Button parkButton;
        SQLiteConnection db;
        List<LatLng> markersList;
        string dbPath;
        LatLng position;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.map_layout);

            // Location
            LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
            string provider = lm.GetBestProvider(new Criteria(), true);
            Location location = lm.GetLastKnownLocation(provider);
            lm.RequestLocationUpdates(LocationManager.GpsProvider, 2000, 1, this);

            // Map
            MapFragment _mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
            if (_mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeZoomControlsEnabled(false)
                    .InvokeCompassEnabled(true);

                FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                _mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, _mapFragment, "map");
                fragTx.Commit();
            }
            _mapFragment.GetMapAsync(this);

            // Park
            parkButton = FindViewById<Button>(Resource.Id.parkButton);
            parkButton.Click += delegate
            {
                foreach (var marker in markersList)
                {
                    db.Insert(new Vehicle() { Lat = marker.Latitude, Lng = marker.Longitude });
                }
                markersList.Clear();

                var intent = new Intent(this, typeof(ParkedActivity));
                StartActivity(intent);
            };

            // Database
            dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "ParkyDatabase.db3");
            db = new SQLiteConnection(dbPath);
            db.CreateTable<Vehicle>();

            markersList = new List<LatLng>();
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            map = googleMap;
            map.MyLocationEnabled = true;
            map.UiSettings.MyLocationButtonEnabled = true;
            map.UiSettings.CompassEnabled = true;

            // Loading saved vehicles
            var table = db.Table<Vehicle>();
            foreach (var vehicle in table)
            {
                MarkerOptions markerOptions = new MarkerOptions()
                    .SetPosition(new LatLng(vehicle.Lat, vehicle.Lng))
                    .SetTitle("Opgeslagen voertuig")
                    .SetSnippet($"Lat {vehicle.Lat} Long {vehicle.Lng}")
                    .Draggable(false);
                map.AddMarker(markerOptions);

                CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(vehicle.Lat, vehicle.Lng), 17);
                map.MoveCamera(cameraUpdate);
            }

            map.MapLongClick += _map_MapLongClick;
        }

        private void _map_MapLongClick(object sender, GoogleMap.MapLongClickEventArgs e)
        {
            MarkerOptions markerOptions = new MarkerOptions()
                .SetPosition(e.Point)
                .SetTitle("Hier staat je voertuig")
                .SetSnippet($"Lat {e.Point.Latitude} Long {e.Point.Longitude}")
                .Draggable(true);
            map.AddMarker(markerOptions);

            markersList.Add(e.Point);
        }

        public void OnLocationChanged(Location location)
        {
            position = new LatLng(location.Latitude, location.Longitude);
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
        }
    }
}