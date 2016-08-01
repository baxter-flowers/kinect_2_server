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
        private NetworkPublisher speechPublisher;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognitionEngine speechEngine;
        private Grammar grammar;
        private String grammarText;
        private String fileLocation;
        private String fileName;
        private String currentLanguage;
        private DateTime lastTTS;
        private float confidenceThreshold = 0.3f;
        private Boolean semanticsStatus;
        private Boolean sentenceStatus;

        public SpeechRecognition(KinectSensor kinect, KinectAudioStream convertStream)
        {
            this.kinectSensor = kinect;
            this.speechPublisher = new NetworkPublisher();
            this.speechPublisher.Bind("33405");
            this.convertStream = convertStream;
        }

        /// <summary>
        /// Check if the new file is different from the current one.
        /// </summary>
        /// <param name="newFile">Full path of the new file</param>
        /// <returns></returns>
 
        public Boolean IsFileNew(String newFile)
        {
            if (this.fileLocation == null)
                return true;
            else
                return !this.fileLocation.Equals(newFile);
        }

        /// <summary>
        /// Check if the speech engine is already set.
        /// </summary>
        /// <returns></returns>
        public Boolean IsSpeechEngineSet()
        {
            return this.speechEngine != null;
        }

        public DateTime LastTTS
        {
            get
            {
                return this.lastTTS;
            }
            set
            {
                this.lastTTS = value;
            }
        }

        /// <summary>
        /// Unload all grammar files from the current speech engine.
        /// </summary>
        public void UnloadGrammars()
        {
            if (this.speechEngine != null)
                this.speechEngine.UnloadAllGrammars();
        }

        /// <summary>
        /// Load the actual grammar file.
        /// </summary>
        public void LoadGrammar()
        {
            if (this.grammar != null && this.speechEngine != null)
                this.speechEngine.LoadGrammar(this.grammar);
        }

        /// <summary>
        /// Check if there is alread a grammar file loaded.
        /// </summary>
        /// <returns></returns>
        public Boolean AnyGrammarLoaded()
        {
            if (this.speechEngine != null)
                return this.speechEngine.Grammars.Count != 0;
            else
                return false;
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
                this.speechPublisher.Bind(lPort);
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
                return this.speechPublisher;
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

        /// <summary>
        /// Generate a new MemoryStream from a string.
        /// </summary>
        /// <param name="s">String used to generate the stream</param>
        /// <returns>A new MemoryStream</returns>
        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Create a new grammar file from a grammar file selected on the server or send by the client.
        /// </summary>
        /// <param name="fileLocation">Full path of the file</param>
        /// <param name="fileName">File name</param>
        /// <param name="raw_grammar">Grammar send by the client</param>
        /// <returns>A string that contains information about raised exceptions</returns>
        public String CreateGrammarF(String fileLocation, String fileName, String raw_grammar = null)
        {
            this.fileLocation = fileLocation;
            this.fileName = fileName;
            RecognizerInfo ri = TryGetKinectRecognizer(raw_grammar);

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                if (raw_grammar != null)
                {
                    using (var memoryStream = this.GenerateStreamFromString(raw_grammar))
                    {
                        try
                        {
                            this.grammar = new Grammar(memoryStream);
                        }
                        catch (FormatException e)
                        {
                            return  "Corrupted grammar file :" + e.Message;
                        }
                    }
                }
                else
                {
                    using (var memoryStream = File.OpenRead(fileLocation))
                    {
                        try
                        {
                            this.grammar = new Grammar(memoryStream);
                        }
                        catch (FormatException e)
                        {
                            return "Corrupted grammar file :" + e.Message;
                        }
                    }
                }
                this.speechEngine.LoadGrammar(this.grammar);

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;
                this.speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                return "";
            }
            else
            {
                return "The language of the file is not installed or unknown";
            }
        }

        public String CreateGrammar(String fileLocation, String fileName, String raw_grammar = null)
        {
            this.fileLocation = fileLocation;
            this.fileName = fileName;

            this.speechEngine = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            this.speechEngine.SetInputToDefaultAudioDevice();

            // Create a grammar from grammar definition XML file.
            if (raw_grammar != null)
            {
                using (var memoryStream = this.GenerateStreamFromString(raw_grammar))
                {
                    try
                    {
                        this.grammar = new Grammar(memoryStream);
                    }
                    catch (FormatException e)
                    {
                        return "Corrupted grammar file :" + e.Message;
                    }
                }
            }
            else
            {
                using (var memoryStream = File.OpenRead(fileLocation))
                {
                    try
                    {
                        this.grammar = new Grammar(memoryStream);
                    }
                    catch (FormatException e)
                    {
                        return "Corrupted grammar file :" + e.Message;
                    }
                }
            }
            this.speechEngine.LoadGrammar(this.grammar);

            // let the convertStream know speech is going active
            //this.convertStream.SpeechActive = true;
            //this.speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

            //this.speechEngine.SetInputToAudioStream(
            //    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            return "";
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
                SetGrammarText(fileLocation);
            else
                this.grammarText = raw_grammar;
            SetCurrentLanguage(grammarText);
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

        /// <summary>
        /// Detect the language of the grammar file.
        /// </summary>
        /// <param name="grammarText">Grammar file as string</param>
        /// <returns>A string that contains information about raised exceptions</returns>
        public String SetCurrentLanguage(string grammarText)
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
            catch (XmlException e)
            {
                String message = "Failed to read Xml File :" + e.Message;
                return message;
            }

            return "";
        }

        /// <summary>
        /// Transform a grammar file into a string.
        /// </summary>
        /// <param name="fileLocation">Full path of the grammar file</param>
        public void SetGrammarText(string fileLocation)
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

        /// <summary>
        /// Getting and sending both full sentence and semantics from a recognized speech.
        /// </summary>
        /// <param name="semantics">Semantics from the recognized speech</param>
        /// <param name="sentence">Sentence from the recognized speech</param>
        /// <returns>A list that contains semantics</returns>
        public List<String> SpeechRecognized(SemanticValue semantics, string sentence)
        {
            List<String> contentSentence = new List<string>();
            List<String> contentSemantic = new List<string>();
            Dictionary<String, List<String>> dico = new Dictionary<string, List<String>>();
            //Fill the dictionary
            contentSentence.Add(sentence);
            dico.Add("sentence", contentSentence);
            foreach (KeyValuePair<String, SemanticValue> child in semantics)
            {
                contentSemantic.Add(semantics[child.Key].Value.ToString());
            }
            dico.Add("semantics", contentSemantic);
            if (dico.Count != 0)
            {
                string json = JsonConvert.SerializeObject(dico);
                this.speechPublisher.SendString(json, "recognized_speech");
                return contentSemantic;
            }
            else
            {
                return null;
            }
        }
    }
}
