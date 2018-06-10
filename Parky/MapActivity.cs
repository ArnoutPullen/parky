using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Plugin.Geolocator;

namespace Parky
{
    [Activity(Label = "MapActivity", MainLauncher = false)]
    public class MapActivity : Activity, IOnMapReadyCallback, ILocationListener
    {
        LatLng marker;
        LatLng hofplein = new LatLng(51.924420, 4.477733);
        LocationManager locationManager;
        GoogleMap map;
        LatLng latLng;
        string provider;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.map_layout);

            MapFragment mapFragment = FragmentManager.FindFragmentById<MapFragment>(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            locationManager = (LocationManager)GetSystemService(Context.LocationService);

            Criteria criteria = new Criteria();
            criteria.Accuracy = Accuracy.Fine;
            criteria.PowerRequirement = Power.High;

            provider = locationManager.GetBestProvider(criteria, true);
            Location location = locationManager.GetLastKnownLocation(provider);
            if (location == null)
            {
                locationManager.RequestLocationUpdates(provider, 400, 1, this);
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            map = googleMap;

            map.UiSettings.ZoomControlsEnabled = true;
            map.UiSettings.CompassEnabled = true;
            map.MyLocationEnabled = true;
            map.UiSettings.MyLocationButtonEnabled = true;

            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(hofplein, 17);
            map.MoveCamera(cameraUpdate);

            map.MapLongClick += GMap_MapLongClick;
            map.MarkerDragEnd += GMap_MarkerDragEnd;
        }

        private void GMap_MapLongClick(object sender, GoogleMap.MapLongClickEventArgs e)
        {
            marker = e.Point;

            CameraUpdate update = CameraUpdateFactory.NewLatLngZoom(marker, 17);
            map.MoveCamera(update);

            MarkerOptions markerOptions = new MarkerOptions();
            markerOptions.SetPosition(marker);
            markerOptions.SetTitle("Hier staat je voertuig");
            markerOptions.SetSnippet($"Latitude: , Longitude: ");
            markerOptions.Draggable(true);

            map.Clear();
            map.AddMarker(markerOptions);
        }

        private void GMap_MarkerDragEnd(object sender, GoogleMap.MarkerDragEndEventArgs e)
        {
            marker = e.Marker.Position;

            CameraUpdate update = CameraUpdateFactory.NewLatLngZoom(marker, 17);
            map.MoveCamera(update);
        }

        public void OnLocationChanged(Location location)
        {
            latLng = new LatLng(location.Latitude, location.Longitude);

            //map.MoveCamera(CameraUpdateFactory.NewLatLng(latLng));

            CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
            builder.Target(latLng);
            CameraPosition cameraPosition = builder.Build();
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
            map.MoveCamera(cameraUpdate);
        }

        public void OnProviderDisabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //throw new NotImplementedException();
        }

        protected override void OnResume()
        {
            base.OnResume();
            locationManager.RequestLocationUpdates(provider, 400, 1, this);

        }
        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
        }
    }
}