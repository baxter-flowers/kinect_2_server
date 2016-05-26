using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
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
        private KinectSensor kinect;
        private CoordinateMapper coordinateMapper;
        private const int MapDepthToByte = 8000 / 256;
        private MultiSourceFrameReader multiSourceFrameReader;
        private ushort[] shorts;
        private Byte[] depthBytes;
        private Byte[] colorBytes;
        private ColorSpacePoint[] colorSpacePoints;
        private int fps;
        private int frameCount;
        

        public MultiSourceImage(KinectSensor kinect, NetworkPublisher dPub, NetworkPublisher cPub)
        {
            this.kinect = kinect;
            this.coordinateMapper = this.kinect.CoordinateMapper;
            this.depthPublisher = dPub;
            this.colorPublisher = cPub;
            this.fps = 20;
            


            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            this.shorts = new ushort[217088];
            this.depthBytes = new Byte[434176];
            this.colorBytes = new Byte[4147200];
            this.colorSpacePoints = new ColorSpacePoint[this.shorts.Length];
        }

        public MultiSourceFrameReader MultiSourceFrameReader
        {
            get
            {
                return this.multiSourceFrameReader;
            }
        }

        public int FPS
        {
            get
            {
                return this.fps;
            }
            set
            {
                this.fps = value;
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


        public void SendDepthFrame(DepthFrame depthFrame)
        {
            depthFrame.CopyFrameDataToArray(this.shorts);
            //this.coordinateMapper.MapDepthFrameToColorSpace(this.shorts, this.colorSpacePoints);
            Buffer.BlockCopy(shorts, 0, this.depthBytes, 0, this.depthBytes.Length);
            this.depthPublisher.SendByteArray(this.depthBytes);
        }

        public void SendColorFrame(ColorFrame colorFrame)
        {
            colorFrame.CopyRawFrameDataToArray(this.colorBytes);
            this.colorPublisher.SendByteArray(this.colorBytes);
        }

        public WriteableBitmap LetsFindABetterNameLater(ColorFrame colorFrame, WriteableBitmap colorBitmap)
        {
            FrameDescription colorFrameDescription = colorFrame.FrameDescription;

            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
            {
                if ((colorFrameDescription.Width == colorBitmap.Width) && (colorFrameDescription.Height == colorBitmap.Height))
                {
                    colorFrame.CopyConvertedFrameDataToIntPtr(
                        colorBitmap.BackBuffer,
                        (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                        ColorImageFormat.Bgra);

                    this.SendColorFrame(colorFrame);
                }
            }
            return colorBitmap;
        }

        public Byte[] AndAnotherOne(DepthFrame depthFrame, WriteableBitmap depthBitmap, Byte[] depthPixels)
        {
            using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
            {
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                if((217088 == (depthBuffer.Size / depthFrameDescription.BytesPerPixel)) &&
                    (depthFrameDescription.Width == depthBitmap.Width) && (depthFrameDescription.Height == depthBitmap.Height))
                {
                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                    // we are setting maxDepth to the extreme potential depth threshold
                    ushort maxDepth = ushort.MaxValue;
                    // If you wish to filter by reliable depth distance, uncomment the following line:
                    //// maxDepth = depthFrame.DepthMaxReliableDistance

                    this.SendDepthFrame(depthFrame);

                    depthPixels = this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth, depthFrameDescription, depthPixels);
                }
            }
            return depthPixels;
        }

        private unsafe Byte[] ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth, FrameDescription depthFrameDescription, Byte[] depthPixels)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                depthPixels[i] = (Byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }

            return depthPixels;
        }
    }
}
