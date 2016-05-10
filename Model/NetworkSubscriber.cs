using System;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkSubscriber
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;

        public NetworkSubscriber()
        {
            this.context = new ZContext();
            this.socket = new ZSocket(context, ZSocketType.SUB);
            this.binded = false;
        }

        public void Bind(String listeningPort)
        {
            try
            {
                this.socket.Bind("tcp://*:"+ listeningPort);
                this.binded = true;
            }
            catch (ZException e)
            {
            }
        }

        public String ReceiveText()
        {
            String status = null;
            if (this.binded)
            {
                try
                {
                    socket.Subscribe("tts");
                    ZFrame frame = this.socket.ReceiveFrame();
                    status = frame.ReadString();
                    status = status.Remove(0, 3);
                }
                catch (ZException e)
                {
                    status = "Cannot receive message: " + e.Message;
                }
            }
            else
            {
                status = "Cannot receive message: Not connected";
            }
            return status;
        }

        public void Close()
        {
            this.socket.Close();
            this.binded = false;
        }

        ~NetworkSubscriber()
        {
            this.context.Dispose();
        }
    }
}
