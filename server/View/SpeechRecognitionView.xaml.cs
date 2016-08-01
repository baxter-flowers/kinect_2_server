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
        private MainWindow mw;

        public SpeechRecognitionView()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.sr = this.mw.SpeechRecogniton;
            InitializeComponent();
        }

        /// <summary>
        /// Turns off or on the speech engine.
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">State information</param>
        private void SwitchSR(object sender, RoutedEventArgs e)
        {
            ClearRecognitionText();

            if (!sr.IsSpeechEngineSet() || !sr.AnyGrammarLoaded())
            {
                SetButtonOn(this.stackSR);
                LoadGrammarFile(sender, e);
            }
            else if (sr.AnyGrammarLoaded())
            {
                SetButtonOff(this.stackSR);
                this.status.Text = Properties.Resources.ZzZz;
                sr.UnloadGrammars();
                this.sr.SpeechRecognitionEngine.RecognizeAsyncStop();
            }
            else
            {
                SetButtonOn(this.stackSR);
                this.status.Text = Properties.Resources.GoOn;
                sr.LoadGrammar();
                this.sr.SpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        /// <summary>
        /// Clearing the sentence and semantics fields.
        /// </summary>
        public void ClearRecognitionText()
        {
            this.lastSemantics.Text = "";
            this.lastSentence.Text = "";
        }

        public void SetButtonOff(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
                stack.Children.Add(img);
            });
            
        }

        public void SetButtonOn(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
                stack.Children.Add(img);
                this.browse.IsEnabled = true;
            });
            
        }

        /// <summary>
        /// Event handler for recognized speech.
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">Data from the recognized speech</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // HACK : do not allow speech to be recognized one second after last tts
            TimeSpan ts = new TimeSpan(0,0,1);
            if (this.sr.LastTTS.Add(ts).CompareTo(DateTime.Now) < 0)
            {
                if (e.Result.Confidence >= this.sr.ConfidenceThreshold)
                {
                    List<String> contentSemantic = this.sr.SpeechRecognized(e.Result.Semantics, e.Result.Text);
                    //Update the text
                    this.Dispatcher.Invoke(() =>
                    {
                        this.lastSemantics.Text = "";
                        this.lastSentence.Text = e.Result.Text;
                        this.lastSemantics.Text = "";
                        if (contentSemantic != null)
                        {
                            for (int i = 0; i < contentSemantic.Count; i++)
                            {
                                this.lastSemantics.Text += contentSemantic[i];
                            }
                        }
                    });
                }

            }
        }

        /// <summary>
        /// Event handler for rejected speech.
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">Data from the rejected speech</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.lastSentence.Text = Properties.Resources.NoWordsRecognized;
                this.lastSemantics.Text = "";
            });
        }


        /// <summary>
        /// Update the XML file when the user opens a file
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">State information</param>
        private void LoadGrammarFile(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (.xml)|*.xml";

            // Display OpenFileDialog
            Nullable<bool> result = dlg.ShowDialog();

            String message = "";

            // Get the selected file path and set it in location if it is different from the actual file
            if (result == true && this.sr.IsFileNew(dlg.FileName))
            {
                ClearRecognitionText();

                // Create a new grammar from the file loaded
                message = this.sr.CreateGrammar(dlg.FileName, dlg.SafeFileName);

                this.sr.FileName = dlg.SafeFileName;
                this.RefreshGrammarFile();
                SetButtonOn(this.stackSR);

            }
            // Turn the button off if the FileDialog is closed and the SpeechEngine doesn't have any grammar loaded
            else if (result == false && !this.sr.AnyGrammarLoaded())
                SetButtonOff(this.stackSR);

            // If there is an error while creating the grammar, a message is written and the button is turned off
            if (!this.sr.AnyGrammarLoaded() || this.sr.CurrentLanguage != null && this.sr.CurrentLanguage.Equals(""))
            {
                SetButtonOff(this.stackSR);
                this.status.Text = message;
                return;
            }
            else
                this.status.Text = Properties.Resources.GoOn;
            
            if (this.sr.IsSpeechEngineSet())
                this.addlist();
        }

        /// <summary>
        /// Changing the name and the language fields from the view.
        /// </summary>
        public void RefreshGrammarFile()
        {
            Dispatcher.Invoke(() =>
            {
                this.currentFile.Text = this.sr.FileName;
                this.currentLanguage.Text = sr.CurrentLanguage;
            });
        }

        public void addlist()
        {
            this.sr.addSRListener(this.SpeechRecognized, this.SpeechRejected);
        }

        /// <summary>
        /// Updating the confidence threshold.
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">State information</param>
        private void SubmitConfidence(object sender, RoutedEventArgs e)
        {
            if (this.confidenceSelector.Value == null)
            {
                this.RefreshConfidenceSelectorValue();
            }
            else
            {
                sr.ConfidenceThreshold = (float)this.confidenceSelector.Value;
                this.ClearRecognitionText();
            }
        }

        /// <summary>
        /// Changing the confidence selector's value.
        /// </summary>
        public void RefreshConfidenceSelectorValue()
        {
            Dispatcher.Invoke(() =>
            {
                this.confidenceSelector.Value = (double)Math.Round(sr.ConfidenceThreshold,1);
            });
        }

        /// <summary>
        /// Switch the microphone source (system mic and Kinect mic array)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchMicSource(object sender, RoutedEventArgs e)
        {
            if (this.sr.IsSystemMicSet)
            {
                SetButtonOff(this.stackMic);
                this.sr.IsSystemMicSet = false;
                if (sr.AnyGrammarLoaded())
                {
                    this.sr.UnloadGrammars();
                    this.sr.CreateGrammar(this.sr.FileLocation, this.sr.FileName, this.sr.GrammarText);
                    this.addlist();
                }
            }
            else
            {
                SetButtonOn(this.stackMic);
                this.sr.IsSystemMicSet = true;
                if (this.sr.AnyGrammarLoaded())
                {
                    this.sr.UnloadGrammars();
                    this.sr.CreateGrammar(this.sr.FileLocation, this.sr.FileName, this.sr.GrammarText);
                    this.addlist();
                }
            }
        }
    }
}
