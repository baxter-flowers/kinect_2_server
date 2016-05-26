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
        private NetworkPublisher network;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognitionEngine speechEngine;
        private Grammar grammar;
        private String grammarText;
        private String fileLocation;
        private String fileName;
        private String currentLanguage;
        private float confidenceThreshold = 0.3f;
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

        public float ConfidenceThreshold
        {
            get
            {
                return this.confidenceThreshold;
            }
            set
            {
                this.confidenceThreshold = value;
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

        public String FileName
        {
            get
            {
                return this.fileName;
            }
            set
            {
                this.fileName = value;
            }
        }

        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        
        public void createGrammar(String fileLocation, String fileName, String raw_grammar = null)
        {
            this.fileLocation = fileLocation;
            this.fileName = fileName;
            RecognizerInfo ri = TryGetKinectRecognizer(raw_grammar);

            if (speechEngine != null)
            {
                speechEngine.UnloadAllGrammars();
            }

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                if (raw_grammar != null)
                {
                    using (var memoryStream = this.GenerateStreamFromString(raw_grammar))
                    {
                        this.grammar = new Grammar(memoryStream);
                    }
                }
                else
                {
                    using (var memoryStream = File.OpenRead(fileLocation))
                    {
                        this.grammar = new Grammar(memoryStream);
                    }
                }
                this.speechEngine.LoadGrammar(this.grammar);

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;
                this.speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        private RecognizerInfo TryGetKinectRecognizer(String raw_grammar = null)
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

            if (raw_grammar == null)
                setGrammarText(fileLocation);
            else
                this.grammarText = raw_grammar;
            setCurrentLanguage(grammarText);
            if (this.currentLanguage != null)
            {
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
            return null;
        }

        public void addSRListener(EventHandler<SpeechRecognizedEventArgs> f1, EventHandler<SpeechRecognitionRejectedEventArgs> f2)
        {
            this.speechEngine.SpeechRecognized += f1; 
            this.speechEngine.SpeechRecognitionRejected += f2;
        }

        public SpeechRecognitionEngine SpeechRecognitionEngine
        {
            get
            {
                return this.speechEngine;
            }
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
            try
            {
                reader.Read();
                currentLanguage = reader.XmlLang;
                reader.Close();
            }
            catch { }
            
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

        public List<String> SpeechRecognized(SemanticValue semantics, string sentence)
        {
            if (semantics.Confidence >= this.confidenceThreshold)
            {
                List<String> contentSentence = new List<string>();
                List<String> contentSemantic = new List<string>();
                Dictionary<String, List<String>> dico = new Dictionary<string, List<String>>();

                if (this.sentenceStatus)
                {
                    //Fill the dictionary
                    contentSentence.Add(sentence);
                    dico.Add("sentence", contentSentence);

                    if (this.semanticsStatus)
                    {
                        //Fill the dictionary
                        foreach (KeyValuePair<String, SemanticValue> child in semantics)
                        {
                            contentSemantic.Add(semantics[child.Key].Value.ToString());
                        }
                        dico.Add("semantics", contentSemantic);
                    }
                }
                else if (this.semanticsStatus)
                {
                    //Fill the dictionary
                    foreach (KeyValuePair<String, SemanticValue> child in semantics)
                    {
                        contentSemantic.Add(semantics[child.Key].Value.ToString());
                    }
                    dico.Add("semantics", contentSemantic);

                    if (this.sentenceStatus)
                    {
                        //Fill the dictionary
                        contentSentence.Add(sentence);
                        dico.Add("sentence", contentSentence);
                    }
                }

                if (dico.Count != 0)
                {
                    string json = JsonConvert.SerializeObject(dico);
                    this.network.SendString(json, "recognized_speech");
                }
                return contentSemantic;
            }
            return null;
        }
    }
}
