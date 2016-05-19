using Microsoft.Speech.Recognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private SpeechRecognition sr;
        private NetworkResponder responder;
        private MainWindow mw;

        public SpeechRecognitionView()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.sr = this.mw.SpeechRecogniton;
            this.responder = this.mw.NetworkResponder;
            InitializeComponent();
        }

        // Turn off or on the speech recognition 
        private void switchSR(object sender, RoutedEventArgs e)
        {
            switchSpeechRecognition(sender, e);
        }

        public void clearRecognitionText()
        {
            this.lastSemantics.Text = "";
            this.lastSentence.Text = "";
        }

        private void switchSpeechRecognition(object sender, RoutedEventArgs e)
        {
            clearRecognitionText();

            if (!sr.isSpeechEngineSet())
            {
                setButtonOn(this.stackSR);
                loadGrammarFile(sender, e);
            }
            else if (sr.anyGrammarLoaded())
            {
                setButtonOff(this.stackSR);
                this.status.Text = Properties.Resources.ZzZz;
                sr.unloadGrammars();
                this.sr.SpeechRecognitionEngine.RecognizeAsyncStop();
            }
            else
            {
                setButtonOn(this.stackSR);
                this.status.Text = Properties.Resources.GoOn;
                sr.loadGrammar();
                this.sr.SpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        public void setButtonOff(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
                stack.Children.Add(img);
            });
            
        }

        public void setButtonOn(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
                stack.Children.Add(img);
            });
            
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            if (semantics.Confidence >= sr.ConfidenceThreshold)
            {
                List<String> contentSentence = new List<string>();
                List<String> contentSemantic = new List<string>();
                Dictionary<String, List<String>> dico = new Dictionary<string, List<String>>();
                string sentence = e.Result.Text;

                //Only sentence is active
                if(sr.SentenceStatus && !sr.SemanticsStatus)
                {
                    //Fill the dictionary
                    contentSentence.Add(sentence);
                    dico.Add("sentence", contentSentence);
                    //Update the text
                    this.lastSentence.Text = sentence;
                    //Send the dictionary
                    string json = JsonConvert.SerializeObject(dico);
                    sr.NetworkPublisher.SendString(json, "recognized_speech");
                }
                //Only semantic is active
                else if (!sr.SentenceStatus && sr.SemanticsStatus)
                {
                    //Fill the dictionary
                    foreach (KeyValuePair<String, SemanticValue> child in semantics)
                    {
                        contentSemantic.Add(semantics[child.Key].Value.ToString());
                    }
                    dico.Add("semantic", contentSemantic);
                    //Update the text
                    this.lastSemantics.Text = "";
                    for (int i = 0; i < contentSemantic.Count; i++)
                    {
                        this.lastSemantics.Text += contentSemantic[i];
                    }
                    //Send the dictionary
                    string json = JsonConvert.SerializeObject(dico);
                    sr.NetworkPublisher.SendString(json, "recognized_speech");
                }
                //Both sentence and semantic are active
                if (sr.SentenceStatus && sr.SemanticsStatus)
                {
                    //Fill the dictionary
                    contentSentence.Add(sentence);
                    dico.Add("sentence", contentSentence);
                    foreach (KeyValuePair<String, SemanticValue> child in semantics)
                    {
                        contentSemantic.Add(semantics[child.Key].Value.ToString());
                    }
                    dico.Add("semantic", contentSemantic);
                    //Update the text
                    this.lastSentence.Text = sentence;
                    this.lastSemantics.Text = "";
                    for (int i = 1; i < contentSemantic.Count; i++)
                    {
                        this.lastSemantics.Text += contentSemantic[i];

                    }
                    //Send the dictionary
                    string json = JsonConvert.SerializeObject(dico);
                    sr.NetworkPublisher.SendString(json, "recognized_speech");
                }
            }
        }

        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (sr.SentenceStatus)
            {
                this.lastSentence.Text = Properties.Resources.NoWordsRecognized;
            }
            this.lastSemantics.Text = "";
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

            if (!sr.isGrammarLoaded() || sr.CurrentLanguage.Equals(""))
            {
                setButtonOff(this.stackSR);
                this.status.Text = Properties.Resources.BadFile;
            }
            else
            {
                this.status.Text = Properties.Resources.GoOn;
            }

            switch (sr.CurrentLanguage)
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
            if (this.sr.isSpeechEngineSet())
            {
                this.addlist();
            }
        }

        public void addlist()
        {
            this.sr.addSRListener(this.SpeechRecognized, this.SpeechRejected);
        }

        private void submitConfidence(object sender, RoutedEventArgs e)
        {
            if (this.confidenceSelector.Value == null)
            {
                this.confidenceSelector.Value = (int)sr.ConfidenceThreshold*100;
            }
            else
            {
                sr.ConfidenceThreshold = ((Double)this.confidenceSelector.Value);
                this.clearRecognitionText();
            }
        }

        private void submitListeningPort(object sender, RoutedEventArgs e)
        {
            if (this.listeningPortSelector.Value == null)
                this.listeningPortSelector.Value = 33405;
            else
            {
                sr.ListeningPort = ((int)this.listeningPortSelector.Value);
            }
        }

        private void switchSem(object sender, RoutedEventArgs e)
        {
            this.lastSemantics.Text = "";
            if (sr.SemanticsStatus)
            {
                this.lastSemantics.Visibility = Visibility.Hidden;
                sr.SemanticsStatus = false;
                setButtonOff(this.stackSem);
            }
            else
            {
                this.lastSemantics.Visibility = Visibility.Visible;
                sr.SemanticsStatus = true;
                setButtonOn(this.stackSem);
            }
        }

        private void switchSen(object sender, RoutedEventArgs e)
        {
            this.lastSentence.Text = "";
            if (sr.SentenceStatus)
            {
                this.lastSentence.Visibility = Visibility.Hidden;
                sr.SentenceStatus = false;
                setButtonOff(this.stackSen);
            }
            else
            {
                this.lastSentence.Visibility = Visibility.Visible;
                sr.SentenceStatus = true;
                setButtonOn(this.stackSen);
            }
        }
    }
}
