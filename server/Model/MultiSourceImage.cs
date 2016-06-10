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
        private Byte[] depthPixels;
        private Byte[] mask;
        private ushort[] depthPixelData;
        private ColorSpacePoint[] colorPoints;
        private Byte[] mappedPixels;
        private Boolean reqRep;
        private Boolean repColorDelivered;
        private Boolean repMappingDelivered;

        public MultiSourceImage(KinectSensor kinect, NetworkPublisher cPub, NetworkPublisher mPub, NetworkPublisher maPub)
        {
            this.kinect = kinect;
            this.coordinateMapper = this.kinect.CoordinateMapper;
            this.colorPublisher = cPub;
            this.mappingPublisher = mPub;
            this.maskPublisher = maPub;
            
            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
            this.multiSourceFrameReader.IsPaused = true;
            this.reqRep = true;
            this.repColorDelivered = false;
            this.repMappingDelivered = false;
            
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

        public void ResetFrameBooleans()
        {
            this.repColorDelivered = false;
            this.repMappingDelivered = false;
        }

        public void addMSIListener(EventHandler<MultiSourceFrameArrivedEventArgs> f)
        {
            this.multiSourceFrameReader.MultiSourceFrameArrived += f;
        }

        private void ColorTreatment(ColorFrame colorFrame)
        {
            // Color treatment
            PixelFormat format = PixelFormats.Bgr32;
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;
            this.colorPixels = new Byte[colorWidth * colorHeight * 2];
            colorFrame.CopyRawFrameDataToArray(this.colorPixels);
            if (this.reqRep && !this.repColorDelivered)
            {
                this.colorPublisher.SendByteArray(this.colorPixels);
                this.repColorDelivered = true;
            }
            else if(!this.reqRep)
                this.colorPublisher.SendByteArray(this.colorPixels);
            
            this.colorPixels = new Byte[colorWidth * colorHeight * ((format.BitsPerPixel + 7) / 8)];
            colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
        }

        private void DepthTreatment(DepthFrame depthFrame)
        {
            // Depth treatment
            PixelFormat format = PixelFormats.Gray8;
            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            ushort minDepth = depthFrame.DepthMinReliableDistance;
            ushort maxDepth = depthFrame.DepthMaxReliableDistance;

            this.depthPixels = new byte[depthWidth * depthHeight];
            this.depthPixelData = new ushort[depthWidth * depthHeight];

            depthFrame.CopyFrameDataToArray(this.depthPixelData);

            for (int i = 0; i < this.depthPixelData.Length; i++)
            {
                ushort depth = this.depthPixelData[i];
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
            
        }

        public void MapDepthToColor(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Mapping color & depth
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;
            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            float factor = 0.2547f;
            int smallWidth = (int)(colorWidth * factor);
            int smallHeight = (int)(colorHeight * factor);

            this.mappedPixels = new Byte[smallWidth * smallHeight];
            //this.mask = new Byte[colorWidth * colorHeight];
            this.mask = Enumerable.Repeat((Byte)1, smallWidth * smallHeight).ToArray();
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];

            

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

                if (this.reqRep && !this.repMappingDelivered)
                {
                    this.mappingPublisher.SendByteArray(this.mappedPixels);
                    this.maskPublisher.SendByteArray(this.mask);
                    this.repMappingDelivered = true;
                }
                else if (!this.reqRep)
                {
                    this.mappingPublisher.SendByteArray(this.mappedPixels);
                    this.maskPublisher.SendByteArray(this.mask);
                }
                    
            
        }

        public ImageSource FrameTreatment(ColorFrame colorFrame, DepthFrame depthFrame, String mode)
        {
            PixelFormat formatBgr32 = PixelFormats.Bgr32;
            PixelFormat formatGray8 = PixelFormats.Gray8;
            int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
            double factor = 0.2547;

            this.ColorTreatment(colorFrame);

            this.DepthTreatment(depthFrame);

            this.MapDepthToColor(colorFrame, depthFrame);

            // Return ImageSource depending on mode
            int stride;
            if (mode.Equals("Color"))
            {
                stride = colorFrame.FrameDescription.Width * formatBgr32.BitsPerPixel / 8;
                return BitmapSource.Create(colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height, 200, 200, formatBgr32, null, this.colorPixels, stride);
            }
            else if (mode.Equals("Depth"))
            {
                stride = depthFrame.FrameDescription.Width * formatGray8.BitsPerPixel / 8;
                return BitmapSource.Create(depthFrame.FrameDescription.Width, depthFrame.FrameDescription.Height, 96.0, 96.0, formatGray8, null, this.depthPixels, stride);
            }
            else if(mode.Equals("Mapped"))
            {
                stride = (int)(colorFrame.FrameDescription.Width*factor * formatGray8.BitsPerPixel / 8);
                return BitmapSource.Create((int)(colorFrame.FrameDescription.Width * factor), (int)(colorFrame.FrameDescription.Height * factor), 96.0, 96.0, formatGray8, null, this.mappedPixels, stride);
            }
            else
                return null;
        }
     }
}
