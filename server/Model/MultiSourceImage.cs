using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kinect2Server
{
    public class MultiSourceImage
    {
        private NetworkPublisher depthPublisher;
        private NetworkPublisher colorPublisher;
        private NetworkPublisher mappingPublisher;
        private KinectSensor kinect;
        private CoordinateMapper coordinateMapper;
        private MultiSourceFrameReader multiSourceFrameReader;
        private const int MapDepthToByte = 8000 / 256;
        private int frameCount;
        private Byte[] colorPixels;
        private Byte[] depthPixels;
        private ushort[] depthPixelData;
        private ColorSpacePoint[] colorPoints;
        private Byte[] mappedPixels;

        public MultiSourceImage(KinectSensor kinect, NetworkPublisher dPub, NetworkPublisher cPub, NetworkPublisher mPub)
        {
            this.kinect = kinect;
            this.coordinateMapper = this.kinect.CoordinateMapper;
            this.depthPublisher = dPub;
            this.colorPublisher = cPub;
            this.mappingPublisher = mPub;
            
            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
            
        }

        public MultiSourceFrameReader MultiSourceFrameReader
        {
            get
            {
                return this.multiSourceFrameReader;
            }
        }

        public int FrameCount
        {
            get
            {
                return this.frameCount;
            }
            set
            {
                this.frameCount = value;
            }
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
            this.colorPixels = new Byte[colorWidth * colorHeight * ((format.BitsPerPixel + 7) / 8)];

            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                colorFrame.CopyRawFrameDataToArray(this.colorPixels);
            }
            else
            {
                colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
            }

            this.colorPublisher.SendByteArray(this.colorPixels);
        }

        private unsafe void DepthTreatment(DepthFrame depthFrame)
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
            
            
            //this.depthPublisher.SendByteArray(this.depthPixels);
        }

        public void MapDepthToColor(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Mapping color & depth
            int colorWidth = colorFrame.FrameDescription.Width;
            int colorHeight = colorFrame.FrameDescription.Height;
            int depthWidth = depthFrame.FrameDescription.Width;
            int depthHeight = depthFrame.FrameDescription.Height;

            this.mappedPixels = new Byte[colorWidth * colorHeight];
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];

            this.coordinateMapper.MapDepthFrameToColorSpace(this.depthPixelData, this.colorPoints);
            
            for (int index = 0; index < this.colorPoints.Length; index++)
            {
                if (!float.IsNegativeInfinity(this.colorPoints[index].X) && !float.IsNegativeInfinity(this.colorPoints[index].Y))
                {
                    int colorX = (int)this.colorPoints[index].X;
                    int colorY = (int)this.colorPoints[index].Y;
                    if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                    {
                        int colorIndex = (colorY * colorWidth) + colorX;
                        this.mappedPixels[colorIndex] = this.depthPixels[index];
                    }
                }
            }
            
            this.mappingPublisher.SendByteArray(this.mappedPixels);
        }

        public ImageSource FrameTreatment(ColorFrame colorFrame, DepthFrame depthFrame, String mode)
        {
            PixelFormat formatBgr32 = PixelFormats.Bgr32;
            PixelFormat formatGray8 = PixelFormats.Gray8;
            int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

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
            else if (mode.Equals("Mapped"))
            {
                stride = colorFrame.FrameDescription.Width * formatGray8.BitsPerPixel / 8;
                return BitmapSource.Create(colorFrame.FrameDescription.Width,colorFrame.FrameDescription.Height, 16, 16, formatGray8,null,this.mappedPixels,stride);
            }
            else
            {
                return null;
            }
        }
     }
}
