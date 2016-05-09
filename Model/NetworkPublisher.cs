using System;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkPublisher
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;

        public NetworkPublisher()
        {
            this.context = new ZContext();
            this.socket = new ZSocket(context, ZSocketType.PUB);
            this.binded = false;
        }

        public void Bind(String listening_port)
        {
            String status = null;
            try
            {
                this.socket.Bind("tcp://*:" + listening_port);
                this.binded = true;
            }
            catch (ZException e)
            {
                status = ("Socket connection failed, server cannot listen on p ort " + listening_port + ": " + e.Message);
            }
        }

        public void SendJSON(String json_string, String topic)
        {
            String status = null;
            if (this.binded)
            {
                ZFrame frame = new ZFrame(string.Format(topic + " {0}",json_string));
                try
                {
                    this.socket.Send(frame);
                }
                catch (ZException e)
                {
                    status = "Cannot publish message: " + e.Message;
                }

            }
            else
            {
                status = ("Cannot publish message: Not binded");
            }
        }

        public void Close()
        {
            this.socket.Close();
            this.binded = false;
        }

        ~NetworkPublisher()
        {
            this.context.Dispose();
        }
    }
}
