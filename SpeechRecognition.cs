using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace Kinect2Server
{
    public class SpeechRecognition
    {
        private NetworkPublisher network;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognitionEngine speechEngine;
        private Grammar grammar;
        private String grammarText;
        private String fileLocation;
        private String fileName;
        private String currentLanguage;
        private Double confidenceThreshold = 0.30;
        private Boolean semanticsStatus;
        private Boolean sentenceStatus;



        public SpeechRecognition(KinectSensor kinect, NetworkPublisher network, KinectAudioStream convertStream)
        {
            this.kinectSensor = kinect;
            this.network = network;
            this.convertStream = convertStream;
        }

        public Boolean isGrammarLoaded()
        {
            return this.grammar != null;
        }

        public Boolean isFileNew(String newFile)
        {
            if (this.fileLocation == null)
                return true;
            else
                return !this.fileLocation.Equals(newFile);
        }

        public Boolean isSpeechEngineSet()
        {
            return this.speechEngine != null;
        }

        public void unloadGrammars()
        {
            this.speechEngine.UnloadAllGrammars();
        }

        public void loadGrammar()
        {
            this.speechEngine.LoadGrammar(this.grammar);
        }

        public Boolean anyGrammarLoaded()
        {
            if (!this.isGrammarLoaded())
                return false;
            else
                return this.speechEngine.Grammars.Count != 0;
        }

        public String CurrentLanguage
        {
            get
            {
                return this.currentLanguage;
            }
        }

        public Double ConfidenceThreshold
        {
            get
            {
                return this.confidenceThreshold;
            }
            set
            {
                this.confidenceThreshold = value / 100;
            }
        }

        public int ListeningPort
        {
            set
            {
                string lPort = value.ToString();
                this.network.Bind(lPort);
            }
        }

        public Boolean SemanticsStatus
        {
            get
            {
                return this.semanticsStatus;
            }
            set
            {
                this.semanticsStatus = value;
            }
        }

        public Boolean SentenceStatus
        {
            get
            {
                return this.sentenceStatus;
            }
            set
            {
                this.sentenceStatus = value;
            }
        }

        public NetworkPublisher NetworkPublisher
        {
            get
            {
                return this.network;
            }
        }

        public void disableSpeechEngine()
        {
            this.speechEngine.RecognizeAsyncStop();
        }

        private void setKinectSensor()
        {
            // Only one sensor is supported
            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor != null)
            {
                // open the sensor
                this.kinectSensor.Open();

                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = this.kinectSensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                // create the convert stream
                this.convertStream = new KinectAudioStream(audioStream);

            }
        }

        public void createGrammar(String fileLocation, String fileName)
        {
            this.fileLocation = fileLocation;
            this.fileName = fileName;
            RecognizerInfo ri = TryGetKinectRecognizer();

            if (speechEngine != null)
            {
                speechEngine.UnloadAllGrammars();
            }

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = File.OpenRead(fileLocation))
                {
                    this.grammar = new Grammar(memoryStream);
                    this.speechEngine.LoadGrammar(this.grammar);
                }


                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;
                this.speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        private RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            setGrammarText(fileLocation);
            setCurrentLanguage(grammarText);

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && this.currentLanguage.Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            return null;
        }

        public void addSRListener(EventHandler<SpeechRecognizedEventArgs> f1, EventHandler<SpeechRecognitionRejectedEventArgs> f2)
        {
            this.speechEngine.SpeechRecognized += f1;
            this.speechEngine.SpeechRecognitionRejected += f2;
        }

        public void removeSRListener(EventHandler<SpeechRecognizedEventArgs> f1, EventHandler<SpeechRecognitionRejectedEventArgs> f2)
        {
            this.speechEngine.SpeechRecognized -= f1;
            this.speechEngine.SpeechRecognitionRejected -= f2;
        }

        public void setCurrentLanguage(string grammarText)
        {
            //Create the XmlNamespaceManager.
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);

            //Create the XmlParserContext.
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            //Create the reader.
            XmlTextReader reader = new XmlTextReader(grammarText, XmlNodeType.Element, context);
            reader.WhitespaceHandling = WhitespaceHandling.None;

            //Get the language
            reader.Read();
            currentLanguage = reader.XmlLang;
            reader.Close();
        }

        public void setGrammarText(string fileLocation)
        {
            //Load the xml file 
            XmlDocument grammar = new XmlDocument();
            try
            {
                grammar.Load(fileLocation);
            }
            catch (System.Xml.XmlException)
            {
            }

            //Convert the xml file into string
            StringWriter sw = new StringWriter();
            XmlTextWriter tw = new XmlTextWriter(sw);
            grammar.WriteTo(tw);
            this.grammarText = sw.ToString();
        }

    }
}
