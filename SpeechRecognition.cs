using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace Kinect2Server
{
    public class SpeechRecognition
    {
        public KinectSensor kinectSensor;
        public KinectAudioStream convertStream;
        public SpeechRecognitionEngine speechEngine;
        public Grammar grammar;
        public String grammarText;
        public String fileLocation;
        public String fileName;
        public String currentLanguage;
        public Double confidenceThreshold = 0.30;
        public Boolean semanticsStatus;
        public Boolean sentenceStatus;



        public SpeechRecognition()
        {
            setKinectSensor();
        }

        /*private SpeechRecognition(KinectSensor kinect, KinectAudioStream convStream, String language, Double confidence, Boolean semStatus, Boolean senStatus)
        {
            this.kinectSensor = kinect;
            this.convertStream = convStream;
            this.ri = TryGetKinectRecognizer();
            this.confidenceThreshold = confidence;
            this.semanticsStatus = semStatus;
            this.sentenceStatus = senStatus;
        }

        private SpeechRecognition(KinectSensor kinect, KinectAudioStream convStream, String language, String fileLocation, Double confidence, Boolean semStatus, Boolean senStatus) :
            this(kinect,convStream,language,confidence,semStatus,senStatus)
        {
            if (this.ri != null)
            {
                this.speechEngine = new SpeechRecognitionEngine(this.ri.Id);
                createGrammar(fileLocation);
            }
        }*/

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
            else
            {
                // on failure, set the status text and close the application
                //this.statusBarText.Text = Properties.Resources.NoKinectReady;
                System.Threading.Thread.Sleep(10000);
            }
        }

        public void createGrammar(String fileLocation)
        {

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (speechEngine != null)
            {
                speechEngine.UnloadAllGrammars();
            }

            if (null != ri)
            {
                //this.statusBarText.Text = "";

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = File.OpenRead(fileLocation))
                {
                    this.grammar = new Grammar(memoryStream);
                    this.speechEngine.LoadGrammar(this.grammar);
                }

                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                //return "";
            }
            else
            {
                //return Properties.Resources.NoSpeechRecognizer;
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


        public void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            if (semantics.Confidence >= confidenceThreshold)
            {
                if (sentenceStatus)
                {
                    string sentence = e.Result.Text;
                }

                if (semanticsStatus)
                {
                    //this.lastSemantics.Text = "";
                    int i = 0;
                    string[] semanticsString = new string[semantics.Count];
                    foreach (KeyValuePair<String, SemanticValue> child in semantics)
                    {
                        semanticsString[i] = semantics[child.Key].Value.ToString();
                        i++;
                    }

                    string json = JsonConvert.SerializeObject(semanticsString);

                    /*for (i = 0; i < semantics.Count; i++)
                    {
                       this.lastSemantics.Text += semanticsString[i].ToString() + " ";
                    }*/
                }
            }
        }

        public void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (sentenceStatus)
            {
                //this.lastSentence.Text = Properties.Resources.NoWordsRecognized;
            }
            //this.lastSemantics.Text = "";
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

        public void setGrammarText(string location)
        {
            //Load the xml file 
            XmlDocument grammar = new XmlDocument();
            try
            {
                grammar.Load(location);
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
