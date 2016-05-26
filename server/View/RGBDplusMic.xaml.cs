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
        Depth
    }

    public partial class RGBDplusMic : UserControl
    {
        private MainWindow mw;
        private MultiSourceImage msi;
        private Boolean display;
        private Mode mode;
        private DateTime dt;

        private WriteableBitmap colorBitmap;
        private WriteableBitmap depthBitmap;
        private const int MapDepthToByte = 8000 / 256;
        private Byte[] depthPixels;
        private FrameDescription depthFrameDescription;
        private int size;
        private Image img;

        public RGBDplusMic()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.msi = this.mw.MultiSourceImage;
            this.msi.addMSIListener(this.Reader_MultiSourceFrameArrive);
            this.msi.MultiSourceFrameReader.IsPaused = true;
            this.mode = Mode.Color;
            
            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.mw.KinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            this.depthFrameDescription=this.mw.KinectSensor.DepthFrameSource.FrameDescription;
            this.size = this.depthFrameDescription.Width * this.depthFrameDescription.Height;
            this.depthPixels = new Byte[this.size];

            // create the bitmaps to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            this.display = false;
            this.img = new Image();
            this.dt = DateTime.Now;
            
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
                this.statusBarItem.Content = "Streaming RGB-D + microphone";
                this.dt = DateTime.Now;
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

        public void setButtonOff(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
                stack.Children.Add(img);
            });
        }

        public void setButtonOn(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
                stack.Children.Add(img);
            });
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.colorBitmap != null && this.depthBitmap != null)
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
            }
        }

        private void Reader_MultiSourceFrameArrive(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (this.msi.FrameCount != 1)
            {
                MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

                // ColorFrame is IDisposable
                using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    if (colorFrame != null)
                    {
                        this.colorBitmap.Lock();
                        this.colorBitmap = this.msi.LetsFindABetterNameLater(colorFrame, this.colorBitmap);
                        if (this.mode == Mode.Color)
                        {
                            this.camera.Source = this.colorBitmap;
                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }
                        this.colorBitmap.Unlock();
                    }
                }

                bool depthFrameProcessed = false;
                // DepthFrame is IDisposable
                using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                {
                    if (depthFrame != null)
                    {
                        this.depthPixels = this.msi.AndAnotherOne(depthFrame, this.depthBitmap, this.depthPixels);
                        depthFrameProcessed = true;
                    }
                }

                if (depthFrameProcessed && this.mode == Mode.Depth)
                {
                    this.camera.Source = this.depthBitmap;
                    this.RenderDepthPixels();
                }

                this.msi.FrameCount++;
            }
            else
            {
                this.msi.FrameCount = 0;
            }
            
        }

        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }
    }
}
