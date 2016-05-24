using Microsoft.Kinect;
using System;
using System.Collections.Generic;
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
        private MultiSourceFrameReader multiSourceFrameReader;
        private ushort[] shorts;
        private ColorSpacePoint[] colorSpacePoints;
        private Byte[] depthBytes;
        private Byte[] colorBytes;
        

        public MultiSourceImage(KinectSensor kinect, NetworkPublisher dPub, NetworkPublisher cPub)
        {
            this.kinect = kinect;
            this.coordinateMapper = this.kinect.CoordinateMapper;
            this.depthPublisher = dPub;
            this.colorPublisher = cPub;


            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            this.shorts = new ushort[217088];
            this.colorSpacePoints = new ColorSpacePoint[217088];
            this.depthBytes = new Byte[434176];
            this.colorBytes = new Byte[4147200];
        }

        public MultiSourceFrameReader MultiSourceFrameReader
        {
            get
            {
                return this.multiSourceFrameReader;
            }
        }

        public void addMSIListener(EventHandler<MultiSourceFrameArrivedEventArgs> f)
        {
            this.multiSourceFrameReader.MultiSourceFrameArrived += f;
        }


        public void SendDepthFrame(DepthFrame depthFrame)
        {
            depthFrame.CopyFrameDataToArray(this.shorts);
            Buffer.BlockCopy(shorts, 0, this.depthBytes, 0, this.depthBytes.Length);
            this.depthPublisher.SendByteArray(this.depthBytes);
        }

        public void SendColorFrame(ColorFrame colorFrame)
        {
            colorFrame.CopyRawFrameDataToArray(this.colorBytes);
            this.colorPublisher.SendByteArray(this.colorBytes);
            //this.ConvertBytesToJpegBytes(this.colorBytes);
        }

        public void ConvertBytesToJpegBytes(Byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                img.Save(ms, ImageFormat.Jpeg);
                int length = ms.ToArray().Length;
            }
        }

    }
}
