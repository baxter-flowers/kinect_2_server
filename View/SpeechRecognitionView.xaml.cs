using Microsoft.Speech.Recognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            mw = (MainWindow)Application.Current.MainWindow;
            sr = mw.getSRInstance();
            InitializeComponent();
        }

        private SpeechRecognition sr;
        private MainWindow mw;



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

            if (!sr.isSpeechEngineSet())
            {
                setButtonOn(this.stackSR);
                loadGrammarFile(sender, e);
                mw.addSRList(this.SpeechRecognized);
            }
            else if (sr.anyGrammarLoaded())
            {
                setButtonOff(this.stackSR);
                sr.unloadGrammars();
            }
            else
            {
                setButtonOn(this.stackSR);
                sr.loadGrammar();
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

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            if (semantics.Confidence >= sr.getConfidenceThreshold())
            {
                if (sr.isSentenceOn())
                {
                    this.lastSentence.Text = e.Result.Text;
                }

                if (sr.isSemanticOn())
                {
                    this.lastSemantics.Text = "";
                    string[] semanticsString = new string[semantics.Count];
                    int i = 0;
                    foreach (KeyValuePair<String, SemanticValue> child in semantics)
                    {
                        semanticsString[i] = semantics[child.Key].Value.ToString();
                        i++;
                    }

                    string json = JsonConvert.SerializeObject(semanticsString);

                    for (i = 0; i < semantics.Count; i++)
                    {
                       this.lastSemantics.Text += semanticsString[i].ToString() + " ";
                    }
                }
            }
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
            if (result == true && sr.isFileNew(dlg.FileName))
            {
                this.status.Text = "";
                this.currentLanguage.Text = "";

                this.currentFile.Text = dlg.SafeFileName;
                clearRecognitionText();

                // Create a new grammar for the file loaded
                sr.createGrammar(dlg.FileName, dlg.SafeFileName);

                //Enable the Browse button
                this.browse.IsEnabled = true;

                setButtonOn(this.stackSR);

            }
            else if (result == false && !sr.isGrammarLoaded())
            {
                setButtonOff(this.stackSR);
            }

            if (!sr.isGrammarLoaded() || sr.getCurrentLanguage().Equals(""))
            {
                setButtonOff(this.stackSR);
                this.status.Text = Properties.Resources.NoSpeechRecognizer;
            }

            switch (sr.getCurrentLanguage())
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
            else
            {
                sr.updateConfidence((Double)this.confidenceSelector.Value);
            }
        }

        private void switchSem(object sender, RoutedEventArgs e)
        {
            this.lastSemantics.Text = "";
            if (sr.isSemanticOn())
            {
                this.lastSemantics.Visibility = Visibility.Hidden;
                sr.changeSemanticsStatus(false);
                setButtonOff(this.stackSem);
            }
            else
            {
                this.lastSemantics.Visibility = Visibility.Visible;
                sr.changeSemanticsStatus(true);
                setButtonOn(this.stackSem);
            }
        }

        private void switchSen(object sender, RoutedEventArgs e)
        {
            this.lastSentence.Text = "";
            if (sr.isSentenceOn())
            {
                this.lastSentence.Visibility = Visibility.Hidden;
                sr.changeSentenceStatus(false);
                setButtonOff(this.stackSen);
            }
            else
            {
                this.lastSentence.Visibility = Visibility.Visible;
                sr.changeSentenceStatus(true);
                setButtonOn(this.stackSen);
            }
        }
    }
}
