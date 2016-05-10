using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;

namespace Kinect2Server
{
    public class TextToSpeech
    {
        private SpeechSynthesizer synthesizer;
        private NetworkSubscriber subscriber;
        private VoiceGender voiceGender;
        private CultureInfo culture;
        private Thread speakThread;
        private String spokenText;
        private Boolean queuedMessages;

        public TextToSpeech(NetworkSubscriber sub)
        {
            this.synthesizer = new SpeechSynthesizer();
            this.subscriber = sub;
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.speakThread = new Thread(new ThreadStart(this.Speak));
            this.speakThread.Start();
            this.queuedMessages = false;
        }


        public void Speak()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                this.spokenText = this.subscriber.ReceiveText();
                lock (this)
                {
                    if (!this.queuedMessages)
                        this.synthesizer.SpeakAsyncCancelAll();
                    this.synthesizer.SpeakAsync(this.spokenText);
                }
            }
        }

        public void addTTSListener(EventHandler<SpeakProgressEventArgs> f)
        {
            lock (this)
            {
                this.synthesizer.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(f);
            }
        }

       
        public VoiceGender VoiceGender
        {
            get
            {
                return this.voiceGender;
            }
            set
            {
                this.voiceGender = value;
                lock (this)
                {
                    this.synthesizer.SelectVoiceByHints(this.voiceGender);
                }
            }
        }

        public CultureInfo Culture
        {
            get
            {
                return this.culture;
            }
            set
            {
                this.culture = value;
                lock (this)
                {
                    this.synthesizer.SelectVoiceByHints(this.VoiceGender, VoiceAge.Adult, 0, this.culture);
                }
            }
        }

        public String SpokenText
        {
            get
            {
                return this.spokenText;
            }
        }

        public Thread SpeakThread
        {
            get
            {
                return this.speakThread;
            }
            set
            {
                this.speakThread = value;
            }
        }

        public Boolean QueuedMessages
        {
            get
            {
                return this.queuedMessages;
            }
            set
            {
                this.queuedMessages = value;
            }
        }
    }
}
