//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SpeechBasics
{
    using System;
    using System.Xml;
    using System.Collections.Generic;    
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;    
    using System.Windows;    
    using System.Windows.Documents;
    using System.Windows.Media;
    using Microsoft.Kinect;    
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Win32;
    using System.Collections;

 
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. For the sake of simplicity, we're omitting that code in this sample.")]
    public partial class MainWindow : Window
    {
        private const string MediumGreyBrushKey = "MediumGreyBrush";

        // Active Kinect sensor
        private KinectSensor kinectSensor = null;

        // Stream for 32b-16b conversion
        private KinectAudioStream convertStream = null;

        // Speech recognition engine using audio data from Kinect
        private SpeechRecognitionEngine speechEngine = null;

        // XML File converted to string
        private static string grammarText;

        // Name of the current file loaded
        private static string fileName = "SpeechGrammar-fr-FR";

        // Location of the current file loaded
        private static string location = "C:/Users/Yoan/Documents/Internship Lucas/SpeechBasics-WPF-Lucas/SpeechGrammar-fr-FR.xml";

        // Current language recognized
        private static string currentLanguage = "fr-FR";


        public MainWindow()
        {
            this.InitializeComponent();
        }

        // Update the XML file when the user opens a new file
        private void updateXMLGrammarFile(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (.xml)|*.xml";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file path and set it in location if it is different from the actual file
            if (result == true && location!=dlg.FileName)
            {
                location = dlg.FileName;
                fileName = dlg.SafeFileName;
                
            }
            setGrammarText(location);
            createGrammar(sender, e);
            setCurrentLanguage(grammarText);

            switch (currentLanguage)
                {
                    case "en-US" :
                        language.Text = "Current language : american English";
                        break;

                    case "fr-FR" :
                        language.Text = "Current language : French";
                        break;
                }
        }

       
        private static void setGrammarText(string location) {
            //Load the xml file 
            XmlDocument grammar = new XmlDocument();
            grammar.Load(location);

            //Convert the xml file into string
            StringWriter sw = new StringWriter();
            XmlTextWriter tw = new XmlTextWriter(sw);
            grammar.WriteTo(tw);
            grammarText = sw.ToString();
        }

        private static void setCurrentLanguage(string grammarText)
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

        private static RecognizerInfo TryGetKinectRecognizer()
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
            
            setGrammarText(location);
            setCurrentLanguage(grammarText);

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && currentLanguage.Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private void setKinectSensor(object sender, RoutedEventArgs e)
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
                // on failure, set the status text
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
                return;
            }
        }

        private void createGrammar(object sender, RoutedEventArgs e)
        {
            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                setGrammarText(location);
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(grammarText)))
                {
                    var g = new Grammar(memoryStream);
                    this.speechEngine.LoadGrammar(g);
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
            }
            else
            {
                this.statusBarText.Text = Properties.Resources.NoSpeechRecognizer;
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            setKinectSensor(sender, e);
            createGrammar(sender, e);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }

            if (null != this.kinectSensor)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "FORWARD":
                        this.lastWord.Text = "up";
                        break;

                    case "BACKWARD":
                        this.lastWord.Text = "down";
                        break;

                    case "LEFT":
                        this.lastWord.Text = "left";
                        break;

                    case "RIGHT":
                        this.lastWord.Text = "right";
                        break;

                }
            }


        }
     
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            lastWord.Text = "Last word recognized : ...";
        }
    }
}