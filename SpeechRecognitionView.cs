using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Kinect2Server
{
    class SpeechRecognitionView 
    {
        /*MainWindow mw = new MainWindow();
        SpeechRecognition sr = new SpeechRecognition();

        public void clearRecognitionText()
        {
            mw.lastSemantics.Text = "";
            mw.lastSentence.Text  = "";
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
            if (result == true && sr.fileLocation != dlg.FileName)
            {
                sr.fileLocation = dlg.FileName;
                sr.fileName = dlg.SafeFileName;

                mw.currentFile.Text = sr.fileName;
                clearRecognitionText();

                // Create a new grammar for the file loaded
                sr.createGrammar(sr.fileLocation);

                sr.setGrammarText(sr.fileLocation);
                sr.setCurrentLanguage(sr.grammarText);

                
            }
            else if (result == false)
            {
                setButtonOff();
            }

            if (sr.grammar == null)
            {
                setButtonOff();
            }

            switch (sr.currentLanguage)
            {
                case "en-US":
                    mw.language.Text = Properties.Resources.AmericanEnglish;
                    break;

                case "fr-FR":
                    mw.language.Text = Properties.Resources.French;
                    break;
            }
        }

        
        // Create, close or open the KinectSensor regarding to its status 
        private void switchSR(object sender, RoutedEventArgs e)
        {
            switchSpeechRecognition(sender, e);
        }

        private void submitConfidence(object sender, RoutedEventArgs e)
        {
            if (mw.upDown.Value == null)
            {
                mw.upDown.Value = 30;
            }
            sr.confidenceThreshold = (double)mw.upDown.Value / 100;
        }

        private void switchSemantics(object sender, RoutedEventArgs e)
        {
            Image img = new Image();
            mw.semanticsStack.Children.Clear();
            mw.lastSemantics.Text = "";
            if (sr.semanticsStatus)
            {
                mw.lastSemantics.Visibility = Visibility.Hidden;
                sr.semanticsStatus = false;
                img.Source = new BitmapImage(new Uri(@"Images/switch_off.png", UriKind.Relative));
            }
            else
            {
                mw.lastSemantics.Visibility = Visibility.Visible;
                sr.semanticsStatus = true;
                img.Source = new BitmapImage(new Uri(@"Images/switch_on.png", UriKind.Relative));
            }
            mw.semanticsStack.Children.Add(img);
        }

        private void switchSentence(object sender, RoutedEventArgs e)
        {
            Image img = new Image();
            mw.sentenceStack.Children.Clear();
            mw.lastSentence.Text = "";
            if (sr.sentenceStatus)
            {
                mw.lastSentence.Visibility = Visibility.Hidden;
                sr.sentenceStatus = false;
                img.Source = new BitmapImage(new Uri(@"Images/switch_off.png", UriKind.Relative));
            }
            else
            {
                mw.lastSentence.Visibility = Visibility.Visible;
                sr.sentenceStatus = true;
                img.Source = new BitmapImage(new Uri(@"Images/switch_on.png", UriKind.Relative));
            }
            mw.sentenceStack.Children.Add(img);
        }

        private void switchSpeechRecognition(object sender, RoutedEventArgs e)
        {
            clearRecognitionText();
            if (sr.speechEngine == null)
            {
                setButtonOn();
                updateXMLGrammarFile(sender, e);
                //Enable the Browse button
                mw.browse.IsEnabled = true;
            }
            else if (sr.speechEngine.Grammars.Count != 0)
            {
                setButtonOff();
                sr.speechEngine.UnloadAllGrammars();
            }
            else
            {
                setButtonOn();
                sr.speechEngine.LoadGrammar(sr.grammar);
            }
        }

        private void setButtonOff()
        {
            Image img = new Image();
            mw.stackP.Children.Clear();
            img.Source = new BitmapImage(new Uri(@"Images/switch_off.png", UriKind.Relative));
            mw.stackP.Children.Add(img);
        }

        private void setButtonOn()
        {
            Image img = new Image();
            mw.stackP.Children.Clear();
            img.Source = new BitmapImage(new Uri(@"Images/switch_on.png", UriKind.Relative));
            mw.stackP.Children.Add(img);
        }*/
    }
}
