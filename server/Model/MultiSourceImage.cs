using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect2Server
{
    public class MultiSourceImage
    {
        private NetworkPublisher depthPublisher;
        private NetworkPublisher colorPublisher;
        private KinectSensor kinect;
        private MultiSourceFrameReader multiSourceFrameReader;
        private ushort[] shorts;
        private Byte[] depthBytes;
        private Byte[] colorBytes;

        public MultiSourceImage(KinectSensor kinect, NetworkPublisher dPub, NetworkPublisher cPub)
        {
            this.kinect = kinect;
            this.depthPublisher = dPub;
            this.colorPublisher = cPub;

            this.multiSourceFrameReader = this.kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            this.shorts = new ushort[217088];
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

        public static Byte[] GetBytesUShort(ushort shortToConvert)
        {
            return BitConverter.GetBytes(shortToConvert);
        }

        public void SendDepthFrame(int size, DepthFrame depthFrame)
        {
            depthFrame.CopyFrameDataToArray(shorts);
            Buffer.BlockCopy(shorts, 0, this.depthBytes, 0, this.depthBytes.Length);
            this.depthPublisher.SendByteArray(this.depthBytes);
        }

        public void SendColorFrame(int size, ColorFrame colorFrame)
        {
            colorFrame.CopyRawFrameDataToArray(this.colorBytes);
            this.colorPublisher.SendByteArray(this.colorBytes);
        }
    }
}
