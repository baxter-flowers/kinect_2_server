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
            this.socket = new ZSocket(this.context, ZSocketType.PUB);
            this.binded = false;
        }

        public void Bind(String listeningPort)
        {
            String status = null;
            try
            {
                this.socket.Bind("tcp://*:" + listeningPort);
                this.binded = true;
            }
            catch (ZException e)
            {
                status = ("Socket connection failed, server cannot listen on port " + listeningPort + ": " + e.Message);
            }
        }

        public void SendString(String message, String topic)
        {
            String status = null;
            if (this.binded)
            {
                ZFrame frame = new ZFrame(string.Format(topic + " {0}",message));
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

        public void SetConflate()
        {
            this.socket.SetOption(ZSocketOption.CONFLATE, 1);
        }

        public void SendByteArray(Byte[] byteArray)
        {
            String status = null;
            if (this.binded)
            {
                ZFrame frame = new ZFrame(byteArray);
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
