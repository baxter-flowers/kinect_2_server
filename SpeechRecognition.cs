using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Kinect2Server
{
    class SpeechRecognition
    {
        private KinectSensor kinectSensor = null;
        private KinectAudioStream convertStream = null;
        private RecognizerInfo ri = null;
        private Grammar grammar = null;
        private SpeechRecognitionEngine speechEngine = null;
        private static string grammarText = null;
        private static string fileName = null;
        private static string location = "C:/Users/Yoan/Documents/Internship Lucas/SpeechBasics-WPF-Lucas/testGrammar.xml";
        private static string currentLanguage = null;

        private SpeechRecognition(KinectSensor kinectSensor)
        {
            this.kinectSensor = kinectSensor;
            this.ri = TryGetKinectRecognizer();
        }

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
    }
}
