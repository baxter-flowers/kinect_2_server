using Microsoft.Kinect;
using System;

namespace Kinect2Server
{
    public class DepthImage
    {
        private NetworkPublisher publisher;
        private KinectSensor kinect;
        private DepthFrameReader depthFrameReader;
        private ushort[] shorts;
        private Byte[] bytes;

        public DepthImage(KinectSensor kinect, NetworkPublisher pub)
        {
            this.kinect = kinect;
            this.publisher = pub;
            this.depthFrameReader = this.kinect.DepthFrameSource.OpenReader();
        }

        public DepthFrameReader DepthFrameReader
        {
            get
            {
                return this.depthFrameReader;
            }
        }

        public void addDIListener(EventHandler<DepthFrameArrivedEventArgs>f)
        {
            this.depthFrameReader.FrameArrived += f;
        }

        public static Byte[] GetBytesUShort(ushort shortToConvert)
        {
            return BitConverter.GetBytes(shortToConvert);
        }

        public void SendDepthFrame(int size, DepthFrame depthFrame)
        {
            shorts = new ushort[size];
            depthFrame.CopyFrameDataToArray(shorts);

            bytes = new Byte[size * 2];
            int index = 0;
            for (int i = 0; i < shorts.Length; i++)
            {
                Byte[] converted = GetBytesUShort(shorts[i]);
                converted.CopyTo(bytes, index);
                index += 2;
            }

            this.publisher.SendByteArray(bytes);
            shorts = null;
            bytes = null;
        }
    }
}
