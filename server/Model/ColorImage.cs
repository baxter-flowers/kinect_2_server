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
    public class ColorImage
    {
        private NetworkPublisher publisher;
        private KinectSensor kinect;
        private ColorFrameReader colorFrameReader;
        private Byte[] bytes;

        public ColorImage(KinectSensor kinect, NetworkPublisher pub)
        {
            this.kinect = kinect;
            this.publisher = pub;
            this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
        }

        public void addCIListener(EventHandler<ColorFrameArrivedEventArgs> f)
        {
            this.colorFrameReader.FrameArrived += f;
        }

        public void removeCIListener(EventHandler<ColorFrameArrivedEventArgs> f)
        {
            this.colorFrameReader.FrameArrived -= f;
        }

        public void colorFrameToByteArray(int size, ColorFrame colorFrame)
        {
            bytes = new Byte[size/2];
            colorFrame.CopyRawFrameDataToArray(bytes);
            this.publisher.SendByteArray(bytes);
            bytes = null;
        }
    }
}
