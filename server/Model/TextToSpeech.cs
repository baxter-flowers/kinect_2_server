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
        private Boolean queuedMessages;
        private Boolean replied = true;

        public TextToSpeech(SpeechRecognition sr)
        {
            this.sr = sr;
            this.synthesizer = new SpeechSynthesizer();
            this.responder = new NetworkResponder();
            this.responder.Bind("33407");
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.synthesizer.SpeakCompleted += this.UnpauseSR;


            this.speakThread = new Thread(new ThreadStart(this.Speak));
            this.speakThread.SetApartmentState(ApartmentState.STA);
            this.speakThread.IsBackground = true;
            this.speakThread.Start();
        }


        public void Speak()
        {
            while (this.speakThread.IsAlive)
            {
                if (this.replied)
                {
                    this.replied = false;
                    this.spokenText = this.responder.Receive();

                    if (this.sr.isGrammarLoaded())
                        this.sr.unloadGrammars();

                    if (!this.queuedMessages)
                        this.synthesizer.SpeakAsyncCancelAll();
                    this.synthesizer.SpeakAsync(this.spokenText);
                }
                
            }
        }

        private void UnpauseSR(object sender, SpeakCompletedEventArgs e)
        {
            if (this.sr.isGrammarLoaded())
                this.sr.loadGrammar();
            this.responder.Reply("");
            this.replied = true;
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
