
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

namespace Ravc.Client.Android
{
	[Activity (Label = "Ravc.Client.Android", MainLauncher = true, Icon = "@drawable/icon")]			
	public class HomeActivity : Activity
	{
		EditText hostNameTextEdit;
		EditText portTextEdit;
		EditText bufferingOffsetTextEdit;
		CheckBox showDebugInfoCheckBox;
		Button connectButton;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView(Resource.Layout.HomeLayout);
			hostNameTextEdit = FindViewById<EditText>(Resource.Id.HostName);
			portTextEdit = FindViewById<EditText>(Resource.Id.Port);
			bufferingOffsetTextEdit = FindViewById<EditText>(Resource.Id.BufferingOffset);
			showDebugInfoCheckBox = FindViewById<CheckBox>(Resource.Id.ShowDebugInfo);
			connectButton = FindViewById<Button>(Resource.Id.ConnectButton);

			connectButton.Click += OnConnect;
		}

		void OnConnect(object sender, EventArgs args)
		{
			var streamIntent = new Intent (this, typeof (GLActivity));
			streamIntent.PutExtra("HostName", hostNameTextEdit.Text);

			int port;
			if (!int.TryParse (portTextEdit.Text, out port)) 
			{
				new AlertDialog.Builder(this).SetMessage("Incorrect port").Show();
				return;
			}
			streamIntent.PutExtra("Port", port);

			double bufferingOffset;
			if (!double.TryParse(bufferingOffsetTextEdit.Text, out bufferingOffset)) 
			{
				new AlertDialog.Builder(this).SetMessage("Incorrect buffering offset").Show();
				return;
			}
			streamIntent.PutExtra("BufferingOffset", bufferingOffset);

			streamIntent.PutExtra("ShowDebugInfo", showDebugInfoCheckBox.Checked);

			StartActivity(streamIntent);
		}
	}
}

