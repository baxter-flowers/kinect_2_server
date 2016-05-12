using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkResponder
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;

        public NetworkResponder()
        {
            this.context = new ZContext();
            this.socket = new ZSocket(this.context, ZSocketType.REP);
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
