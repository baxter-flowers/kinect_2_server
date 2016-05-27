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
        private int frameCount;
        private PixelFormat format;
        private int colorWidth;
        private int colorHeight;
        private int depthWidth;
        private int depthHeight;
        private Byte[] colorPixels;
        private Byte[] depthPixels;
        private ushort[] depthPixelData;
        private DepthSpacePoint[] depthPoints;
        private Byte[] mappingPixels;
        private Boolean first;

        public MultiSourceImage(KinectSensor kinect, NetworkPublisher dPub, NetworkPublisher cPub, NetworkPublisher mPub)
        {
            this.kinect = kinect;
            this.coordinateMapper = this.kinect.CoordinateMapper;
            this.depthPublisher = dPub;
            this.colorPublisher = cPub;
            this.mappingPublisher = mPub;
            
            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            this.format = PixelFormats.Bgr32;
            FrameDescription depthDescription = this.kinect.DepthFrameSource.FrameDescription;
            FrameDescription colorDescription = this.kinect.ColorFrameSource.FrameDescription;
            this.colorWidth = colorDescription.Width;
            this.colorHeight = colorDescription.Height;
            this.depthWidth = depthDescription.Width;
            this.depthHeight = depthDescription.Height;
            this.first = true;

            //Arrays initialization

            this.colorPixels = new Byte[this.colorWidth * this.colorHeight * ((this.format.BitsPerPixel + 7) / 8)];
            this.depthPixelData = new ushort[this.depthWidth * this.depthHeight];
            this.depthPixels = new byte[this.depthWidth * this.depthHeight * ((this.format.BitsPerPixel + 7) / 8)];
            this.mappingPixels = new Byte[this.colorWidth * this.colorHeight * ((this.format.BitsPerPixel + 7) / 8)];
            this.depthPoints = new DepthSpacePoint[this.colorWidth * this.colorHeight];

            
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

        public ImageSource FrameTreatment(ColorFrame colorFrame, DepthFrame depthFrame, String mode)
        {
            // Color treatment

            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                colorFrame.CopyRawFrameDataToArray(this.colorPixels);
            }
            else
            {
                colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
            }
            // Color sending
            this.colorPublisher.SendByteArray(this.colorPixels);


            // Depth treatment
            ushort minDepth = depthFrame.DepthMinReliableDistance;
            ushort maxDepth = depthFrame.DepthMaxReliableDistance;

            depthFrame.CopyFrameDataToArray(this.depthPixelData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < this.depthPixelData.Length; ++depthIndex)
            {
                ushort depth = this.depthPixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                this.depthPixels[colorIndex++] = intensity; // B
                this.depthPixels[colorIndex++] = intensity; // G
                this.depthPixels[colorIndex++] = intensity; // R

                ++colorIndex;
            }
            // Depth sending
            this.depthPublisher.SendByteArray(this.depthPixels);


            // Mapping color & depth
            if (this.first)
            {
                this.coordinateMapper.MapColorFrameToDepthSpace(this.depthPixelData, this.depthPoints);
                this.first = false;
            }
            
            for (colorIndex = 0; colorIndex < this.depthPoints.Length; colorIndex++)
            {
                DepthSpacePoint depthPoint = this.depthPoints[colorIndex];

                if (!float.IsNegativeInfinity(depthPoint.X) && !float.IsNegativeInfinity(depthPoint.Y))
                {
                    int depthX = (int)Math.Floor(depthPoint.X + 0.5f);
                    int depthY = (int)Math.Floor(depthPoint.Y + 0.5f);

                    if ((depthX >= 0) && (depthX < this.depthWidth) && (depthY >= 0) && (depthY < this.depthHeight))
                    {
                        int depthIndex = (depthY * this.depthWidth) + depthX;

                        int sourceIndex = colorIndex * ((this.format.BitsPerPixel + 7) / 8);

                        this.mappingPixels[sourceIndex] = this.colorPixels[sourceIndex++]; // B
                        this.mappingPixels[sourceIndex] = this.colorPixels[sourceIndex++]; // G
                        this.mappingPixels[sourceIndex] = this.colorPixels[sourceIndex++]; // R
                        this.mappingPixels[sourceIndex] = 255;                      // A
                    }
                }
            }
            // Sending mapping
            this.mappingPublisher.SendByteArray(this.mappingPixels);

            // Return ImageSource depending on mode
            int stride;
            if (mode.Equals("Color"))
            {
                stride = this.colorWidth * this.format.BitsPerPixel / 8;
                return BitmapSource.Create(this.colorWidth, this.colorHeight, 96.0, 96.0, this.format, null, this.colorPixels, stride);
            }
            else if (mode.Equals("Depth"))
            {
                stride = this.depthWidth * this.format.BitsPerPixel / 8;
                return BitmapSource.Create(this.depthWidth, this.depthHeight, 96.0, 96.0, this.format, null, this.depthPixels, stride);
            }
            else if (mode.Equals("Mapped"))
            {
                stride = this.colorWidth * this.format.BitsPerPixel / 8;
                return BitmapSource.Create(this.colorWidth, this.colorHeight, 96.0, 96.0, this.format, null, this.mappingPixels, stride);
            }
            else
            {
                return null;
            }
        }
     }
}
