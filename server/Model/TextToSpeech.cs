using Microsoft.Speech.Recognition;
using System;
using System.Collections;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;

namespace Kinect2Server
{
    public class TextToSpeech
    {
        private SpeechSynthesizer synthesizer;
        private NetworkResponder responder;
        private SpeechRecognition sr;
        private VoiceGender voiceGender;
        private CultureInfo culture;
        private Thread speakThread;
        private String spokenText;
        private Boolean blocking = true;
        private int numSpeakings;
        private Object numSpeakingsLock;

        public TextToSpeech(SpeechRecognition sr)
        {
            this.sr = sr;
            this.synthesizer = new SpeechSynthesizer();
            this.responder = new NetworkResponder(false);
            this.responder.Bind("33407");
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.synthesizer.SpeakCompleted += this.TTSCompleted;
            this.numSpeakings = 0;
            this.numSpeakingsLock = new Object();

            this.speakThread = new Thread(new ThreadStart(this.Speak));
            this.speakThread.SetApartmentState(ApartmentState.STA);
            this.speakThread.IsBackground = true;
            this.speakThread.Start();
        }

        public void Speak()
        {
            while (this.speakThread.IsAlive)
            {
                this.spokenText = this.responder.Receive();
                this.spokenText = this.responder.Receive();
                if (this.spokenText[0].Equals('f'))
                {
                    this.blocking = false;
                    this.responder.Reply("non-blocking");
                    this.synthesizer.SpeakAsyncCancelAll();
                }
                else
                {
                    this.blocking = true;
                }
                this.spokenText = this.spokenText.Substring(1);
                lock(this.numSpeakingsLock)
                {
                    if (this.numSpeakings == 0)
                    {
                        this.sr.pause();
                    }
                    this.numSpeakings += 1;
                }
                this.synthesizer.SpeakAsync(this.spokenText);
            }
        }

        private void TTSCompleted(object sender, SpeakCompletedEventArgs e)
        {
            lock (this.numSpeakingsLock)
            {
                if (this.numSpeakings == 1)
                {
                    this.sr.unpause();
                }
                this.numSpeakings -= 1;
            }
            if (this.blocking)
            {
                this.responder.Reply("blocking");
            }
        }

        /// <summary>
        /// Add a new EventHandler of type SpeakProgressEventArgs to the synthesizer.
        /// </summary>
        public void AddTTSListener(EventHandler<SpeakProgressEventArgs> f)
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
