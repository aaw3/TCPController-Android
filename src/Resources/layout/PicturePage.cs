using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TCPCommanderAndroid
{
    [Activity(Label = "PicturePage", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class PicturePage : Activity
    {
        ImageView screenshotView;
        Show_Dialog dialog;
        LinearLayout linearlayout;
        public bool startingUp;
        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            EnterImmersiveMode();
            SetContentView(Resource.Layout.picture_page);
            //screenshotView = FindViewById<ImageView>(Resource.Id.screenshotView);
            screenshotView = new ImageView(this);//new ImageViewExt(this);//new ImageView(this); //ADD IMAGEVIEWEXT(THIS); LATER
            linearlayout = FindViewById<LinearLayout>(Resource.Id.picturepageLinearLayout);
            //screenshotView.SetBackgroundColor(Color.Black);
            //screenshotView.SetBackgroundColor(Color.White);
            dialog = new Show_Dialog(this);

            screenshotView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);//(bitmap.Width, bitmap.Height);

            //if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBean)
            //{
            //RequestWindowFeature(WindowFeatures.NoTitle);
            //Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            //}
            //else
            //{
            //    Window.DecorView.SystemUiVisibility = StatusBarVisibility.Hidden;
            //    //ActionBar.Hide();
            //}

            //PowerManager pm = (PowerManager)GetSystemService(Context.PowerService);
            //PowerManager.WakeLock wl = pm.NewWakeLock(WakeLockFlags.ScreenDim, "ScreenDim");
            //wl.Acquire();
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            linearlayout.AddView(screenshotView);


            //timer = new System.Timers.Timer();
            //timer.Interval = 1000;
            //timer.Enabled = true;
            startingUp = true;
            await Task.Delay(500); //may be too much time
            startingUp = false;
            //timer.Start();
            //timer.Elapsed += (sender, e) =>
            //{
                ReplaceImage();
            //};
        }

        public void EnterImmersiveMode()
        {
            int uiOptions = (int)Window.DecorView.SystemUiVisibility;
            //uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
        }

        //System.Timers.Timer timer;
        protected override void OnResume() //WASTED 3 HOURS BECAUSE OF picture_page.xml's LINEARLAYOUT HAD android:foreground="@android:color/black"!!!! FIXED NOW
        {
            base.OnResume();

            //screenshotView.Enabled = true;
            //screenshotView.Visibility = ViewStates.Visible;
            //screenshotView.RequestLayout();

            //linearlayout.BringChildToFront(screenshotView);

            if (!startingUp)
            {
                //timer.Start();
            }
        }

        bool happenedOnce;
        bool happenedTwice;
        string part1;
        string part2;
        bool initiated;
        byte[] data;
        public async void ReplaceImage()
        {
            //if (!happenedOnce)
            //{
            //    part1 = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //    happenedOnce = true;
            //}
            //else
            //{
            //    part2 = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //    await Task.Run(() => RunOnUiThread(async () =>
            //    {
            //        await dialog.ShowDialog("Time Taken", part1 + " : " + part2, false, Show_Dialog.MessageResult.OK);
            //    }));
            //}

                await Task.Run(() => askForScreenshot());
                data = await Task.Run(() => getScreenshot(TCP_Connection.TCP_Instance.client));
                initiated = true;

            if (data == null)
            {
                await Task.Run(() => ReplaceImage());
            }

            await Task.Run(() => RunOnUiThread(() =>
            {
                //screenshotView.setimage
                screenshotView.SetImageBitmap(Android.Graphics.Bitmap.CreateBitmap(Android.Graphics.BitmapFactory.DecodeByteArray(data, 0, data.Length)));//ImageSource.FromStream(() => new MemoryStream(data));
                //await dialog.ShowDialog("Data Received", "Bytes: " + data.Length, false, Show_Dialog.MessageResult.OK);
            }));
            //if (!happenedTwice)
            //{
            //    happenedTwice = true;
                await Task.Run(() => ReplaceImage());
            //}

            //string part1 = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //if (!Java.Lang.Thread.Interrupted())
            //{
                //RunOnUiThread(() =>
                //{
                //    try
                //    {

                //    screenshotView.SetImageBitmap(Android.Graphics.Bitmap.CreateBitmap(Android.Graphics.BitmapFactory.DecodeByteArray(data, 0, data.Length)));//ImageSource.FromStream(() => new MemoryStream(data));
                //    }
                //    catch (Exception ex)
                //    {
                //        dialog.ShowDialog("Error", ex.GetType().ToString() + ": " + ex.Message, false, Show_Dialog.MessageResult.OK);
                //    }
                //});
            //}
            //string part2 = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            //dialog.ShowDialog("Times", "Started at: " + part1 + "\nEnded at: " + part2, false, Show_Dialog.MessageResult.OK);
        }

        protected override void OnPause()
        {
            base.OnPause();

            //timer.Stop();
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

        //public async Task<byte[]> getScreenshot(TcpClient client)
        //{
        //    try
        //    {
        //        NetworkStream stream = client.GetStream();
        //        byte[] fileSizeBytes = new byte[4];
        //        int bytes = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
        //        int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

        //        int bytesLeft = dataLength;
        //        byte[] data = new byte[dataLength]; //System.OverflowException: 'Arithmetic operation resulted in an overflow.' KEEP HAPPENING

        //        int buffersize = (1024 * 1024) / 2;
        //        int bytesRead = 0;

        //        while (bytesLeft > 0)
        //        {
        //            int curDataSize = Math.Min(buffersize, bytesLeft);
        //            if (client.Available < curDataSize)
        //            {
        //                curDataSize = client.Available;
        //            }

        //            bytes = stream.Read(data, bytesRead, curDataSize);
        //            bytesRead += curDataSize;
        //            bytesLeft -= curDataSize;
        //        }

        //        return data;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (MainActivity.analyticsEnabled)
        //        {
        //            MainActivity.developerExceptionList.Add(ex);
        //        }

        //        //dialog.ShowDialog("Error", ex.ToString(), false, Show_Dialog.MessageResult.OK);
        //        ClientDisconnected();
        //        return null;
        //    }
        //}

        public async Task<byte[]> getScreenshot(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] fileSizeBytes = new byte[4];
                int bytes = stream.Read(fileSizeBytes, 0, fileSizeBytes.Length);
                int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

                int bytesLeft = dataLength;
                byte[] data = new byte[dataLength]; //System.OverflowException: 'Arithmetic operation resulted in an overflow.' KEEP HAPPENING

                int buffersize = (1024 * 1024) / 2;
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

        public void askForScreenshot()
        {
            //try
            {
                TcpClient client = TCP_Connection.TCP_Instance.client;
                if (client == null)
                {
                    throw new ObjectDisposedException(client.ToString());
                }
                NetworkStream ns = client.GetStream();
                byte[] message = Encoding.ASCII.GetBytes("{TAKE_SCREENSHOT}");
                ns.Write(message, 0, message.Length);
            }
            //catch (Exception ex)
            //{
            //    if (MainActivity.analyticsEnabled)
            //    {
            //        MainActivity.developerExceptionList.Add(ex);
            //    }

            //    //await dialog.ShowDialog("Error", ex.ToString(), false, Show_Dialog.MessageResult.OK);
            //    ClientDisconnected();
            //}
        }
    }

    class ImageViewExt : ImageView
    {
        private static int INVALID_POINTER_ID = -1;
        private float mPosX;
        private float mPosY;

        private float mLastTouchX;
        private float mLastTouchY;

        private float mLastGestureX;
        private float mLastGestureY;
        private int mActivePointerId = INVALID_POINTER_ID;

        private ScaleGestureDetector mScaleDetector;
        private static float mScaleFactor = 1.0f;

        public ImageViewExt(Context context) : base(context)
        {
            mScaleDetector = new ScaleGestureDetector(Context, new ScaleListener());
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            mScaleDetector.OnTouchEvent(e);

            //int action = e.Action;
            switch (e.Action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                    if (!mScaleDetector.IsInProgress)
                    {
                        float x = e.GetX();
                        float y = e.GetY();

                        mLastTouchX = x;
                        mLastTouchY = y;
                        mActivePointerId = e.GetPointerId(0);
                    }
                    break;
                case MotionEventActions.Pointer1Down:
                    if (mScaleDetector.IsInProgress)
                    {
                        float gx = mScaleDetector.FocusX;
                        float gy = mScaleDetector.FocusY;

                        mLastGestureX = gx;
                        mLastGestureY = gy;
                    }
                    break;
                case MotionEventActions.Move:
                    if (!mScaleDetector.IsInProgress)
                    {
                        int pointerIdx = e.FindPointerIndex(mActivePointerId);
                        float x = e.GetX(pointerIdx);
                        float y = e.GetY(pointerIdx);

                        float dx = x - mLastTouchX;
                        float dy = y - mLastTouchY;

                        mPosX += dx;
                        mPosY += dy;

                        Invalidate();

                        mLastTouchX = x;
                        mLastTouchY = y;
                    }
                    else
                    {
                        float gx = mScaleDetector.FocusX;
                        float gy = mScaleDetector.FocusY;

                        float gdx = gx - mLastGestureX;
                        float gdy = gy - mLastGestureY;

                        mPosX += gdx;
                        mPosY += gdy;

                        Invalidate();

                        mLastGestureX = gx;
                        mLastGestureY = gy;
                    }
                    break;
                case MotionEventActions.Up:
                    mActivePointerId = INVALID_POINTER_ID;
                    break;
                case MotionEventActions.Cancel:
                    mActivePointerId = INVALID_POINTER_ID;
                    break;
                case MotionEventActions.PointerUp:

                    int pointerIdx2 = (int)(e.Action & MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
                    int pointerId = e.GetPointerId(pointerIdx2);

                    if (pointerId == mActivePointerId)
                    {
                        int NewPointerIndex = pointerIdx2 == 0 ? 1 : 0;
                        mLastTouchX = e.GetX(NewPointerIndex);
                        mLastTouchY = e.GetY(NewPointerIndex);
                        mActivePointerId = e.GetPointerId(NewPointerIndex);
                    }
                    else
                    {
                        int TempPointerIdx = e.FindPointerIndex(mActivePointerId);
                        mLastTouchX = e.GetX(TempPointerIdx);
                        mLastTouchY = e.GetY(TempPointerIdx);
                    }
                    break;
            }

            return true;
        }

        protected override void OnDraw(Canvas canvas)
        {
            canvas.Save();

            canvas.Translate(mPosX, mPosY);
            if (mScaleDetector.IsInProgress)
            {
                canvas.Scale(mScaleFactor, mScaleFactor, mScaleDetector.FocusX, mScaleDetector.FocusY);
            }
            else
            {
                canvas.Scale(mScaleFactor, mScaleFactor, mLastGestureX, mLastGestureY);
            }
            base.OnDraw(canvas);
            canvas.Restore();
        }


        private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            public override bool OnScale(ScaleGestureDetector detector)
            {
                mScaleFactor *= detector.ScaleFactor;

                mScaleFactor = Math.Max(0.1f, Math.Min(mScaleFactor, 10.0f));

                return true;
            }

        }
    }
}