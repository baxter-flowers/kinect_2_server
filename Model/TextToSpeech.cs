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

        public TextToSpeech(NetworkSubscriber sub)
        {
            this.synthesizer = new SpeechSynthesizer();
            this.subscriber = sub;
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.speakThread = new Thread(new ThreadStart(Speak));
            this.speakThread.Start();
        }


        public void Speak()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                this.spokenText = this.subscriber.ReceiveText();
                this.synthesizer.SpeakAsync(this.spokenText);
            }
        }

        public void addTTSListener(EventHandler<SpeakProgressEventArgs> f)
        {
            this.synthesizer.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(f);
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
                this.synthesizer.SelectVoiceByHints(this.voiceGender);
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
                this.synthesizer.SelectVoiceByHints(this.VoiceGender, VoiceAge.Adult, 0, this.culture);
            }
        }

        public String SpokenText
        {
            get
            {
                return this.spokenText;
            }
        }
    }
}
