using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Collections.Generic;
using System;

using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.transport;
using net.named_data.jndn.util;

using net.named_data.jndn.security;
using net.named_data.jndn.security.identity;
using net.named_data.jndn.security.policy;
using ILOG.J2CsMapping.NIO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Android.Views;
using Android.Content.PM;

namespace NDN_BeaconFinder
{
    [Activity(Label = "UCLA NDN Beacon Finder App", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity, ListView.IOnItemClickListener
    {
        // Create threads here
        Thread faceUpdate,discoveryUpdate;
        public static bool gotReply = false;
        bool currentlyDiscovering = false;

        public static string DISCOVERY_INTEREST_STRING = "/icear/beacon/discover";
        public static string IDENTITY_INTEREST_STRING = "/icear/beacon/status";

        public static List<Beacon> BeaconList;
        public static Beacon SelectedBeacon;

        int NumberOfBroadcasts = 0;

        // Events
        private event EventHandler onBeaconComplete;
        private event EventHandler onInterestCounter;

        // NDN Static Variables
        public static Face face;

        // References to the Objects within the Activity
        private TextView NumberOfBeacons;
        private ProgressBar ProgressBar_Discovery;
        private Button DiscoverBeacons;
        private Button DisplayBeacons;
        public static ListView BeaconListView;
        private BeaconListViewAdapter adapter;
        private DiscoveryCounter NDN_Counter;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Create new List
            BeaconList = new List<Beacon>();

            eraseSelectedBeacon();

            /// Create References
            DiscoverBeacons = FindViewById<Button>(Resource.Id.btnSim);
            NumberOfBeacons = FindViewById<TextView>(Resource.Id.txtNumber);
            DisplayBeacons = FindViewById<Button>(Resource.Id.btnDisplay);
            BeaconListView = FindViewById<ListView>(Resource.Id.lstBeacons);
            ProgressBar_Discovery = FindViewById<ProgressBar>(Resource.Id.progressBar_Discovery);   
            
            adapter = new BeaconListViewAdapter(this, BeaconList);

            // Assign the Adapter
            BeaconListView.Adapter = adapter;

            // ListView Click
            BeaconListView.OnItemClickListener = this;

            DisplayBeacons.Click += DisplayBeacons_Click;

        }

        protected override void OnStart()
        {
            base.OnStart();

            // NDN Code
            try
            {
                // Create the Face
                face = RegisterFace();

                // Create the Counter
                NDN_Counter = new DiscoveryCounter();

                /*
                // Run the Faceupdate continuous thread
                faceUpdate = new Thread(new ThreadStart(delegate
                {
                    while (true)
                    {
                        face.processEvents();
                        // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                        System.Threading.Thread.Sleep(30);

                        if (NDN_Counter.callbackCount_ > 0)
                        {
                            // We got a callback from an interest we sent!
                            // Reset Callback count
                            NDN_Counter.callbackCount_ = 0;
                            onInterestCounter.Invoke(this, new EventArgs());
                        }
                    }
                }));
                faceUpdate.Start();
                */

                this.onBeaconComplete += MainActivity_onBeaconComplete;
                this.onInterestCounter += MainActivity_onInterestCounter;

                // Button Method
                DiscoverBeacons.Click += (sender, e) =>
                {
                    /// Final Code
                    //DiscoverBeacons.Text = "Discovering...";
                    DiscoverBeacons.Text = "Display Beacons";
                    ProgressBar_Discovery.Visibility = Android.Views.ViewStates.Visible;
                    //DiscoverBeacons.Enabled = false;
                    NumberOfBeacons.Text = "Processing";
                    BeaconList.Clear();
                    eraseSelectedBeacon();

                    // Create the new Discover Interest
                    var discoverInterest = new Interest(new Name(DISCOVERY_INTEREST_STRING));
                    discoverInterest.setInterestLifetimeMilliseconds(5000);
                    NDN_Counter.expectedInterests = 1;

                    // Express the Interest
                    face.expressInterest(discoverInterest, NDN_Counter, NDN_Counter);
                    while (NDN_Counter.callbackCount_ < 1)
                    {
                        face.processEvents();
                        // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
                        System.Threading.Thread.Sleep(100);
                    }
                    NDN_Counter.callbackCount_ = 0;

                    onBeaconComplete.Invoke(this, new EventArgs());
                    // Invoke complete Event                    

                };
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("exception: " + e.Message);
            }
        }

        private void DisplayBeacons_Click(object sender, EventArgs e)
        {
            onBeaconComplete.Invoke(sender,e);
        }

        private void MainActivity_onBeaconComplete(object sender, EventArgs e)
        {
            if (BeaconList.Count != 0)
            {
                DiscoverBeacons.Text = "Display Beacons";
                ProgressBar_Discovery.Visibility = Android.Views.ViewStates.Invisible;

                NumberOfBeacons.Text = BeaconList.Count.ToString();
                Console.WriteLine("Setting the Number of Beacons to: " + BeaconList.Count.ToString());

                // This is after the text changed - Therefore this means that we just finished Discovering
                NumberOfBeacons.Text = BeaconList.Count.ToString();

                // Refresh the Listview Adapter (Otherwise you wont see the new objects in the Listview)
                BeaconListView.Adapter = null;
                //adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, TempList);
                adapter = new BeaconListViewAdapter(this, BeaconList);
                BeaconListView.Adapter = adapter;

                DiscoverBeacons.Text = "Discover Beacons";
            }
            else
            {
                NumberOfBeacons.Text = "Error";
            }
            
            DiscoverBeacons.Enabled = true;
            return;
        }

        private void MainActivity_onInterestCounter(object sender, EventArgs e)
        {
            if (NDN_Counter.GotData)
            {
                //onBeaconComplete.Invoke(sender, e);
                Console.WriteLine("Got Data! Took " + NumberOfBroadcasts.ToString() + " attempts.");
                NumberOfBroadcasts = 0;
            }
            else if (NumberOfBroadcasts <= 10)
            {
                // Create the new Discover Interest
                var discoverInterest = new Interest(new Name(DISCOVERY_INTEREST_STRING));
                discoverInterest.setInterestLifetimeMilliseconds(1000);

                face.expressInterest(discoverInterest, NDN_Counter, NDN_Counter);
                NumberOfBroadcasts++;
            }
            else if (NumberOfBroadcasts >= 10)
            {
                NumberOfBroadcasts = 0;
                Console.WriteLine("Ran out of Broadcasts...");
                //onBeaconComplete.Invoke(sender, e);
            }
            return;
        }
        
        // Resisters the Face with the NFD Android App
        private Face RegisterFace()
        {
            Face face = new Face
                  (new TcpTransport(), new TcpTransport.ConnectionInfo("127.0.0.1"));
            try
            {
                var prefixRegisterCallbacks = new prefixRegistrationStuff();

                var identityStorage = new MemoryIdentityStorage();
                var privateKeyStorage = new MemoryPrivateKeyStorage();
                var keyChain = new KeyChain
                  (new IdentityManager(identityStorage, privateKeyStorage),
                    new SelfVerifyPolicyManager(identityStorage));
                keyChain.setFace(face);

                // Initialize the storage.
                var name = new Name(IDENTITY_INTEREST_STRING);
                if (!identityStorage.doesIdentityExist(name))
                {
                    keyChain.createIdentityAndCertificate(name);

                    // set default identity
                    keyChain.getIdentityManager().setDefaultIdentity(name);
                }

                face.setCommandSigningInfo(keyChain, keyChain.getDefaultCertificateName());

                var registerSuccessThing = new registerSuccessClass();
                
                face.registerPrefix(name, prefixRegisterCallbacks, prefixRegisterCallbacks, registerSuccessThing);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("exception: " + e.Message);
            }
            return face;
        }

        private async void ContinuousFaceUpdate(Face face)
        {
            while (true)
            {
                face.processEvents();
                await Task.Delay(100);
            }
        }

        private void eraseSelectedBeacon()
        {
            // Populate the Dummy Beacon - This information should never be seen by the User
            SelectedBeacon = new Beacon("Invalid Beacon");
            SelectedBeacon.ChangeMACAdress("Invalid MAC Address");
        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {
            // Set the selected Beacon to the Static Object
            Console.WriteLine("Clicked on Position: " + position);
            SelectedBeacon = BeaconList[position];

            // Open an Activity
            // Setup a new Intent
            var intent = new Intent(this, typeof(BeaconDetailActivity));
            StartActivity(intent);
        }        
    }

    /* NDN Stuff down below */

    class DiscoveryCounter : OnData, OnTimeout
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

            // Create a Beacon Object for each one in the data
            string[] beaconNames = contentString.Split("\n".ToCharArray()); // This puts the string to NAME,ADDRESS

            MainActivity.BeaconList.Clear();

            foreach (string beacon in beaconNames)
            {
                string beaconName = "";
                string beaconAddr = "";
                // beacon is in the format of NAME,ADDRESS
                string[] beaconData = beacon.split(",");
                if (beaconData.Length == 2)
                {
                    // First array item will be the name, second will be the address
                    beaconName = beaconData[0].Replace("Device name: ", string.Empty);
                    beaconAddr = beaconData[1].Replace("Device Address: ", string.Empty);
                }
                else
                {
                    //Something went wrong
                }

                // Create a new Beacon with the Name
                Beacon newBeacon = new Beacon(beaconName);
                // Set the MAC Address
                newBeacon.ChangeMACAdress(beaconAddr);

                // Add the Beacon to the Master List
                MainActivity.BeaconList.Add(newBeacon);

                // Log
                Console.WriteLine("Added a Beacon, Name: " + newBeacon.Name + ", MAC Addr: " + newBeacon.Mac_Address);
            }
        }

        public void
        onTimeout(Interest interest)
        {
            ++callbackCount_;
            Console.Out.WriteLine("Time out for interest " + interest.getName().toUri());
            gotData = false;
        }

        public int callbackCount_ = 0;
        public int expectedInterests = 0;
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

    class prefixRegistrationStuff : OnInterestCallback, OnRegisterFailed
    {
        public void onRegisterFailed(Name prefix)
        {
            Console.Out.WriteLine(prefix.ToString() + ": prefix registration failed");
        }

        public void onInterest(Name prefix, Interest interest, Face face, long interestFilterId, InterestFilter filter)
        {
            Console.Out.WriteLine("Got interest with name: " + prefix.ToString());
        }       
    }

    class registerSuccessClass : OnRegisterSuccess
    {
        public void onRegisterSuccess(Name prefix, long registeredPrefixId)
        {
            Console.Out.WriteLine("\n" + "successfully registered prefix" + prefix.ToString());
        }
        
    }
}

