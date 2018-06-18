using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;
using SQLite;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace Parky
{
    [Activity(Label = "ParkedActivity")]
    public class ParkedActivity : Activity
    {
        SQLiteConnection db;
        string dbPath;
        TextView displayText;
        Button clearBtn;
        Button mapBtn;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Views
            SetContentView(Resource.Layout.parked_layout);
            displayText = FindViewById<TextView>(Resource.Id.textView1);
            clearBtn = FindViewById<Button>(Resource.Id.clearButton);
            mapBtn = FindViewById<Button>(Resource.Id.mapButton);

            // Database
            dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "ParkyDatabase.db3");
            db = new SQLiteConnection(dbPath);
            var table = db.Table<Vehicle>();
            foreach (var vehicle in table)
            {
                displayText.Text += string.Format(" Naam: {3} \n Verdieping: {4} \n Info: {5} \n Coordinaten: {0}, {1} \n Datum: {2} \n", 
                    vehicle.Lat, vehicle.Lng, vehicle.Placed, vehicle.Name, vehicle.Verdieping, vehicle.Info);
                displayText.Text += "\n";
            }

            // Clickevents
            clearBtn.Click += delegate
            {
                db.DeleteAll<Vehicle>();
                var intent = new Intent(this, typeof(ParkedActivity));
                StartActivity(intent);
            };

            mapBtn.Click += delegate
            {
                var intent = new Intent(this, typeof(MapActivity));
                StartActivity(intent);
            };
        }
    }
}