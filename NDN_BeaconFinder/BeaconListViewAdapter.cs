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
    public class BeaconListViewAdapter : BaseAdapter<string>
    {
        private List<Beacon> mitems;
        private Context mContext;

        public BeaconListViewAdapter(Context context, List<Beacon> items)
        {
            mitems = items;
            mContext = context;
        }

        public override int Count
        {
            get { return mitems.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override string this[int position]
        {
            get { return mitems[position].Name; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View row = convertView;

            if(row == null)
            {
                row = LayoutInflater.From(mContext).Inflate(Resource.Layout.listView_row, null, false);
            }

            TextView txtName = row.FindViewById<TextView>(Resource.Id.txtName);
            txtName.Text = mitems[position].Name;

            TextView txtAddress = row.FindViewById<TextView>(Resource.Id.txtAddress);
            txtAddress.Text = mitems[position].Mac_Address;
            return row;
        }
    }
}