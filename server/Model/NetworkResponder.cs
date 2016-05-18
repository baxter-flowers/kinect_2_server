using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkResponder
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;
        private Thread json_thread;

        public NetworkResponder()
        {
            this.context = new ZContext();
            this.socket = new ZSocket(this.context, ZSocketType.REP);
            this.binded = false;
            this.json_thread = new Thread(new ThreadStart(this.ReceiveJson));
            this.json_thread.IsBackground = true;
            this.json_thread.Start();
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

        public void ReceiveJson()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                String status = null;
                if (this.binded)
                {
                    try
                    {
                        ZFrame frame = this.socket.ReceiveFrame();
                        status = frame.ReadString();
                        ZFrame reply = new ZFrame("Json string received");
                        this.socket.Send(reply);
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
            }
        }

        public void Close()
        {
            this.socket.Close();
            this.binded = false;
        }

        ~NetworkResponder()
        {
            this.context.Dispose();
        }
    }
}
