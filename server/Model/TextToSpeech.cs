using Microsoft.Speech.Recognition;
using System;
using System.Collections;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;
using Newtonsoft.Json.Linq;

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
        //private String spokenText;
        private Queue queue = new Queue();
        private String last_text;
        private Boolean processing_blocking_speech = false;

        public TextToSpeech(SpeechRecognition sr)
        {
            this.sr = sr;
            this.synthesizer = new SpeechSynthesizer();
            this.responder = new NetworkResponder(false);
            this.responder.Bind("33407");
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.synthesizer.SpeakCompleted += this.TTSCompleted;

            this.speakThread = new Thread(new ThreadStart(this.Speak));
            this.speakThread.SetApartmentState(ApartmentState.STA);
            this.speakThread.IsBackground = true;
            this.speakThread.Start();
        }

        public void Say(Int32 id, String speech, Boolean blocking)
        {
            this.synthesizer.SpeakAsyncCancelAll();
            this.synthesizer.SpeakAsync(speech);
            if (!blocking)
            {
                this.responder.Reply("non-blocking");
            }
        }

        public void Speak()
        {
            while (this.speakThread.IsAlive)
            {
                String json_message = this.responder.Receive();
                json_message = this.responder.Receive();
                JObject message = JObject.Parse(json_message);
                Boolean blocking = (Boolean)message["blocking"];
                Int32 id = (Int32)message["id"];
                String speech = (String)message["sentence"];

                if (!this.processing_blocking_speech)
                {
                    lock (this.queue)
                    {
                        if (this.queue.Count == 0)
                        {
                            this.sr.pause();
                        }
                        this.queue.Enqueue(json_message);
                        this.last_text = speech;
                    }
                    this.Say(id, speech, blocking);
                }
            }
        }

        private void TTSCompleted(object sender, SpeakCompletedEventArgs e)
        {
            Boolean blocking;

            lock (this.queue)
            {
                if (this.queue.Count == 1)
                {
                    this.sr.unpause();
                }

            String json_message = (String)this.queue.Dequeue();
            JObject message = JObject.Parse(json_message);
            blocking = (Boolean)message["blocking"];
            Int32 id = (Int32)message["id"];
            String speech = (String)message["sentence"];
            }

            if (blocking)
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
                return this.last_text;
            }
        }

    }
}
