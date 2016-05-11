using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.depthFrameReader.FrameArrived += this.Reader_DepthFrameArrived;
        }

        private void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    FrameDescription colorFrameDescription = depthFrame.FrameDescription;
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        int size = (colorFrameDescription.Width * colorFrameDescription.Height);
                        shorts = new ushort[size];

                        depthFrame.CopyFrameDataToArray(shorts);
                        bytes = new Byte[size*2];
                        int j = 0;
                        for (int i = 0; i < shorts.Length; i++)
                        {
                            bytes[j] = (Byte)shorts[i];
                            j += 2;
                        }
                        this.publisher.SendByteArray(bytes);
                        shorts = null;
                        bytes = null;
                    }
                }
            }
        }
    }
}
