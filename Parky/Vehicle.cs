using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Parky
{
    class Vehicle
    {
        public string Name { get; set; }
        public string Verdieping { get; set; }
        public string Info { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime Placed { get; set; }

        public Vehicle()
        {
            Placed = DateTime.Today;
        }
    }
}