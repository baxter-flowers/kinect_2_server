using Microsoft.Kinect;
using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kinect2Server
{
    public class MultiSourceImage
    {
        private NetworkPublisher colorPublisher;
        private NetworkPublisher mappingPublisher;
        private NetworkPublisher maskPublisher;
        private KinectSensor kinect;
        private CoordinateMapper coordinateMapper;
        private MultiSourceFrameReader multiSourceFrameReader;
        private const int MapDepthToByte = 8000 / 256;
        private Byte[] colorPixels;
        private Byte[] colorPixelsSending;
        private Byte[] depthPixels;
        private Byte[] mask;
        private ushort[] depthPixelData;
        private ColorSpacePoint[] colorPoints;
        private Byte[] mappedPixels;
        private Boolean reqRep;
        private Boolean repColorDelivered;
        private Boolean repMappingDelivered;
        private Boolean repMaskDelivered;

        public MultiSourceImage(KinectSensor kinect)
        {
            this.kinect = kinect;
            this.coordinateMapper = this.kinect.CoordinateMapper;
            this.colorPublisher = new NetworkPublisher();
            this.colorPublisher.SetConflate();
            this.colorPublisher.Bind("33408");

            this.mappingPublisher = new NetworkPublisher();
            this.mappingPublisher.SetConflate();
            this.mappingPublisher.Bind("33409");

            this.maskPublisher = new NetworkPublisher();
            this.maskPublisher.SetConflate();
            this.maskPublisher.Bind("33410");
            
            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
            this.multiSourceFrameReader.IsPaused = true;
            this.reqRep = true;
            this.repColorDelivered = true;
            this.repMappingDelivered = true;
            this.repMaskDelivered = true;
            
        }

        public MultiSourceFrameReader MultiSourceFrameReader
        {
            get
            {
                return this.multiSourceFrameReader;
            }
        }

        public Boolean Request_Reply
        {
            get
            {
                return this.reqRep;
            }
            set
            {
                this.reqRep = value;
            }
        }

        public Boolean RepColorDelivered
        {
            get
            {
                return this.repColorDelivered;
            }
            set
            {
                this.repColorDelivered = value;
            }
        }

        public Boolean RepMappingDelivered
        {
            get
            {
                return this.repMappingDelivered;
            }
            set
            {
                this.repMappingDelivered = value;
            }
        }

        public Boolean RepMaskDelivered
        {
            get
            {
                return this.repMaskDelivered;
            }
            set
            {
                this.repMaskDelivered = value;
            }
        }

        public void ResetFrameBooleans()
        {
            this.repColorDelivered = false;
            this.repMappingDelivered = false;
            this.repMaskDelivered = false;
        }

        public void addMSIListener(EventHandler<MultiSourceFrameArrivedEventArgs> f)
        {
            this.multiSourceFrameReader.MultiSourceFrameArrived += f;
        }

        private void FindAName(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Arrays initialization & copy data, then dispose frames
            // Color
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;
            this.colorPixelsSending = new Byte[colorWidth * colorHeight * 2];
            colorFrame.CopyRawFrameDataToArray(this.colorPixelsSending);
            PixelFormat format = PixelFormats.Bgr32;
            this.colorPixels = new Byte[colorWidth * colorHeight * ((format.BitsPerPixel + 7) / 8)];
            colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
            colorFrame.Dispose();
            // Sending color data
            this.SendingColorData();
            

            // Depth
            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            ushort minDepth = depthFrame.DepthMinReliableDistance;
            ushort maxDepth = depthFrame.DepthMaxReliableDistance;

            this.depthPixels = new byte[depthWidth * depthHeight];
            this.depthPixelData = new ushort[depthWidth * depthHeight];
            depthFrame.CopyFrameDataToArray(this.depthPixelData);
            depthFrame.Dispose();
            this.DepthTreatment(minDepth, maxDepth);

            // Mapping
            float factor = 0.2547f;
            int smallWidth = (int)(colorWidth * factor);
            int smallHeight = (int)(colorHeight * factor);

            this.mappedPixels = new Byte[smallWidth * smallHeight];
            this.mask = Enumerable.Repeat((Byte)1, smallWidth * smallHeight).ToArray();
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
            // Mapping both rgb & depth and then sending this mapping & the corresponding mask
            this.Mapping(factor, smallWidth, smallHeight);
            this.SendingMapping();

        }

        private void SendingColorData()
        {
            this.colorPublisher.SendByteArray(this.colorPixelsSending);
        }


        private void DepthTreatment(ushort minDepth, ushort maxDepth)
        {
            for (int i = 0; i < this.depthPixelData.Length; i++)
            {
                ushort depth = this.depthPixelData[i];
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }


        private void Mapping(float factor, int smallWidth, int smallHeight)
        {
            this.coordinateMapper.MapDepthFrameToColorSpace(this.depthPixelData, this.colorPoints);
            for (int index = 0; index < this.colorPoints.Length; index++)
            {
                int colorX = (int)(this.colorPoints[index].X);
                int colorY = (int)(this.colorPoints[index].Y);
                colorX = (int)(colorX * factor);
                colorY = (int)(colorY * factor);
                if (!float.IsNegativeInfinity(colorX) && !float.IsNegativeInfinity(colorY))
                {
                    if ((colorX >= 0) && (colorX < smallWidth) && (colorY >= 0) && (colorY < smallHeight))
                    {
                        int colorIndex = (colorY * smallWidth) + colorX;
                        this.mappedPixels[colorIndex] = this.depthPixels[index];
                        this.mask[colorIndex] = 0;
                    }
                }
            }
            /*for (int i = 0; i < this.mask.Length; ++i)
            {
                if (this.mask[i] != 255)
                    this.mask[i] = 1;
            }

                ColorSpacePoint[] noNegativeInfinityArray = new ColorSpacePoint[this.colorPoints.Length];
                int newindex = 0;
                for (int index = 0; index < this.colorPoints.Length; index++)
                {
                    if (!float.IsNegativeInfinity(this.colorPoints[index].X) && !float.IsNegativeInfinity(this.colorPoints[index].Y))
                    {
                        noNegativeInfinityArray[newindex] = this.colorPoints[index];
                        ++newindex;
                    }
                }
                Array.Resize(ref noNegativeInfinityArray, newindex+1);

                byte[] byteArray = new byte[this.colorPoints.Length * sizeof(ColorSpacePoint)];

                fixed (ColorSpacePoint* src = this.colorPoints)
                fixed (byte* dest = byteArray)
                {
                    ColorSpacePoint* typedDest = (ColorSpacePoint*)dest;
                    for (int i = 0; i < this.colorPoints.Length; ++i)
                    {
                        typedDest[i] = src[i];
                    }
                }*/
        }


        private void SendingMapping()
        {
            this.mappingPublisher.SendByteArray(this.mappedPixels);
            this.maskPublisher.SendByteArray(this.mask);
        }

        public ImageSource FrameTreatment(ColorFrame colorFrame, DepthFrame depthFrame, String mode)
        {
            this.FindAName(colorFrame, depthFrame);

            // Return ImageSource depending on mode
            //return null;
            PixelFormat formatBgr32 = PixelFormats.Bgr32;
            PixelFormat formatGray8 = PixelFormats.Gray8;
            double factor = 0.2547;
            int stride;
            if (mode.Equals("Color"))
            {
                stride = 1920 * formatBgr32.BitsPerPixel / 8;
                return BitmapSource.Create(1920, 1080, 200, 200, formatBgr32, null, this.colorPixels, stride);
            }
            else if (mode.Equals("Depth"))
            {
                stride = 512 * formatGray8.BitsPerPixel / 8;
                return BitmapSource.Create(512, 424, 96.0, 96.0, formatGray8, null, this.depthPixels, stride);
            }
            else if(mode.Equals("Mapped"))
            {
                stride = (int)(1920 * factor * formatGray8.BitsPerPixel / 8);
                return BitmapSource.Create((int)(1920 * factor), (int)(1080 * factor), 96.0, 96.0, formatGray8, null, this.mappedPixels, stride);
            }
            else
                return null;
        }
     }
}
