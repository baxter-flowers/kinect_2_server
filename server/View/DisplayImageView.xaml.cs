using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kinect2Server.View
{
    public partial class ColorImageView : UserControl
    {
        private MainWindow mw;
        private ColorImage ci;
        private DepthImage di;
        private Boolean display;

        private WriteableBitmap colorBitmap;
        private WriteableBitmap depthBitmap;
        private const int MapDepthToByte = 8000 / 256;
        private byte[] depthPixels;
        private FrameDescription depthFrameDescription;
        private int size;

        private string statusText;

        public ColorImageView()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.ci = this.mw.ColorImage;
            this.di = this.mw.DepthImage;
            //this.ci.addCIListener(this.Reader_ColorFrameArrived);
            this.di.addDIListener(this.Reader_DepthFrameArrived);
            //this.ci.ColorFrameReader.IsPaused = true;
            this.di.DepthFrameReader.IsPaused = true;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.mw.KinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            this.depthFrameDescription=this.mw.KinectSensor.DepthFrameSource.FrameDescription;
            this.size = this.depthFrameDescription.Width * this.depthFrameDescription.Height;
            this.depthPixels = new byte[this.size];

            // create the bitmaps to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            this.display = false;

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public ImageSource ColorImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        public ImageSource DepthImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        private void switchDisplay(object sender, RoutedEventArgs e)
        {
            if (this.display)
            {
                this.display = false;
                this.setButtonOff(this.stackDisplay);
                this.di.DepthFrameReader.IsPaused = true;
                //this.ci.ColorFrameReader.IsPaused = true;
            }
            else
            {
                this.display = true;
                this.setButtonOn(this.stackDisplay);
                this.di.DepthFrameReader.IsPaused = false;
                //this.ci.ColorFrameReader.IsPaused = false;
            }
        }

        private void setButtonOff(StackPanel stack)
        {
            Image img = new Image();
            stack.Children.Clear();
            img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
            stack.Children.Add(img);
        }

        private void setButtonOn(StackPanel stack)
        {
            Image img = new Image();
            stack.Children.Clear();
            img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
            stack.Children.Add(img);
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.colorBitmap != null && this.depthBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder colorEncoder = new PngBitmapEncoder();
                BitmapEncoder depthEncoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                colorEncoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));
                depthEncoder.Frames.Add(BitmapFrame.Create(this.depthBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string colorPath = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");
                string depthPath = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Depth-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(colorPath, FileMode.Create))
                    {
                        colorEncoder.Save(fs);
                    }

                    using (FileStream fs = new FileStream(depthPath, FileMode.Create))
                    {
                        depthEncoder.Save(fs);
                    }

                    //this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, colorPath);
                }
                catch (IOException)
                {
                    //this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, colorPath);
                }
            }
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            uint size = (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4);
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                size,
                                ColorImageFormat.Bgra);

                            this.ci.colorFrameToByteArray((int)size, colorFrame);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));

                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        private void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.size) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            this.di.depthFrameToByteArray(this.size, depthFrame);

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
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
