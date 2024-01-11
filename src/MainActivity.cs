using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.IO;
using System.Xml.Serialization;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using AlertDialog = Android.App.AlertDialog;
using Android.Content;
using System.Text.RegularExpressions;
//using Java.Lang;
using System.Threading;
using Android.Views;
using Android.Views.InputMethods;
using System.Collections.Generic;

namespace TCPCommanderAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        byte[] buffer = new byte[1];
        string folderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string preferencesFile = "/prefs.xml";
        bool prefsExist;

        EditText ipAddress;
        EditText TCP_Port;
        EditText UDP_Port;
        Button connectButton;
        StoredData readStoredData;
        CheckBox developeranalyticsmodeCheck;
        public static bool succesfullyDisconnected;
        public static bool clientIsOpen;
        public static bool lostConnection;
        public static bool alertSuccessfulConnection;
        public static bool analyticsEnabled;
        //public bool readFile;
        Show_Dialog dialog;

        public static List<Exception> developerExceptionList = new List<Exception>();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            dialog = new Show_Dialog(this);

            ipAddress = FindViewById<EditText>(Resource.Id.ipAddress);
            TCP_Port = FindViewById<EditText>(Resource.Id.TCP_Port);
            UDP_Port = FindViewById<EditText>(Resource.Id.UDP_Port);
            connectButton = FindViewById<Button>(Resource.Id.connectButton);
            developeranalyticsmodeCheck = FindViewById<CheckBox>(Resource.Id.developeranalyticsmodeCheck);
            developeranalyticsmodeCheck.CheckedChange += Analytics_Checked;

            ipAddress.EditorAction += HandleEditorAction;
            TCP_Port.EditorAction += HandleEditorAction;
            UDP_Port.EditorAction += HandleEditorAction;

            ipAddress.TextChanged += IPAddress_TextChanged;
            connectButton.Click += Connect_Clicked;
            //Port.TextChanged += Port_TextChanged;


            if (Directory.GetFiles(folderPath).Length > 0)
            {
                if (File.Exists(folderPath + preferencesFile))
                {
                    prefsExist = true;
                    XmlSerializer Serializer = new XmlSerializer(typeof(StoredData));
                    FileStream fs = new FileStream(folderPath + preferencesFile, FileMode.Open);
                    readStoredData = (StoredData)Serializer.Deserialize(fs);
                    ipAddress.Text = readStoredData.PrivateIPV4;
                    TCP_Port.Text = readStoredData.TCP_Port;
                    UDP_Port.Text = readStoredData.UDP_Port;
                    fs.Close();
                }
            }
        }

        public async override void OnBackPressed()
        {
            bool response = await dialog.ShowDialog("Quit?", "Would you like to close the application?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;

            if (response)
            {
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
            }
        }

        protected async override void OnResume()
        {
            developeranalyticsmodeCheck.Checked = analyticsEnabled;

            base.OnResume();

            if (succesfullyDisconnected)
            {
                lockButton(true, 0);
                succesfullyDisconnected = false;
                dialog.ShowDialog("Disconnection", "Succesfully disconnected from server!", false, Show_Dialog.MessageResult.OK);
                await Task.Delay(1000);
                lockButton(false, 0);
            }


            if (lostConnection)
            {
                lostConnection = false;
                lockButton(true, 0);
                await Task.Delay(1000);
                lockButton(false, 0);
                ConnectionLost();
            }

            if (analyticsEnabled)
            {
                if (developerExceptionList.Count > 0)
                {
                    bool response = await dialog.ShowDialog("Developer Analytics Mode", "Would you like to see all the previous error messages?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;

                    if (response)
                    {
                        string errorList = "";
                        for (int i = 0; i < developerExceptionList.Count; i++)
                        {
                            errorList += developerExceptionList[i].GetType().ToString() + ":\n" + developerExceptionList[i].Message + "\n\n";
                        }

                        await dialog.ShowDialog("Errors Caught: (" + developerExceptionList.Count + ")", errorList, false, Show_Dialog.MessageResult.OK);
                        developerExceptionList.Clear();
                    }
                }
            }
        }


        protected override void OnPause()
        {
            base.OnPause();
            if (clientIsOpen)
            {
                lockButton(true, 0);
            }
        }

        public async void ConnectionLost()
        {
            bool response = await dialog.ShowDialog("Connection Lost", "The client has lost connection to the server.\nWould you like to reconnect?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true  : false;

            if (response)
            {
                Connect_Clicked(new object(), new EventArgs());
            }
        }
        public async void lockButton(bool locked, int timeout)
        {
            if (timeout > 0)
            {
                await Task.Delay(timeout);
            }
            new Handler(MainLooper).Post(new Java.Lang.Runnable(() =>
            {
                connectButton.Enabled = !locked;
                connectButton.Clickable = !locked;
            }));
        }

        int tryConnectTime = 1000;
        public async Task<TcpClient> tryConnect()
        {
            try
            {
                if (client == null)
                {
                    client = new TcpClient();
                }

                var connectionTask = client.ConnectAsync(ipAddress.Text, Convert.ToInt32(TCP_Port.Text)).ContinueWith(task =>
                {
                    return task.IsFaulted ? null : client;
                }, TaskContinuationOptions.ExecuteSynchronously);
                var timeoutTask = Task.Delay(tryConnectTime).ContinueWith<TcpClient>(task => null, TaskContinuationOptions.ExecuteSynchronously);
                var resultTask = Task.WhenAny(connectionTask, timeoutTask).Unwrap();
                resultTask.Wait();
                var resultTcpClient = await resultTask;

                return resultTcpClient;
            }
            catch (Exception ex)
            {
                if (analyticsEnabled)
                {
                    developerExceptionList.Add(ex);
                }
                await dialog.ShowDialog("Error", ex.ToString(), false, Show_Dialog.MessageResult.OK);
                return null;
            }
        }

        public void MasterConnection()
        {
            lockButton(true, 0);
            client = tryConnect().Result;
            lockButton(false, 250);
        }

        TcpClient client = new TcpClient();
        private async void Connect_Clicked(object sender, EventArgs e)
        {
            if (ipAddress.Text == "" || TCP_Port.Text == "" || UDP_Port.Text == "")
            {
                await dialog.ShowDialog("Error", "The IPAddress or Ports was blank!", false, Show_Dialog.MessageResult.OK);
                return;
            }

            try
            {
                MasterConnection();

                if (client != null)
                {
                    clientIsOpen = true;
                    TCP_Connection.TCP_Instance.client = client;
                    //TryResumeActivity(typeof(FunctionsPage));
                    StartActivity(typeof(FunctionsPage));

                    alertSuccessfulConnection = true;
                    FunctionsPage.storedprivIP = ipAddress.Text;

                    if (!prefsExist || ipAddress.Text != readStoredData.PrivateIPV4 || TCP_Port.Text != readStoredData.TCP_Port || UDP_Port.Text != readStoredData.UDP_Port)
                    {
                        XmlSerializer Serializer = new XmlSerializer(typeof(StoredData));
                        StoredData SD = new StoredData();
                        SD.PrivateIPV4 = ipAddress.Text;
                        SD.TCP_Port = TCP_Port.Text;
                        SD.UDP_Port = UDP_Port.Text;

                        TextWriter Writer = new StreamWriter(folderPath + preferencesFile);
                        Serializer.Serialize(Writer, SD);
                        Writer.Close();
                    }
                }
                else
                {
                    clientIsOpen = false;
                    bool reconnect = await dialog.ShowDialog("Connection Unsuccessful", "Could not connect to the server!\nWould you like to try to reconnect?", false, Show_Dialog.MessageResult.YES, Show_Dialog.MessageResult.NO) == Show_Dialog.MessageResult.YES ? true : false;
                    if (reconnect)
                    {
                        Connect_Clicked(new object(), new EventArgs());
                    }

                }
            }
            catch (Exception ex)
            {
                if (analyticsEnabled)
                {
                    developerExceptionList.Add(ex);
                }
                await dialog.ShowDialog("Error", ex.ToString(), false, Show_Dialog.MessageResult.OK);
                clientIsOpen = false;
            }
        }

        private void IPAddress_TextChanged(object sender, EventArgs e)
        {
            //ipAddress.Text.RemoveLetters();
            //ipAddress.Text.RemoveSpecialChars();

            //if (Regex.IsMatch(ipAddress.Text, @"[^0-9.]"))
            //{
            //    ipAddress.Text = Regex.Replace(ipAddress.Text, @"[^0-9.]", string.Empty);
            //    ipAddressIgnoreChange = true;
            //}

            if (ipAddress.Text.Contains(".."))
            {
                int location = ipAddress.Text.IndexOf("..");
                ipAddress.Text = ipAddress.Text.Replace("..", ".");
                ipAddress.SetSelection(location + 1);
            }
        }

        public void TryResumeActivity(Type className)
        {
            Intent openActivity = new Intent(this, className);
            openActivity.SetFlags(ActivityFlags.ReorderToFront);
            StartActivityIfNeeded(openActivity, 0);
        }

        private void HandleEditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            e.Handled = false;
            if (e.ActionId == ImeAction.Next)
            {
                EditText etSender = ((EditText)sender);
                if (etSender.Hint == "IP Address")
                {
                    TCP_Port.RequestFocus();
                    TCP_Port.SetSelection(TCP_Port.Text.Length);
                }
                else if (etSender.Hint == "TCP Port")
                {
                    UDP_Port.RequestFocus();
                    UDP_Port.SetSelection(UDP_Port.Text.Length);
                }
            } else if (e.ActionId == ImeAction.Go)
            {
                InputMethodManager IM = (InputMethodManager)GetSystemService(Context.InputMethodService);
                IM.HideSoftInputFromWindow(UDP_Port.WindowToken, 0);
                UDP_Port.ClearFocus();
                connectButton.PerformClick();
            }
            e.Handled = true;
        }

        private void Analytics_Checked(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                analyticsEnabled = true;
            }
            else
            {
                analyticsEnabled = false;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    public class StoredData
    {
        public string PrivateIPV4;
        public string TCP_Port;
        public string UDP_Port;
    }

    public class Show_Dialog
    {

        public enum MessageResult
        {
            NONE = 0,
            OK = 1,
            CANCEL = 2,
            ABORT = 3,
            RETRY = 4,
            IGNORE = 5,
            YES = 6,
            NO = 7
        }

        Activity mcontext;
        public Show_Dialog(Activity activity) : base()
        {
            this.mcontext = activity;
        }



        public Task<MessageResult> ShowDialog(string Title, string Message, bool SetCancelable = false, /*bool SetInverseBackgroundForced = false,*/ MessageResult PositiveButton = MessageResult.OK, MessageResult NegativeButton = MessageResult.NONE, MessageResult NeutralButton = MessageResult.NONE, int IconAttribute = Android.Resource.Attribute.AlertDialogIcon)
        {
            var tcs = new TaskCompletionSource<MessageResult>();

            var builder = new AlertDialog.Builder(mcontext);
            builder.SetIconAttribute(IconAttribute);
            builder.SetTitle(Title);
            builder.SetMessage(Message);
            //builder.SetInverseBackgroundForced(SetInverseBackgroundForced);
            builder.SetCancelable(SetCancelable);

            builder.SetPositiveButton((PositiveButton != MessageResult.NONE) ? PositiveButton.ToString() : string.Empty, (senderAlert, args) =>
            {
                tcs.SetResult(PositiveButton);
            });
            builder.SetNegativeButton((NegativeButton != MessageResult.NONE) ? NegativeButton.ToString() : string.Empty, delegate
            {
                tcs.SetResult(NegativeButton);
            });
            builder.SetNeutralButton((NeutralButton != MessageResult.NONE) ? NeutralButton.ToString() : string.Empty, delegate
            {
                tcs.SetResult(NeutralButton);
            });


            builder.Show();

            return tcs.Task;
        }
    }
}