using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Content;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using Android.Content.PM;

namespace Parky
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Button mapButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // Ask for finelocation permission if needed
            //while (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            //{
            //    //ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation);
            //    ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessFineLocation }, 1000);

            //    if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
            //    {
            //        break;
            //    }
            //}

            FindViews();
            HandleEvents();
        }

        private void FindViews()
        {
            mapButton = FindViewById<Button>(Resource.Id.mapButton);
        }

        private void HandleEvents()
        {
            mapButton.Click += Button1_Click;
        }

        private void Button1_Click(object sender, System.EventArgs e)
        {
            var intent = new Intent(this, typeof(MapActivity));
            StartActivity(intent);
        }
    }
}

