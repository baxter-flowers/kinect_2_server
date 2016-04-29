using System;    
using System.IO;

namespace Kinect2Server
{
    
    public class KinectAudioStream : Stream
    {
        private Stream kinect32BitStream;

        public KinectAudioStream(Stream input)
        {
            this.kinect32BitStream = input;
        }

        public bool SpeechActive { get; set; }


        public override bool CanRead
        {
            get { return true; }
        }


        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            // Speech does not call - but set value correctly
            get { return false; }
        }

        
        public override long Position
        {
            // Speech gets the position
            get { return 0; }
            set { throw new NotImplementedException(); }
        }

       
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

       
        public override void Flush()
        {
            throw new NotImplementedException();
        }

      
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Even though CanSeek == false, Speech still calls seek. Return 0 to make Speech happy instead of NotImplementedException()
            return 0;
        }

     
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

    
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

      
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Kinect gives 32-bit float samples. Speech asks for 16-bit integer samples.
            const int SampleSizeRatio = sizeof(float) / sizeof(short); // = 2. 

            // Speech reads at high frequency - allow some wait period between reads (in msec)
            const int SleepDuration = 50;

            // Allocate buffer for receiving 32-bit float from Kinect
            int readcount = count * SampleSizeRatio;
            byte[] kinectBuffer = new byte[readcount];

            int bytesremaining = readcount;

            // Speech expects all requested bytes to be returned
            while (bytesremaining > 0)
            {
                // If we are no longer processing speech commands, exit
                if (!this.SpeechActive)
                {
                    return 0;
                }

                int result = this.kinect32BitStream.Read(kinectBuffer, readcount - bytesremaining, bytesremaining);
                bytesremaining -= result;

                // Speech will read faster than realtime - wait for more data to arrive
                if (bytesremaining > 0)
                {
                    System.Threading.Thread.Sleep(SleepDuration);
                }
            }

            // Convert each float audio sample to short
            for (int i = 0; i < count / sizeof(short); i++)
            {
                // Extract a single 32-bit IEEE value from the byte array
                float sample = BitConverter.ToSingle(kinectBuffer, i * sizeof(float));

                // Make sure it is in the range [-1, +1]
                if (sample > 1.0f)
                {
                    sample = 1.0f;
                }
                else if (sample < -1.0f)
                {
                    sample = -1.0f;
                }

                // Scale float to the range (short.MinValue, short.MaxValue] and then 
                // convert to 16-bit signed with proper rounding
                short convertedSample = Convert.ToInt16(sample * short.MaxValue);

                // Place the resulting 16-bit sample in the output byte array
                byte[] local = BitConverter.GetBytes(convertedSample);
                System.Buffer.BlockCopy(local, 0, buffer, offset + (i * sizeof(short)), sizeof(short));
            }

            return count;
        }
    }
}
