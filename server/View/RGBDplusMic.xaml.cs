using Microsoft.Kinect;
using System;
using System.Globalization;
using System.IO;
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
        private Boolean display;
        private Mode mode;

        public RGBDplusMic()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.msi = this.mw.MultiSourceImage;
            this.msi.addMSIListener(this.Reader_MultiSourceFrameArrived);
            this.msi.MultiSourceFrameReader.IsPaused = true;
            this.mode = Mode.Color;
            this.DataContext = this;
            this.display = false;

            InitializeComponent();

            this.statusBarItem.Content = "Streaming off";
        }

        private void switchDisplay(object sender, RoutedEventArgs e)
        {
            if (this.display)
            {
                this.display = false;
                this.setButtonOff(this.stackDisplay);
                this.msi.MultiSourceFrameReader.IsPaused = true;
                this.statusBarItem.Content = "Streaming off";
            }
            else
            {
                this.display = true;
                this.setButtonOn(this.stackDisplay);
                this.msi.MultiSourceFrameReader.IsPaused = false;
                this.statusBarItem.Content = "Streaming RGB-D images + microphone";
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
            /*if (this.colorBitmap != null && this.depthBitmap != null)
            {
                // create a png bitmap & a jpg encoder
                BitmapEncoder colorEncoder = new JpegBitmapEncoder();
                BitmapEncoder depthEncoder = new PngBitmapEncoder();

                // create frames from the writable bitmaps and add to encoders
                colorEncoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));
                depthEncoder.Frames.Add(BitmapFrame.Create(this.depthBitmap));

                string time = DateTime.Now.ToString("dd'-'MMM'-'HH'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string colorPath = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".jpeg");
                string depthPath = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Depth-" + time + ".png");

                // write the new files to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(colorPath, FileMode.Create))
                    {
                        colorEncoder.Save(fs);
                    }

                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(depthPath, FileMode.Create))
                    {
                        depthEncoder.Save(fs);
                    }

                    this.statusBarItem.Content = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, myPhotos);
                }
                catch (IOException)
                {
                    this.statusBarItem.Content = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, myPhotos);
                }
            }*/
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            /*if (this.msi.FrameCount != 1)
            {*/
                ColorFrame colorFrame = null;
                DepthFrame depthFrame = null;

                try
                {
                    MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
                    if (multiSourceFrame == null)
                        return;

                    colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                    depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();

                    if (colorFrame == null | depthFrame == null)
                        return;

                    this.camera.Source = this.msi.FrameTreatment(colorFrame, depthFrame, this.mode.ToString());

                }
                catch { }
                finally
                {
                    if (colorFrame != null)
                        colorFrame.Dispose();
                    if (depthFrame != null)
                        depthFrame.Dispose();
                }
                /*this.msi.FrameCount++;
            }
            else
            {
                this.msi.FrameCount = 0;
            }*/
        }
    }
}
