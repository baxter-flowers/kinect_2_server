using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Globalization;
using System.Threading;

namespace Kinect2Server
{
    public class TextToSpeech
    {
        private SpeechSynthesizer synthesizer;
        private CultureInfo culture;

        public TextToSpeech()
        {
            string defaultLanguage = Thread.CurrentThread.CurrentUICulture.ToString();
            synthesizer = new SpeechSynthesizer();
            PromptBuilder cultures = new PromptBuilder(new CultureInfo(defaultLanguage));
        }

        public void speak(string language)
        {

            this.synthesizer.SelectVoiceByHints(VoiceGender.Male,VoiceAge.Teen);
            this.synthesizer.Volume = 100;
            this.synthesizer.Rate = 10;
        }
    }
}
