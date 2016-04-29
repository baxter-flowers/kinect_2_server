using System;
using ZeroMQ;

namespace Kinect2Server.Model
{
    class NetworkSubscriber
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;

        public NetworkSubscriber()
        {
            this.context = new ZContext();
            this.socket = new ZSocket(context, ZSocketType.SUB);
        }
    }
}
