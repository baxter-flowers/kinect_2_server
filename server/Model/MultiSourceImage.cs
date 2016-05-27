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

        public void SendFrames(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            depthFrame.CopyFrameDataToArray(this.shorts);
            Buffer.BlockCopy(shorts, 0, this.depthBytes, 0, this.depthBytes.Length);
            colorFrame.CopyRawFrameDataToArray(this.colorBytes);

            this.depthPublisher.SendByteArray(this.depthBytes);
            this.colorPublisher.SendByteArray(this.colorBytes);
        }


        public ImageSource ToBitMap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public ImageSource ToBitMap(DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] pixelData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(pixelData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixels[colorIndex++] = intensity; // B
                pixels[colorIndex++] = intensity; // G
                pixels[colorIndex++] = intensity; // R

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public ImageSource MapDepthToColor(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            PixelFormat format = PixelFormats.Bgr32;
            int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
            ushort[] depthData = new ushort[depthFrame.FrameDescription.Width * depthFrame.FrameDescription.Height];
            Byte[] colorData = new Byte[colorFrame.FrameDescription.Width * colorFrame.FrameDescription.Height * bytesPerPixel];
            Byte[] displayPixels = new Byte[colorFrame.FrameDescription.Width * colorFrame.FrameDescription.Height * bytesPerPixel];
            DepthSpacePoint[] depthPoints = new DepthSpacePoint[colorFrame.FrameDescription.Width * colorFrame.FrameDescription.Height];

            depthFrame.CopyFrameDataToArray(depthData);
            colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);
            this.coordinateMapper.MapColorFrameToDepthSpace(depthData, depthPoints);
            Array.Clear(displayPixels, 0, displayPixels.Length);

            for (int colorIndex = 0; colorIndex < depthPoints.Length; colorIndex++)
            {
                DepthSpacePoint depthPoint = depthPoints[colorIndex];

                if (!float.IsNegativeInfinity(depthPoint.X) && !float.IsNegativeInfinity(depthPoint.Y))
                {
                    int depthX = (int)Math.Floor(depthPoint.X + 0.5f);
                    int depthY = (int)Math.Floor(depthPoint.Y + 0.5f);

                    if ((depthX >= 0) && (depthX < depthFrame.FrameDescription.Width) && (depthY >= 0) && (depthY < depthFrame.FrameDescription.Height))
                    {
                        int depthIndex = (depthY * depthFrame.FrameDescription.Width) + depthX;

                        int sourceIndex = colorIndex * bytesPerPixel;

                        displayPixels[sourceIndex] = colorData[sourceIndex++]; // B
                        displayPixels[sourceIndex] = colorData[sourceIndex++]; // G
                        displayPixels[sourceIndex] = colorData[sourceIndex++]; // R
                        displayPixels[sourceIndex] = 255;                      // A
                    }
                }
            }
            int stride = colorFrame.FrameDescription.Width * format.BitsPerPixel / 8;

            return BitmapSource.Create(colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height, 96, 96, format, null, displayPixels, stride);
        }
     }
}
