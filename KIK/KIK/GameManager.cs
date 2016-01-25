using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;

namespace KIK
{
    [Activity(Label = "KIK - wybierz przeciwnika", Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class GameManager: Activity
    {
        //public static TcpClient client = null;
        string nickname = null;
        public static Thread th = null;       
        ListView lv = null;
        List<Tuple<string, string>> data;
        AlertDialog ad = null;
        public static bool thLock = false;
        bool czyOtwartyAlert = false;
        public static bool isPlaying = false;

        void DelegatDlaItemClick(AdapterView.ItemClickEventArgs e)
        {
            Tuple<string, string> item = lv.GetItemAtPosition(e.Position).Cast<Tuple<string, string>>();
            if (item.Item1 == nickname)
                return;
            ad = new AlertDialog.Builder(this).Create();
            ad.SetCancelable(false); // This blocks the 'BACK' button
            ad.SetMessage(string.Format("Czy chcesz zaprosiæ u¿ytkownika {0} do gry?", item.Item1));
            ad.SetButton("Nie", delegate
            {
            });
            ad.SetButton2("Tak", delegate
            {
                byte[] buff = new byte[MainActivity.MAX_LENGTH];
                buff = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.Invite), item.Item1));
                Sockets.client.GetStream().Write(buff, 0, buff.Length);
            });
            ad.Show();
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.TablesScreen);            
            nickname = Intent.GetStringExtra("Nickname");
            FindViewById<TextView>(Resource.Id.loginField).Text = nickname;
            data = new List<Tuple<string, string>>();
            ParsePlayerList(Intent.GetStringExtra("PlayerList"));
            lv = FindViewById<ListView>(Resource.Id.playerList);
            lv.Adapter = new PlayerListAdapter(this, data);
            lv.ItemClick += delegate(object sender, AdapterView.ItemClickEventArgs e)
            {
                DelegatDlaItemClick(e);
            };
            th = new Thread(new ThreadStart(MessageExecuteThread));            
            th.Start();            
        }

        private void ParsePlayerList(string plist)
        {
            string[] pp = plist.Split(':');
            data.Clear();
            foreach (string p in pp)
            {
                string[] pattr = p.Split(',');
                data.Add(new Tuple<string, string>(pattr[0], string.Format("Wins: {0}, Loses: {1}", pattr[1], pattr[2])));
            }
        }

        void MessageExecuteThread()
        {
            byte[] buffer;
            NetworkStream stream = Sockets.client.GetStream();

            while (Sockets.client.Connected)
            {
                if (thLock)
                    continue;

                if (isPlaying)
                {
                    buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.ListRequestClient), nickname));
                    stream.Write(buffer, 0, buffer.Length);
                    isPlaying = false;
                }

                try
                {
                    buffer = new byte[MainActivity.MAX_LENGTH];
                    stream.Read(buffer, 0, buffer.Length);
                    string[] data = MessageParser.Split(buffer);
                    if (data.Length == 1)
                    {
                        Sockets.client.Close();
                        RunOnUiThread(delegate {
                            ad = new AlertDialog.Builder(this).Create();
                            ad.SetCancelable(false); // This blocks the 'BACK' button
                            ad.SetMessage("Nie mo¿na po³¹czyæ siê z serwerem. SprawdŸ po³¹czenie z internetem.");
                            ad.SetButton("OK", delegate
                            {
                                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                            });
                            ad.Show();
                        });
                    }
                    switch (MessageParser.ToMessageType(data[0]))
                    {
                        case MessageTypes.SendPlayers:
                            ParsePlayerList(data[1]);
                            lv.Post(delegate { lv.Adapter = new PlayerListAdapter(this, this.data); });
                            break;
                        case MessageTypes.InviteClient:
                            if (data[1] == nickname)
                                break;
                            RunOnUiThread(delegate
                            {                                
                                ad = new AlertDialog.Builder(this).Create();
                                ad.SetCancelable(false); // This blocks the 'BACK' button
                                ad.SetMessage(string.Format("Masz zaproszenie do gry od {0}.", data[1]));                                
                                ad.SetButton("Odrzuæ", delegate 
                                {
                                    buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.Decline), data[1]));
                                    stream.Write(buffer, 0, buffer.Length);
                                    czyOtwartyAlert = false;
                                });
                                ad.SetButton2("Akceptuj", delegate
                                {
                                    buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.Accept), data[1]));
                                    stream.Write(buffer, 0, buffer.Length);
                                    czyOtwartyAlert = false;
                                });
                                ad.Show();
                            });
                            czyOtwartyAlert = true;
                            while (czyOtwartyAlert)
                                continue;
                            Thread.Sleep(100);
                            break;
                        case MessageTypes.StartClient:
                            RunOnUiThread(delegate
                            {
                                Intent intent = new Intent(this, typeof(GameActivity));
                                intent.PutExtra("Nickname", nickname);
                                intent.PutExtra("Data", data[1]);
                                StartActivity(intent);
                            });
                            thLock = true;
                            isPlaying = true;
                            break;
						case MessageTypes.PlayerIsBusy:
							RunOnUiThread(delegate {
								ad = new AlertDialog.Builder(this).Create();
								ad.SetCancelable(false); // This blocks the 'BACK' button
								ad.SetMessage(string.Format("Gracz {0} jest zajêty.", data[1]));
								ad.SetButton("Zamknij", delegate {});
								ad.Show();
							});
							break;
                    }
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    return;
                }
                catch (Exception e)
                {
                    Sockets.client.Close();
                    RunOnUiThread(delegate {
                        ad = new AlertDialog.Builder(this).Create();
                        ad.SetCancelable(false); // This blocks the 'BACK' button
                        ad.SetMessage("Nie mo¿na po³¹czyæ siê z serwerem. SprawdŸ po³¹czenie z internetem.");
                        ad.SetButton("OK", delegate
                        {
                            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                        });
                        ad.Show();
                    });
                }
            }
        }

        public override void OnBackPressed()
        {
            AlertDialog ad = new AlertDialog.Builder(this).Create();
            ad.SetCancelable(false); // This blocks the 'BACK' button
            ad.SetMessage("Czy chcesz wyjœæ z gry?");
            ad.SetButton("Nie", delegate { /*dialogLive = false;*/ });
            ad.SetButton2("Tak", delegate
            {
                Sockets.client.Close();
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                //dialogLive = false;
            });
            //dialogLive = true;
            ad.Show();
        }

    }
}