using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Kinect2Server
{
    class NetworkPublisher
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
            try
            {
                this.socket.Bind("tcp://*:" + listening_port);
                this.binded = true;
            }
            catch (ZException)
            {
                //Console.WriteLine("Socket connection failed, server cannot listen on port " + listening_port + ": " + e.Message);
            }
        }

        public void SendJSON(String json_string)
        {
            if (this.binded)
            {
                ZFrame frame = new ZFrame(json_string);
                try
                {
                    this.socket.Send(frame);
                }
                catch (ZException)
                {
                    //Console.WriteLine("Cannot publish message: " + e.Message);
                }

            }
            else
            {
                //Console.WriteLine("Cannot publish message: Not binded");
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
