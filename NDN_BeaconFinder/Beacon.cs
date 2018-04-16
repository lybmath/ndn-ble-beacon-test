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

namespace NDN_BeaconFinder
{
    public class Beacon
    {
        private string name;
        private string mac_address;

        /// Accesors
        public string Name { get { return name; } }
        public string Mac_Address { get { return mac_address; } }

        public Beacon(string Beacon_Name)
        {
            name = Beacon_Name;
        }

        /// <summary>
        /// Change the Name of the Beacon
        /// </summary>
        /// <param name="New_Name">The new name</param>
        /// <returns></returns>
        public bool ChangeName(string New_Name)
        {
            /// Input Sanitation
            if (string.IsNullOrEmpty(New_Name))
            {
                return false;
            }

            // Otherwise we set the name to the new name
            name = New_Name;
            return true;
        }

        /// <summary>
        /// Change the Description of the Beacon
        /// </summary>
        /// <param name="New_Description">The new description</param>
        /// <returns></returns>
        public bool ChangeMACAdress(string New_Address)
        {
            /// Input Sanitation
            if (string.IsNullOrEmpty(New_Address))
            {
                return false;
            }

            // Otherwise we set the description to the new description
            mac_address = New_Address;
            return true;
        }

        public override string ToString()
        {
            return name;
        }

        public override bool Equals(object obj)
        {
            Beacon compare = obj as Beacon;
            return mac_address.Equals(compare.Mac_Address);
        }

    }
}