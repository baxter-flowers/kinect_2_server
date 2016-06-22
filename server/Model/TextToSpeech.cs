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
        private NetworkSubscriber ttsSubscriber;
        private SpeechRecognition sr;
        private VoiceGender voiceGender;
        private CultureInfo culture;
        private Thread speakThread;
        private String spokenText;
        private Boolean queuedMessages;
        private Queue textQueue;

        public TextToSpeech(SpeechRecognition sr)
        {
            this.sr = sr;
            this.synthesizer = new SpeechSynthesizer();
            this.ttsSubscriber = new NetworkSubscriber();
            this.ttsSubscriber.Bind("33407");
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.synthesizer.SpeakCompleted += this.UnpauseSR;

            this.speakThread = new Thread(new ThreadStart(this.Speak));
            this.speakThread.IsBackground = true;
            this.speakThread.Start();
            this.queuedMessages = false;
            this.textQueue = new Queue();
        }


        public void Speak()
        {
            while (this.speakThread.IsAlive)
            {
                this.textQueue.Enqueue(this.ttsSubscriber.ReceiveText());
                if(this.sr.isGrammarLoaded())
                    this.sr.unloadGrammars();
                lock (this)
                {
                    this.spokenText = (String)this.textQueue.Dequeue();
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

        private void UnpauseSR(object sender, SpeakCompletedEventArgs e)
        {
            if (this.sr.isGrammarLoaded())
                this.sr.loadGrammar();
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
