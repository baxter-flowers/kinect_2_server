using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kinect2Server
{
    public class ColorImages
    {
        private NetworkPublisher publisher;
        private KinectSensor kinect;
        private ColorFrameReader colorFrameReader;
        private Byte[] bytes;

        public ColorImages(KinectSensor kinect, NetworkPublisher pub)
        {
            this.kinect = kinect;
            this.publisher = pub;
            this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if(colorFrame!=null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        int size = (colorFrameDescription.Width * colorFrameDescription.Height * 2);
                        bytes = new Byte[size];
                        colorFrame.CopyRawFrameDataToArray(bytes);
                        this.publisher.SendByteArray(bytes);
                        bytes = null;
                    }
                }
            }
        }
    }
}
