using Kinect2Server.View;
using Microsoft.Speech.Recognition;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkResponder
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;
        private Boolean instant;

        public NetworkResponder(bool instant = true)
        {
            this.context = new ZContext();
            this.socket = new ZSocket(this.context, instant ? ZSocketType.REP : ZSocketType.ROUTER);
            this.binded = false;
            this.instant = instant;
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

        public String Receive()
        {
                String request = null;
                if (this.binded)
                {
                    try 
                    {
                        ZFrame frame = this.socket.ReceiveFrame();
                        request = frame.ReadString();
                        
                    }
                    catch (ZException e)
                    {
                        request = "Cannot receive message: " + e.Message;
                    }
                }
                else
                {
                    request = "Cannot receive message: Not connected";
                }
            return request;
        }

        public void Reply(String reply)
        {
            if (!this.instant) { 
                ZFrame envelope = new ZFrame("client");  // Identity is hardcoded, must match the client's identity
                this.socket.SendFrame(envelope, ZSocketFlags.More);  
            }
            ZFrame message = new ZFrame(reply);
            this.socket.Send(message);
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
