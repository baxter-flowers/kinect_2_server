using Microsoft.Kinect;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private Boolean reqRep;
        private Mode mode;

        public RGBDplusMic()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.msi = this.mw.MultiSourceImage;
            this.af = this.mw.AudioFrame;
            this.msi.addMSIListener(this.Reader_MultiSourceFrameArrived);
            this.msi.MultiSourceFrameReader.IsPaused = true;
            this.mode = Mode.Color;
            this.DataContext = this;
            this.display = false;
            this.mic = false;
            this.reqRep = false;

            InitializeComponent();

            this.statusBarItem.Content = "Streaming & recording off";
        }

        private void switchDisplay(object sender, RoutedEventArgs e)
        {
            if (this.display)
            {
                this.display = false;
                this.setButtonOff(this.stackDisplay);
                this.msi.MultiSourceFrameReader.IsPaused = true;
                if(this.mic)
                    this.statusBarItem.Content = "Streaming off. Recording on";
                else
                    this.statusBarItem.Content = "Streaming & recording off";
            }
            else
            {
                this.display = true;
                this.setButtonOn(this.stackDisplay);
                this.msi.MultiSourceFrameReader.IsPaused = false;
                if (this.mic)
                    this.statusBarItem.Content = "Streaming RGB-D images & recording";
                else
                    this.statusBarItem.Content = "Streaming RGB-D images. Recording off";
            }
        }

        private void switchMic(object sender, RoutedEventArgs e)
        {
            if (this.mic)
            {
                this.mic = false;
                this.setButtonOff(this.stackMic);
                this.af.AudioBeamFrameReader.IsPaused = true;
                if (this.display)
                    this.statusBarItem.Content = "Streaming RGB-D images. Recording off";
                else
                    this.statusBarItem.Content = "Streaming & recording off";
            }
            else
            {
                this.mic = true;
                this.setButtonOn(this.stackMic);
                this.af.AudioBeamFrameReader.IsPaused = false;
                if (this.display)
                    this.statusBarItem.Content = "Streaming RGB-D images & recording";
                else
                    this.statusBarItem.Content = "Streaming off. Recording on";

            }
        }

        private void switchSending(object sender, RoutedEventArgs e)
        {
            if (this.reqRep)
            {
                this.reqRep = false;
                this.setButtonOff(this.stackSending);
                // TODO add network things
            }
            else
            {
                this.reqRep = true;
                this.setButtonOn(this.stackSending);
                // TODO add network things
            }
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

        public void setButtonOff(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
                stack.Children.Add(img);
            });
        }

        public void setButtonOn(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
                stack.Children.Add(img);
            });
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // create jpeg encoder
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            // create frame from the camera source and add to encoders
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

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            ColorFrame colorFrame = null;
            DepthFrame depthFrame = null;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null)
                return;

            colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
            depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();

            if (colorFrame == null | depthFrame == null)
                return;

            this.camera.Source = this.msi.FrameTreatment(colorFrame, depthFrame, this.mode.ToString());

            if (colorFrame != null)
                colorFrame.Dispose();
            if (depthFrame != null)
                depthFrame.Dispose();
        }
    }
}
