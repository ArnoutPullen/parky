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
using Plugin.Geolocator;
using Plugin.CurrentActivity;
using System.Threading.Tasks;
using Android.Gms.Location;
using Android.Gms.Common.Apis;
using Android.Support.V4.App;
using Android;
using Android.Content.PM;
using Android.Gms.Common;
using static Android.Gms.Common.Apis.GoogleApiClient;

namespace Parky
{
    [Activity(Label = "MapActivity")]
    public class MapActivity : Activity, 
        IOnMapReadyCallback,
        IConnectionCallbacks, 
        IOnConnectionFailedListener, 
        Android.Gms.Location.ILocationListener
    {
        GoogleMap map;
        Button parkButton;
        SQLiteConnection db;
        List<LatLng> markersList;
        string dbPath;
        LatLng myPosition;
        TextView textView;
        private const int MY_PERMISSION_REQUEST_CODE = 7171;
        private const int PLAY_SERVICES_RESOLUTION_REQUEST = 7172;
        private bool mRequestingLocationUpdates = false;
        private LocationRequest mLocationRequest;
        private GoogleApiClient mGoogleApiClient;
        private Location mLastLocation;
        private static int UPDATE_INTERVAL = 5000; //sec
        private static int FATEST_INTERVAL = 3000; //sec
        private static int DISPLACAEMENT = 10; //METER
        Button tButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Find views
            SetContentView(Resource.Layout.map_layout);
            textView = FindViewById<TextView>(Resource.Id.textView1);
            tButton = FindViewById<Button>(Resource.Id.trackingButton);
            parkButton = FindViewById<Button>(Resource.Id.parkButton);

            // Permissions check
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted
                && ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] {
                    Manifest.Permission.AccessCoarseLocation,
                    Manifest.Permission.AccessFineLocation
                }, MY_PERMISSION_REQUEST_CODE);
            }
            else
            {
                if (CheckPlayServices())
                {
                    BuildGoogleApiClient();
                    CreateLocationRequest();
                }
            }

            // Map
            MapFragment _mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
            if (_mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeZoomControlsEnabled(false)
                    .InvokeCompassEnabled(true);

                Android.App.FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                _mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, _mapFragment, "map");
                fragTx.Commit();
            }
            _mapFragment.GetMapAsync(this);

            // Parkbutton
            parkButton.Click += delegate
            {
                foreach (var marker in markersList)
                {
                    db.Insert(new Vehicle() { Lat = marker.Latitude, Lng = marker.Longitude });
                }
                markersList.Clear();
                map.Clear();
                var intent = new Intent(this, typeof(ParkedActivity));
                StartActivity(intent);
            };

            // Database
            dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "ParkyDatabase.db3");
            db = new SQLiteConnection(dbPath);
            db.CreateTable<Vehicle>();
            markersList = new List<LatLng>();




            tButton.Click += delegate
            {
                TogglePeriodicLocationUpdates();
            };
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            switch (requestCode)
            {
                case MY_PERMISSION_REQUEST_CODE:
                    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    {
                        if (CheckPlayServices())
                        {
                            BuildGoogleApiClient();
                            CreateLocationRequest();
                        }
                    }
                    break;
            }
        }

        private void TogglePeriodicLocationUpdates()
        {
            if (!mRequestingLocationUpdates)
            {
                tButton.Text = "Stop";
                mRequestingLocationUpdates = true;
                StartLocationUpdates();

            }
            else
            {
                tButton.Text = "Track";
                mRequestingLocationUpdates = false;
                StopLocationUpdates();
            }
        }

        private void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest()
                .SetInterval(UPDATE_INTERVAL)
                .SetFastestInterval(FATEST_INTERVAL)
                .SetPriority(LocationRequest.PriorityHighAccuracy)
                .SetSmallestDisplacement(DISPLACAEMENT);
        }

        private void BuildGoogleApiClient()
        {
            mGoogleApiClient = new GoogleApiClient.Builder(this)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .AddApi(LocationServices.API).Build();
            mGoogleApiClient.Connect();
        }

        private bool CheckPlayServices()
        {
            int resultCode = GooglePlayServicesUtil.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GooglePlayServicesUtil.IsUserRecoverableError(resultCode))
                {
                    GooglePlayServicesUtil.GetErrorDialog(resultCode, this, PLAY_SERVICES_RESOLUTION_REQUEST).Show();

                }
                else
                {
                    Toast.MakeText(ApplicationContext, "This device does not support Google Play Services", ToastLength.Long).Show();
                    Finish();
                }
                return false;
            }
            return true;
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



        public void OnConnected(Bundle connectionHint)
        {
            DisplayLocation();
            if (mRequestingLocationUpdates)
            {
                StartLocationUpdates();
            }
        }

        private void StartLocationUpdates()
        {
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted
                && ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                return;
            }
            LocationServices.FusedLocationApi.RequestLocationUpdates(mGoogleApiClient, mLocationRequest, this);
        }

        private void StopLocationUpdates()
        {
            LocationServices.FusedLocationApi.RemoveLocationUpdates(mGoogleApiClient, this);
        }

        private void DisplayLocation()
        {
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted
                && ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                return;
            }
            mLastLocation = LocationServices.FusedLocationApi.GetLastLocation(mGoogleApiClient);
            if (mLastLocation != null)
            {
                textView.Text = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude).ToString();
            }
            else
            {
                textView.Text = "No location";
            }
        }

        public void OnConnectionSuspended(int cause)
        {
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
        }

        public void OnLocationChanged(Location location)
        {
            mLastLocation = location;
            DisplayLocation();
        }
    }
}