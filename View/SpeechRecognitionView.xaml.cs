using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2Server.View
{
    /// <summary>
    /// Interaction logic for SpeechRecognitionView.xaml
    /// </summary>
    public partial class SpeechRecognitionView : UserControl
    {
        public SpeechRecognitionView()
        {
            InitializeComponent();
        }

        SpeechRecognition sr = new SpeechRecognition();

        // Turn off or on the speech recognition 
        private void switchSR(object sender, RoutedEventArgs e)
        {
            switchSpeechRecognition(sender, e);
        }

        public void clearRecognitionText()
        {
            this.lastSemantics.Text = "";
            this.lastSemantics.Text = "";
        }

        private void switchSpeechRecognition(object sender, RoutedEventArgs e)
        {
            clearRecognitionText();

            if (sr.speechEngine == null)
            {
                setButtonOn(this.stackSR);
                loadGrammarFile(sender, e);
                //Enable the Browse button
                this.browse.IsEnabled = true;
            }
            else if (sr.speechEngine.Grammars.Count != 0)
            {
                setButtonOff(this.stackSR);
                sr.speechEngine.UnloadAllGrammars();
            }
            else
            {
                setButtonOn(this.stackSR);
                sr.speechEngine.LoadGrammar(sr.grammar);
            }
        }

        //
        private void setButtonOff(StackPanel stack)
        {
            Image img = new Image();
            stack.Children.Clear();
            img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
            stack.Children.Add(img);
        }

        private void setButtonOn(StackPanel stack)
        {
            Image img = new Image();
            stack.Children.Clear();
            img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
            stack.Children.Add(img);
        }


        // Update the XML file when the user opens a file
        private void loadGrammarFile(object sender, RoutedEventArgs e)
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
                this.status.Text = "";
                this.currentLanguage.Text = "";
                sr.fileLocation = dlg.FileName;
                sr.fileName = dlg.SafeFileName;

                this.currentFile.Text = sr.fileName;
                clearRecognitionText();

                // Create a new grammar for the file loaded
                sr.createGrammar(sr.fileLocation);

                setButtonOn(this.stackSR);

            }
            else if (result == false && sr.grammar == null)
            {
                setButtonOff(this.stackSR);
            }
            /*else if (){

            }*/

            if (sr.grammar == null || sr.currentLanguage.Equals(""))
            {
                setButtonOff(this.stackSR);
                this.status.Text = Properties.Resources.NoSpeechRecognizer;
            }

            switch (sr.currentLanguage)
            {
                case "en-US":
                    this.currentLanguage.Text = Properties.Resources.AmericanEnglish;
                    break;

                case "fr-FR":
                    this.currentLanguage.Text = Properties.Resources.French;
                    break;

                //Add as much languages as you want provided that the recognized is installed on the PC
                //You also have to add a Ressource in the file Ressources.resx for exemple :
                //        - Call your ressource German and fill it with "Current language : German"
            }
        }

        private void submitConfidence(object sender, RoutedEventArgs e)
        {
            if (this.confidenceSelector.Value == null)
            {
                this.confidenceSelector.Value = 30;
            }
            sr.confidenceThreshold = (double)this.confidenceSelector.Value / 100;
        }

        private void switchSem(object sender, RoutedEventArgs e)
        {
            this.lastSemantics.Text = "";
            if (sr.semanticsStatus)
            {
                this.lastSemantics.Visibility = Visibility.Hidden;
                sr.semanticsStatus = false;
                setButtonOff(this.stackSem);
            }
            else
            {
                this.lastSemantics.Visibility = Visibility.Visible;
                sr.semanticsStatus = true;
                setButtonOn(this.stackSem);
            }
        }

        private void switchSen(object sender, RoutedEventArgs e)
        {
            this.lastSentence.Text = "";
            if (sr.sentenceStatus)
            {
                this.lastSentence.Visibility = Visibility.Hidden;
                sr.sentenceStatus = false;
                setButtonOff(this.stackSen);
            }
            else
            {
                this.lastSentence.Visibility = Visibility.Visible;
                sr.sentenceStatus = true;
                setButtonOn(this.stackSen);
            }
        }
    }
}
