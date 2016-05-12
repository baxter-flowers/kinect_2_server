using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect2Server
{
    public class AudioFrame
    {
        private NetworkPublisher publisher;
        private KinectSensor kinect;
        private AudioBeamFrameReader audioBeamFrameReader;
        private readonly Byte[] audioBuffer;
        private Byte[] fullAudio;

        public AudioFrame(KinectSensor kinect, NetworkPublisher pub)
        {
            this.kinect = kinect;
            this.publisher = pub;

            AudioSource audioSource = this.kinect.AudioSource;
            this.audioBeamFrameReader = audioSource.OpenReader();

            // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
            // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
            // With 4 bytes per sample, that gives us 1024 bytes.
            this.audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

            this.audioBeamFrameReader.FrameArrived += this.Reader_AudioBeamFrameArrived;
        }

        private void Reader_AudioBeamFrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameReference frameReference = e.FrameReference;
            AudioBeamFrameList frameList = frameReference.AcquireBeamFrames();

            if (frameList != null)
            {
                using (frameList)
                {
                    // Only one audio beam is supported. Get the sub frame list for this beam
                    IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                    this.fullAudio = new Byte[audioBuffer.Length*subFrameList.Count];
                    int start = 0;
                    foreach (AudioBeamSubFrame subFrame in subFrameList)
                    {
                        subFrame.CopyFrameDataToArray(this.audioBuffer);
                        this.audioBuffer.CopyTo(this.fullAudio,start);
                        start += this.audioBuffer.Length;
                    }

                    this.publisher.SendByteArray(this.fullAudio);
                    this.fullAudio = null;
                }
            }
        }
    }
}
