using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TCPCommanderAndroid
{
    [Activity(Label = "FunctionsPage", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class FunctionsPage : Activity
    {
        public static string storedprivIP;
        Show_Dialog dialog;
        Button testButton;
        Button pingButton;
        Button shutdownButton;
        Button monitorONButton;
        Button monitorOFFButton;
        Button screenshotButton;
        Button PCLockButton;
        //ImageView screenshotView;
        Vibrator vibrator;
        byte[] buffer = new byte[1];
        System.Timers.Timer connectionTimer;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.functions_page);

            dialog = new Show_Dialog(this);

            testButton = FindViewById<Button>(Resource.Id.testButton);
            pingButton = FindViewById<Button>(Resource.Id.pingButton);
            shutdownButton = FindViewById<Button>(Resource.Id.shutdownButton);
            monitorONButton = FindViewById<Button>(Resource.Id.monitorONButton);
            monitorOFFButton = FindViewById<Button>(Resource.Id.monitorOFFButton);
            screenshotButton = FindViewById<Button>(Resource.Id.screenshotButton);
            PCLockButton = FindViewById<Button>(Resource.Id.PCLockButton);
            //screenshotView = FindViewById<ImageView>(Resource.Id.screenshotView);
            vibrator = (Vibrator)GetSystemService(Context.VibratorService);

            testButton.Click += Test_Clicked;
            pingButton.Click += Ping_Clicked;
            shutdownButton.Click += Shutdown_Clicked;
            monitorONButton.Click += MonitorOn_Clicked;
            monitorOFFButton.Click += MonitorOff_Clicked;
            screenshotButton.Click += TakeScreenshot_Clicked;
            PCLockButton.Click += PCLock_Clicked;

            connectionTimer = new System.Timers.Timer();
            connectionTimer.Interval = 1000;
            connectionTimer.Enabled = true;
            connectionTimer.Start();
            connectionTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                if (TCP_Connection.TCP_Instance.client != null)
                {
                    if (MainActivity.clientIsOpen)
                    {
                        if (TCP_Connection.TCP_Instance.client.Client.Poll(0, SelectMode.SelectRead))
                        {
                            if (TCP_Connection.TCP_Instance.client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                            {
                                //DisplayAlert("Client Disconnected", "Client Disconnected", "OK");
                                ClientDisconnected();
                            }
                        }
                    }
                }
            };
        }

        bool PCLockStatus;
        protected override void OnResume()
        {
            base.OnResume();

            if(MainActivity.alertSuccessfulConnection)
            {
                MainActivity.alertSuccessfulConnection = false;
                dialog.ShowDialog("Connected", "Connected to server successfully!", false, Show_Dialog.MessageResult.OK);
                writeMessage("{PC_LOCK_STATUS}");
                string status = getDataAsString(TCP_Connection.TCP_Instance.client);

                if (status.Contains("{PC_LOCK_STATUS}") && status.Contains("{/PC_LOCK_STATUS}"))
                {
                    string result = getBetween(status, "{PC_LOCK_STATUS}", "{/PC_LOCK_STATUS}");

                    if (result.ToLower() == "true")
                    {
                        PCLockStatus = true;
                        PCLockButton.Text = "Lock Computer (Currently: Locked)";
                    }
                    else if (result.ToLower() == "false")
                    {
                        PCLockStatus = false;
                        PCLockButton.Text = "Lock Computer (Currently: Unlocked)";
                    }
                }
            }
            connectionTimer.Start();
        }

        protected override void OnPause()
        {
            base.OnPause();

            connectionTimer.Stop();
        }

        public override async void OnBackPressed()
        {
            bool response = await dialog.ShowDialog("Are You Sure?", "Do you wan't to disconnect and go to the login page?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;
            if (response)
            {
                TCP_Connection.TCP_Instance.client.GetStream().Close();
                TCP_Connection.TCP_Instance.client.Close();
                MainActivity.clientIsOpen = false;
                MainActivity.succesfullyDisconnected = true;
                //TryResumeActivity(typeof(MainActivity));
                StartActivity(typeof(MainActivity));
            }      
        }

        public void ClientDisconnected()
        {
            try
            {
                TCP_Connection.TCP_Instance.client.Close();
                TCP_Connection.TCP_Instance.client.GetStream().Close();
            }
            catch (Exception ex)
            {
                if (MainActivity.analyticsEnabled)
                {
                    MainActivity.developerExceptionList.Add(ex);
                }

            }
            MainActivity.clientIsOpen = false;
            MainActivity.lostConnection = true;
            //TryResumeActivity(typeof(MainActivity));
            StartActivity(typeof(MainActivity));
        }

        int vibrationTime = 500;
        private void Test_Clicked(object sender, EventArgs e)
        {

            writeMessage("{TEST}");

            string returnMessage = getDataAsString(TCP_Connection.TCP_Instance.client);

            if (returnMessage == null)
            {
                return;
            }

            if (returnMessage == "{TEST_RESPOND}")
            {
                if (vibrator.HasVibrator)
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        vibrator.Vibrate(VibrationEffect.CreateOneShot(vibrationTime, 255));
                    }
                    else
                    {
                        vibrator.Vibrate(vibrationTime);
                    }
                }
                dialog.ShowDialog("Test Successful", "The server received the message and responded", false, Show_Dialog.MessageResult.OK);
            }
        }

        private async void Shutdown_Clicked(object sender, EventArgs e)
        {
            bool response = await dialog.ShowDialog("Shutdown System", "Are you sure you want to force shutdown your computer?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;

            if (response)
            {
                writeMessage("{SHUTDOWN}");

                string returnMessage = getDataAsString(TCP_Connection.TCP_Instance.client);

                if (returnMessage == null)
                {
                    return;
                }

                if (returnMessage == "{BEGAN_SHUTDOWN}")
                {
                    await dialog.ShowDialog("Shutdown Successful", "The server has received the message and has begun shutdown", false, Show_Dialog.MessageResult.OK);
                }
            }
        }

        private void MonitorOn_Clicked(object sender, EventArgs e)
        {
            writeMessage("{MONITOR_ON}");

            string returnMessage = getDataAsString(TCP_Connection.TCP_Instance.client);

            if (returnMessage == null)
            {
                return;
            }

            if (returnMessage == "{MONITOR_TURNED_ON}")
            {
                dialog.ShowDialog("Display Settings", "The server has received the message and has turned the monitor on", false, Show_Dialog.MessageResult.OK);
            }
        }

        private void MonitorOff_Clicked(object sender, EventArgs e)
        {
            writeMessage("{MONITOR_OFF}");

            string returnMessage = getDataAsString(TCP_Connection.TCP_Instance.client);

            if (returnMessage == null)
            {
                return;
            }

            if (returnMessage == "{MONITOR_TURNED_OFF}")
            {
                dialog.ShowDialog("Display Settings", "The server has received the message and has turned the monitor off", false, Show_Dialog.MessageResult.OK);
            }
        }

        private void TakeScreenshot_Clicked(object sender, EventArgs e)
        {
            StartActivity(typeof(PicturePage));
            writeMessage("{TAKE_SCREENSHOT}");

            //var data = getData(Connection.Instance.client);

            //if (data == null)
            //{
            //    return;
            //}

            //screenshotView.SetImageBitmap(Android.Graphics.Bitmap.CreateBitmap(Android.Graphics.BitmapFactory.DecodeByteArray(data, 0, data.Length)));//ImageSource.FromStream(() => new MemoryStream(data));

        }

        private async void PCLock_Clicked(object sender, EventArgs e)
        {
            if (PCLockStatus)
            {
                bool response = await dialog.ShowDialog("Unlock PC?", "Are you sure you want to unlock your computer?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;

                if (response)
                {
                    writeMessage("{LOCK_PC}");

                    if (getDataAsString(TCP_Connection.TCP_Instance.client) == "{PC_LOCKED}")
                    {
                        dialog.ShowDialog("Unlock Successful", "The Computer Recieved your PC Unlock Message", false, Show_Dialog.MessageResult.OK);
                        PCLockStatus = !PCLockStatus;

                    }
                }
                else
                {
                    dialog.ShowDialog("Unlock PC Cancelled", "Your PC Unlock Attempt was Cancelled", false, Show_Dialog.MessageResult.OK);
                }
            }
            else
            {
                bool response = await dialog.ShowDialog("Lock PC?", "Are you sure you want to lock your computer?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;

                if (response)
                {
                    writeMessage("{LOCK_PC}");

                    if (getDataAsString(TCP_Connection.TCP_Instance.client) == "{PC_LOCKED}")
                    {
                        dialog.ShowDialog("Lock Successful", "The Computer Recieved your PC Lock Message", false, Show_Dialog.MessageResult.OK);
                        PCLockStatus = !PCLockStatus;
                    }
                }
                else
                {
                    dialog.ShowDialog("Lock PC Cancelled", "Your PC Lock Attempt was Cancelled", false, Show_Dialog.MessageResult.OK);
                }

                if (PCLockStatus)
                {
                    PCLockButton.Text = "Lock Computer (Currently: Locked)";
                }
                else
                {
                    PCLockButton.Text = "Lock Computer (Currently: Unlocked)";
                }
            }
        }
        Stopwatch swr = new Stopwatch();
        Stopwatch sww = new Stopwatch();
        private void Ping_Clicked(object sender, EventArgs e)
        {
            string information = "Write : Read";
            for (int i = 0; i < 8; i++)
            {
                sww.Reset();
                sww.Start();

                //writeMessage("{SPEED_TEST}" + kbbuffer.ToString() + "{/SPEED_TEST}");
                sww.Stop();
                //int actualReadBytes = Encoding.UTF8.GetBytes("{SPEED_TEST}" + kbbuffer.ToString() + "{/SPEED_TEST}").Length;
                //long speedWrite = (actualReadBytes * 8) / (int)sww.ElapsedMilliseconds;

                //information += speedWrite + "mbps";

                swr.Reset();
                swr.Start();
                int byteLength = getData(TCP_Connection.TCP_Instance.client).Length;
                swr.Stop();
                long speedRead = (byteLength * 8) / (int)swr.ElapsedMilliseconds;

                information += " : " + speedRead + "mbps\n";
            }

            dialog.ShowDialog("Speed Test", information, false, Show_Dialog.MessageResult.OK);
            
        }

        public void TryResumeActivity(Type className)
        {
            Intent openActivity = new Intent(this, className);
            openActivity.SetFlags(ActivityFlags.ReorderToFront);
            StartActivityIfNeeded(openActivity, 0);
        }

        public /*async*/ void writeMessage(string input)
        {
            try
            {
                TcpClient client = TCP_Connection.TCP_Instance.client;
                if (client == null)
                {
                    throw new ObjectDisposedException(client.ToString());
                }
                NetworkStream ns = client.GetStream();
                byte[] message = Encoding.ASCII.GetBytes(input);
                ns.Write(message, 0, message.Length);
            }
            catch (Exception ex)
            {
                if (MainActivity.analyticsEnabled)
                {
                    MainActivity.developerExceptionList.Add(ex);
                }

                //await dialog.ShowDialog("Error", ex.ToString(), false, Show_Dialog.MessageResult.OK);
                ClientDisconnected();
            }
        }

        public string getDataAsString(TcpClient client)
        {
            byte[] bytes = getData(client);
            if (bytes != null)
            {
                return Encoding.ASCII.GetString(bytes);
            }
            else
            {
                return null;
            }
        }

        public byte[] getData(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

                int bytesLeft = dataLength;
                byte[] data = new byte[dataLength];

                int buffersize = 1024;
                int bytesRead = 0;

                while (bytesLeft > 0)
                {
                    int curDataSize = Math.Min(buffersize, bytesLeft);
                    if (client.Available < curDataSize)
                    {
                        curDataSize = client.Available;
                    }

                    bytes = stream.Read(data, bytesRead, curDataSize);
                    bytesRead += curDataSize;
                    bytesLeft -= curDataSize;
                }

                return data;
            }
            catch (Exception ex)
            {
                if (MainActivity.analyticsEnabled)
                {
                    MainActivity.developerExceptionList.Add(ex);
                }

                //dialog.ShowDialog("Error", ex.ToString(), false, Show_Dialog.MessageResult.OK);
                ClientDisconnected();
                return null;
            }
        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
    }
}