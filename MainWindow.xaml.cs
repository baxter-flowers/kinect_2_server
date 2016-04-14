using System;
using System.Xml;
using System.Collections.Generic;    
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Kinect;    
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2Server
{
     public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            this.InitializeComponent();
        }

/*---------------------------------------------------------------------------------------
* 
*                                WINDOW 
* 
*----------------------------------------------------------------------------------------*/

        

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            
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


/*---------------------------------------------------------------------------------------
* 
*                                KINECT'S STATUS
* 
*----------------------------------------------------------------------------------------*/

        private KinectSensor kinectSensor = null;
        private KinectAudioStream convertStream = null;


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
            }
        }

        // Create, close or open the KinectSensor regarding to its status 
        private void switchSR(object sender, RoutedEventArgs e)
        {
            Image img = new Image();
            this.stackP.Children.Clear();
            if (kinectSensor == null)
            {
                img.Source = new BitmapImage(new Uri(@"Images/switch_on.png", UriKind.Relative));
                this.stackP.Children.Add(img);
                setKinectSensor(sender, e);

                //Enable the Browse button
                this.browse.IsEnabled = true;
            }
            else if (!kinectSensor.IsOpen)
            {
                img.Source = new BitmapImage(new Uri(@"Images/switch_on.png", UriKind.Relative));
                this.stackP.Children.Add(img);
                kinectSensor.Open();
            }
            else
            {
                img.Source = new BitmapImage(new Uri(@"Images/switch_off.png", UriKind.Relative));
                this.stackP.Children.Add(img);
                this.lastWord.Text = "";
                kinectSensor.Close();
            }
        }
        
        
/*---------------------------------------------------------------------------------------
 * 
 *                               SPEECH RECOGNITION
 * 
 *---------------------------------------------------------------------------------------*/
        private Grammar grammar = null;
        private SpeechRecognitionEngine speechEngine = null;
        private static string grammarText;
        private static string fileName;
        private static string location;
        private static string currentLanguage;
        private double confidenceThreshold = 0.3;


        private static void setGrammarText(string location)
        {
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

        private void createGrammar(object sender, RoutedEventArgs e)
        {
            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                this.statusBarText.Text = "";
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = File.OpenRead(location))
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
            }
            else
            {
                this.statusBarText.Text = Properties.Resources.NoSpeechRecognizer;
            }
        }


        // Update the XML file when the user opens a file
        private void updateXMLGrammarFile(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (.xml)|*.xml";

            // Display OpenFileDialog
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file path and set it in location if it is different from the actual file
            if (result == true && location != dlg.FileName)
            {
                location = dlg.FileName;
                fileName = dlg.SafeFileName;

                setGrammarText(location);
                setCurrentLanguage(grammarText);

                // Create a new grammar for the file loaded
                createGrammar(sender, e);
            }

            switch (currentLanguage)
            {
                case "en-US":
                    this.language.Text = Properties.Resources.AmericanEnglish;
                    break;

                case "fr-FR":
                    this.language.Text = Properties.Resources.French;
                    break;
            }
        }

        private void submitConfidence(object sender, RoutedEventArgs e)
        {
            this.confidenceThreshold = (double)this.upDown.Value/100;
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {

            if (e.Result.Confidence >= confidenceThreshold)
            {
                this.lastWord.Text = e.Result.Text;
                /*SemanticValue semantics = e.Result.Semantics;
                foreach(KeyValuePair<String,SemanticValue> child in semantics) {
                    this.lastWord.Text += child.Value.ToString();
                }*/

            }


        }

        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.lastWord.Text = Properties.Resources.NoWordsRecognized;
        }


    }
}