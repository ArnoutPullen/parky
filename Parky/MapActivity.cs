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
        Button vButton;
        SQLiteConnection db;
        string dbPath;
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
        LatLng markerPosition;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Find views
            SetContentView(Resource.Layout.map_layout);
            textView = FindViewById<TextView>(Resource.Id.textView1);
            tButton = FindViewById<Button>(Resource.Id.trackingButton);
            parkButton = FindViewById<Button>(Resource.Id.parkButton);
            vButton = FindViewById<Button>(Resource.Id.vehiclesButton);

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
                BuildGoogleApiClient();
                CreateLocationRequest();
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

            // Database
            dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "ParkyDatabase.db3");
            db = new SQLiteConnection(dbPath);
            db.CreateTable<Vehicle>();

            // Parkbutton
            parkButton.Click += delegate
            {
                LayoutInflater layoutInflater = LayoutInflater.From(this);
                View view = layoutInflater.Inflate(Resource.Layout.user_input_dialog_box, null);
                Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
                alertbuilder.SetView(view);
                var userdata = view.FindViewById<EditText>(Resource.Id.editText);
                var userdata2 = view.FindViewById<EditText>(Resource.Id.editText2);
                var userdata3 = view.FindViewById<EditText>(Resource.Id.editText3);
                alertbuilder.SetCancelable(false)
                    .SetPositiveButton("OK", delegate{
                        db.Insert(new Vehicle() {
                            Name =userdata.Text,
                            Lat = markerPosition.Latitude,
                            Lng = markerPosition.Longitude,
                            Verdieping = userdata2.Text,
                            Info = userdata3.Text
                        });
                        var intent = new Intent(this, typeof(ParkedActivity));
                        StartActivity(intent);

                        Toast.MakeText(this, "Voertuig opgeslagen", ToastLength.Short).Show();
                    })
                    .SetNegativeButton("Cancel", delegate{
                        alertbuilder.Dispose();
                    });
                Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();
                dialog.Show();
            };

            vButton.Click += delegate
            {
                var intent = new Intent(this, typeof(ParkedActivity));
                StartActivity(intent);
            };

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
                        BuildGoogleApiClient();
                        CreateLocationRequest();
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

        public void OnMapReady(GoogleMap googleMap)
        {
            map = googleMap;
            map.MyLocationEnabled = true;
            map.UiSettings.MyLocationButtonEnabled = true;
            map.UiSettings.CompassEnabled = true;

            CameraUpdate cUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(51.917411, 4.484052), 13);
            map.MoveCamera(cUpdate);

            // Loading saved vehicles
            var table = db.Table<Vehicle>();
            foreach (var vehicle in table)
            {
                MarkerOptions markerOptions = new MarkerOptions()
                    .SetPosition(new LatLng(vehicle.Lat, vehicle.Lng))
                    .SetTitle(vehicle.Name)
                    .SetSnippet($"Verdieping: {vehicle.Verdieping} Info: {vehicle.Info}")
                    .Draggable(false);
                map.AddMarker(markerOptions);

                CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(vehicle.Lat, vehicle.Lng), 17);
                map.MoveCamera(cameraUpdate);
            }

            map.MapLongClick += _map_MapLongClick;
        }
        private void _map_MapLongClick(object sender, GoogleMap.MapLongClickEventArgs e)
        {

            markerPosition = e.Point;

            MarkerOptions markerOptions = new MarkerOptions()
                .SetPosition(markerPosition)
                .SetTitle("Sleep naar je voertuig")
                .Draggable(true);

            map.AddMarker(markerOptions);
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