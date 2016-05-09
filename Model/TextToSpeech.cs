using System;
using System.Globalization;
using System.Speech.Synthesis;

namespace Kinect2Server
{
    public class TextToSpeech
    {
        private SpeechSynthesizer synthesizer;
        private NetworkSubscriber subscriber;
        private VoiceGender voiceGender;
        private CultureInfo culture;

        public TextToSpeech(NetworkSubscriber sub)
        {
            this.synthesizer = new SpeechSynthesizer();
            this.subscriber = sub;
            // Configure the audio output to default settings
            this.synthesizer.SetOutputToDefaultAudioDevice();

            this.voiceGender = VoiceGender.Male;
            this.culture = new CultureInfo("en-US");
        }


        public void Speak()
        {
            string text = null;
            while (true)
            {
                text = this.subscriber.ReceiveText();
                if (text != null)
                {
                    this.synthesizer.SpeakAsync(text);
                }
                text = null;
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
    }
}
