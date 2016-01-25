using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    [Activity(Label = "KIK", Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class GameActivity: Activity
    {
        Thread th = null;
        string nickname = null;
        string opponentNickname = null;
        char[] canvas;
        char mySign = ' ';
        char oppSign = ' ';
        bool isMyTurn = false;
        bool dialogLive = false;

        List<Button> pole = null;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.GameLayout);

            canvas = "---------".ToCharArray();
            nickname = Intent.GetStringExtra("Nickname");
            string[] data_splited = Intent.GetStringExtra("Data").Split(',');
            if (data_splited[0] != nickname)
            {
                opponentNickname = data_splited[0];
                mySign = 'O';
                oppSign = 'X';
            }
            else
            {
                opponentNickname = data_splited[1];
                mySign = 'X';
                oppSign = 'O';
            }

            if (data_splited[2] == nickname)
                isMyTurn = true;

            pole = new List<Button>();

            pole.Add(FindViewById<Button>(Resource.Id.pole1));
            pole.Add(FindViewById<Button>(Resource.Id.pole2));
            pole.Add(FindViewById<Button>(Resource.Id.pole3));
            pole.Add(FindViewById<Button>(Resource.Id.pole4));
            pole.Add(FindViewById<Button>(Resource.Id.pole5));
            pole.Add(FindViewById<Button>(Resource.Id.pole6));
            pole.Add(FindViewById<Button>(Resource.Id.pole7));
            pole.Add(FindViewById<Button>(Resource.Id.pole8));
            pole.Add(FindViewById<Button>(Resource.Id.pole9));

            pole[0].Click += delegate { ButtonClickEvent(0); };
            pole[1].Click += delegate { ButtonClickEvent(1); };
            pole[2].Click += delegate { ButtonClickEvent(2); };
            pole[3].Click += delegate { ButtonClickEvent(3); };
            pole[4].Click += delegate { ButtonClickEvent(4); };
            pole[5].Click += delegate { ButtonClickEvent(5); };
            pole[6].Click += delegate { ButtonClickEvent(6); };
            pole[7].Click += delegate { ButtonClickEvent(7); };
            pole[8].Click += delegate { ButtonClickEvent(8); };

            FindViewById<TextView>(Resource.Id.textView2).Text = string.Format("Nickname: {0}   Znak : {1}", nickname, mySign);
            FindViewById<TextView>(Resource.Id.textView3).Text = string.Format("Nickname: {0}   Znak : {1}", opponentNickname, oppSign);

            th = new Thread(new ThreadStart(GameThreadProc));
            th.Start();
        }

        void GameThreadProc()
        {
            NetworkStream stream = Sockets.client.GetStream();
            byte[] buffer;
            AlertDialog ad = null;

            while (Sockets.client.Connected)
            {
                try
                {
                    buffer = new byte[MainActivity.MAX_LENGTH];
                    stream.Read(buffer, 0, buffer.Length);
                    string[] data = MessageParser.Split(buffer);
                    if (data.Length == 1)
                    {
                        Sockets.client.Close();
                        RunOnUiThread(delegate
                        {
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
                        case MessageTypes.TurnClient:
                            RunOnUiThread(delegate { ParseCanvas(data[1]); });
                            isMyTurn = true;
                            break;
                        case MessageTypes.WinClient:
                            RunOnUiThread(delegate
                            {
                                ad = new AlertDialog.Builder(this).Create();
                                ad.SetCancelable(false); // This blocks the 'BACK' button
                                if (data[1].Contains(nickname))
                                    ad.SetMessage("Gratulacje! Wygra³eœ!");
                                else
                                    ad.SetMessage("Niestety przegra³eœ!");
                                ad.SetButton("WyjdŸ", delegate
                                {
                                    GameManager.thLock = false;
                                    Finish();
                                });
                                ad.Show();
                            });
                            return;
                            break;
                        case MessageTypes.LeftClient:
                            RunOnUiThread(delegate
                            {
                                ad = new AlertDialog.Builder(this).Create();
                                ad.SetCancelable(false); // This blocks the 'BACK' button
                                ad.SetMessage("Twój przeciwnik opuœci³ grê!");
                                ad.SetButton("WyjdŸ", delegate
                                {
                                    //th.Abort();
                                    GameManager.thLock = false;
                                    Finish();
                                });
                                ad.Show();
                            });
                            return;
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
                    RunOnUiThread(delegate
                    {
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

        void ParseCanvas(string canv)
        {
            canvas = canv.ToCharArray();
            for(int i=0; i<pole.Count; i++)
                if (canvas[i] != '-')
                    pole[i].Text = string.Format("{0}",canvas[i]);
        }

        private void ButtonClickEvent(int numerek)
        {
            if (isMyTurn)
            {
                if (canvas[numerek] != '-')
                {
                    AlertDialog ad = new AlertDialog.Builder(this).Create();
                    ad.SetCancelable(false); // This blocks the 'BACK' button
                    ad.SetMessage("Te pole jest ju¿ zajête!");
                    ad.SetButton("OK", delegate { });
                    ad.Show();
                    return;
                }
                canvas[numerek] = mySign;
                byte[] buffbuff = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.Turn), new string(canvas)));
                pole[numerek].Text = Char.ToString(mySign);
                Sockets.client.GetStream().Write(buffbuff, 0, buffbuff.Length);
                isMyTurn = false;
            }
            else
            {
                AlertDialog ad = new AlertDialog.Builder(this).Create();
                ad.SetCancelable(false); // This blocks the 'BACK' button
                ad.SetMessage("Poczekaj na swoj¹ kolej!");
                ad.SetButton("OK", delegate {});
                ad.Show();
            }
        }

        public override void OnBackPressed()
        {
            AlertDialog ad = new AlertDialog.Builder(this).Create();
            ad.SetCancelable(false); // This blocks the 'BACK' button
            ad.SetMessage("Czy chcesz opuœciæ grê (oddasz walkowerem)?");
            ad.SetButton("Nie", delegate { dialogLive = false; });
            ad.SetButton2("Tak", delegate
            {
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.Left), nickname));
                Sockets.client.GetStream().Write(buffer, 0, buffer.Length);
                dialogLive = false;
            });
            dialogLive = true;
            ad.Show();
        }

    }
}