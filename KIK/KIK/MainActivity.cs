using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Java.Lang;

namespace KIK
{
    [Activity(Label = "KIK", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]    
	public class MainActivity : Activity
	{
        //public const string SERVER_IP = "192.168.0.3";
		public const string SERVER_IP = "rotfl-online.pl";
        public const int MAX_LENGTH = 1024;
        public const int PORT = 8081;
        //public static TcpClient client = null;
        GameManager gameManager = null;

        void ConnectToServer()
        {
            AlertDialog ad = null;
            try
            {
                Sockets.client = new TcpClient(SERVER_IP, PORT);
            }
            catch (SocketException e)
            {
                ad = new AlertDialog.Builder(this).Create();
                ad.SetCancelable(false); // This blocks the 'BACK' button
                ad.SetMessage("Nie można połączyć się z serwerem. Sprawdź połączenie z internetem.");
                ad.SetButton("OK", delegate
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
                ad.Show();
            }
        }

        void Delegat()
        {
            Intent intent = null;
            EditText edittext = FindViewById<EditText>(Resource.Id.loginArea);
            byte[] buffer = new byte[MAX_LENGTH];
            AlertDialog ad = null;
            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.Hello), edittext.Text));
            Sockets.client.GetStream().Write(buffer, 0, buffer.Length);
            buffer = new byte[MAX_LENGTH];
            Sockets.client.GetStream().Read(buffer, 0, buffer.Length);
            string[] parse = MessageParser.Split(buffer);
            switch (MessageParser.ToMessageType(parse[0]))
            {
                case MessageTypes.WelcomeClient:
                    intent = new Intent(this, typeof(GameManager));
                    intent.PutExtra("Nickname", edittext.Text);
                    //strbuff = System.Text.Encoding.ASCII.GetString(buffer).Split(';')[1];
                    intent.PutExtra("PlayerList", parse[1]);
                    StartActivity(intent);
                    break;
                case MessageTypes.PlayerExist:
                    ad = new AlertDialog.Builder(this).Create();
                    ad.SetCancelable(false); // This blocks the 'BACK' button
                    ad.SetMessage("Gracz o takim nicku istnieje! Wpisz inny login");
                    ad.SetButton("OK", delegate {
                        Sockets.client.Close();
                        ConnectToServer();
                    });
                    ad.Show();
                    break;
                default:
                    ad = new AlertDialog.Builder(this).Create();
                    ad.SetCancelable(false); // This blocks the 'BACK' button
                    ad.SetMessage("Nieoczekiwany błąd. Aplikacja zostanie zamknięta.");
                    ad.SetButton("OK", delegate {
                        Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                    });
                    ad.Show();
                    break;
             }
        }

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);

            Button button = FindViewById<Button>(Resource.Id.zalogujButton);
            EditText edittext = FindViewById<EditText>(Resource.Id.loginArea);
            byte[] buffer = new byte[MAX_LENGTH];
            // logowanie na serwer
            ConnectToServer();

            button.Click += delegate { Delegat(); };
		}
	}
}
	