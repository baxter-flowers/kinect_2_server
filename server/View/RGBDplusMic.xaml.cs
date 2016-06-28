using Microsoft.Kinect;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2Server.View
{
    public enum Mode
    {
        Color,
        Depth,
        Mapped
    }

    public partial class RGBDplusMic : UserControl
    {
        private MainWindow mw;
        private MultiSourceImage msi;
        private AudioFrame af;
        private Boolean display;
        private Boolean mic;
        private Boolean continousStream;
        private Mode mode;
        private int frameCount;
        private int t0_seconds;

        public RGBDplusMic()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.msi = this.mw.MultiSourceImage;
            this.af = this.mw.AudioFrame;
            this.msi.addMSIListener(this.Reader_MultiSourceFrameArrived);
            this.mode = Mode.Color;
            this.DataContext = this;
            this.display = false;
            this.mic = false;
            this.continousStream = false;


            InitializeComponent();

            this.statusBarItem.Content = "Streaming & recording off";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Objec that sent the event</param>
        /// <param name="e">State information</param>
        private void SwitchDisplay(object sender, RoutedEventArgs e)
        {
            if (this.display)
            {
                this.SetButtonOff(this.stackDisplay, "rgbd");
                if(this.mic)
                    this.statusBarItem.Content = "Streaming off. Recording on";
                else
                    this.statusBarItem.Content = "Streaming & recording off";
            }
            else
            {
                this.SetButtonOn(this.stackDisplay, "rgbd");
                this.frameCount = 0;
                this.t0_seconds = DateTime.Now.Second;
                if (this.mic)
                    this.statusBarItem.Content = "Streaming RGB-D images & recording";
                else
                    this.statusBarItem.Content = "Streaming RGB-D images. Recording off";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Objec that sent the event</param>
        /// <param name="e">State information</param>
        private void SwitchMic(object sender, RoutedEventArgs e)
        {
            if (this.mic)
            {
                this.SetButtonOff(this.stackMic, "mic");
                if (this.display)
                    this.statusBarItem.Content = "Streaming RGB-D images. Recording off";
                else
                    this.statusBarItem.Content = "Streaming & recording off";
            }
            else
            {
                this.SetButtonOn(this.stackMic, "mic");
                if (this.display)
                    this.statusBarItem.Content = "Streaming RGB-D images & recording";
                else
                    this.statusBarItem.Content = "Streaming off. Recording on";

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Objec that sent the event</param>
        /// <param name="e">State information</param>
        private void SwitchSending(object sender, RoutedEventArgs e)
        {
            if (this.continousStream)
                this.SetButtonOff(this.stackSending, "sending");
            else
                this.SetButtonOn(this.stackSending, "sending");
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            this.mode = Mode.Color;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            this.mode = Mode.Depth;
        }

        private void Mapped_Click(object sender, RoutedEventArgs e)
        {
            this.mode = Mode.Mapped;
        }

        public void SetButtonOff(StackPanel stack, String param)
        {
            Dispatcher.Invoke(() =>
            {
                if (param.Equals("sending"))
                {
                    this.continousStream = false;
                    this.msi.Request_Reply = true;
                }
                else if (param.Equals("rgbd"))
                {
                    this.display = false;
                    this.msi.MultiSourceFrameReader.IsPaused = true;
                }

                else if (param.Equals("mic"))
                {
                    this.mic = false;
                    this.af.AudioBeamFrameReader.IsPaused = true;
                }
                    
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
                stack.Children.Add(img);
            });
        }

        public void SetButtonOn(StackPanel stack, String param)
        {
            Dispatcher.Invoke(() =>
            {
                if (param.Equals("sending"))
                {
                    this.continousStream = true;
                    this.msi.Request_Reply = false;
                }
                else if (param.Equals("rgbd"))
                {
                    this.display = true;
                    this.msi.MultiSourceFrameReader.IsPaused = false;
                }

                else if (param.Equals("mic"))
                {
                    this.mic = false;
                    this.af.AudioBeamFrameReader.IsPaused = false;
                }

                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
                stack.Children.Add(img);
            });
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">State information</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // create jpeg encoder
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            // create frame from the camera source and add to encoders
            if (this.camera.Source == null)
                return;
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)this.camera.Source));

            string time = DateTime.Now.ToString("dd'-'MMM'-'HH'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = System.IO.Path.Combine(myPhotos, "KinectScreenshot-" + time + ".jpeg");

            // write the new file to disk
            try
            {
                // FileStream is IDisposable
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarItem.Content = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, myPhotos);
            }
            catch (IOException)
            {
                this.statusBarItem.Content = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, myPhotos);
            }
        }

        /// <summary>
        /// Handles the depth and color frames data arriving from the sensor
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">Event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if ((!this.continousStream && !this.msi.RepColorDelivered && !this.msi.RepMappingDelivered && !this.msi.RepMaskDelivered)||this.continousStream)
            {
                this.frameCount++;
                if (DateTime.Now.Second - this.t0_seconds == 1)
                {
                    this.FPS.Content = "FPS:" + this.frameCount;
                    this.frameCount = 0;
                    this.t0_seconds = DateTime.Now.Second;
                }
                this.msi.RepColorDelivered = true;
                this.msi.RepMappingDelivered = true;
                this.msi.RepMaskDelivered = true;
                ColorFrame colorFrame = null;
                DepthFrame depthFrame = null;

                MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
                if (multiSourceFrame == null)
                    return;

                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();

                if (colorFrame == null | depthFrame == null)
                    return;

                this.camera.Source = this.msi.ProcessFramesData(colorFrame, depthFrame, this.mode.ToString());
            }
        }
    }
}
