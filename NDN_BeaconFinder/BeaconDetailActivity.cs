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

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.transport;
using net.named_data.jndn.util;

namespace NDN_BeaconFinder
{
    [Activity(Label = "Beacon Detail")]
    public class BeaconDetailActivity : Activity
    {
        public static string READ_INTEREST_STRING = "/icear/beacon/read";
        static int MAC_ADDRESS_LENGTH = 17;
        static int MAC_ADDRESS_LENGTH_NO_COLONS = 12;

        ReadCounter NDN_Counter;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.BeaconDetail);

            Button btnConnectToBeacon = FindViewById<Button>(Resource.Id.btnConnectToBeacon);

            TextView BeaconName = FindViewById<TextView>(Resource.Id.txtBeaconName_Detail);
            BeaconName.Text = MainActivity.SelectedBeacon.Name;

            TextView BeaconAddress = FindViewById<TextView>(Resource.Id.txtMACAddr_Detail);
            BeaconAddress.Text = MainActivity.SelectedBeacon.Mac_Address;

            // Grab the Face
            Face face = MainActivity.face;

            // NDN Code
            try
            {
                // Create the Counter
                NDN_Counter = new ReadCounter();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("exception: " + e.Message);
            }

            btnConnectToBeacon.Click += (sender, e) =>
            {
                // Send out the Interest for Detail/Connect to the Beacon
                // Create the new Discover Interest

                string readString_MAC = READ_INTEREST_STRING + "/" + removeColonsFromMACAddress(MainActivity.SelectedBeacon.Mac_Address);
                Console.WriteLine("Sending Interest for: " + readString_MAC);
                var discoverInterest = new Interest(new Name(readString_MAC));
                discoverInterest.setInterestLifetimeMilliseconds(3s000);

                // Express the Interest
                face.expressInterest(discoverInterest, NDN_Counter, NDN_Counter);

                while(NDN_Counter.callbackCount_ < 1)
                {
                    face.processEvents();
                }
                Console.WriteLine("Out of NDN Loop");
            };
        }

        public static String addColonsToMACAddress(String address)
        {
            StringBuilder sb = new StringBuilder(MAC_ADDRESS_LENGTH);

            sb.append(address.Substring(0, 2));
            sb.append(':');
            sb.append(address.Substring(2, 2));
            sb.append(':');
            sb.append(address.Substring(4, 2));
            sb.append(':');
            sb.append(address.Substring(6, 2));
            sb.append(':');
            sb.append(address.Substring(8, 2));
            sb.append(':');
            sb.append(address.Substring(10, 2));

            return sb.toString();
        }

        public static String removeColonsFromMACAddress(String address)
        {
            StringBuilder sb = new StringBuilder(MAC_ADDRESS_LENGTH_NO_COLONS);

            sb.append(address.Substring(0, 2));
            sb.append(address.Substring(3, 2));
            sb.append(address.Substring(6, 2));
            sb.append(address.Substring(9, 2));
            sb.append(address.Substring(12, 2));
            sb.append(address.Substring(15, 2));

            String noColons = sb.toString();

            Console.WriteLine("MAC string builder ", noColons);

            return noColons;
        }
    }

    class ReadCounter : OnData, OnTimeout
    {
        public void
        onData(Interest interest, Data data)
        {
            ++callbackCount_;
            var content = data.getContent().buf();
            var contentString = "";
            for (int i = content.position(); i < content.limit(); ++i)
                contentString += (char)content.get(i);

            Console.WriteLine("Recieved Data: " + data.getName().toUri() + "\n\n" + contentString);                        

        }

        public void
        onTimeout(Interest interest)
        {
            ++callbackCount_;
            Console.Out.WriteLine("Time out for interest " + interest.getName().toUri());
            gotData = false;
        }

        public int callbackCount_ = 0;
        private bool gotData = false;

        public bool GotData
        {
            get
            {
                bool returnBool = gotData;
                gotData = false;
                return returnBool;
            }
        }
    }
}