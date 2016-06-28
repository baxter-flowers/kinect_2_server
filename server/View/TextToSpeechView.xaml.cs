 using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2Server.View
{
    /// <summary>
    /// Interaction logic for TextToSpeechView.xaml
    /// </summary>
    public partial class TextToSpeechView : UserControl
    {
        private MainWindow mw;
        private TextToSpeech tts;

        public TextToSpeechView()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.tts = mw.TextToSpeech;
            InitializeComponent();
            this.tts.AddTTSListener(UpdateText);
        }

        /// <summary>
        /// Updating the text when it's spoken
        /// </summary>
        /// <param name="sender">Object that sent the event</param>
        /// <param name="e">Data from the SpeakProgressEventArgs</param>
        private void UpdateText(object sender, SpeakProgressEventArgs e)
        {
            this.text.Text = this.tts.SpokenText;
        }

        private void Male_Checked(object sender, RoutedEventArgs e)
        {
            this.Male_C();
        }

        /// <summary>
        /// Checking the "male" box and setting the VoiceGender to Male. Also unchecking the "female" box.
        /// </summary>
        public void Male_C()
        {
            Dispatcher.Invoke(() =>
            {
                this.Female.IsChecked = false;
                this.Male.IsChecked = true;
                this.tts.VoiceGender = VoiceGender.Male;
            });
        }

        private void Female_Checked(object sender, RoutedEventArgs e)
        {
            this.Female_C();
        }

        /// <summary>
        /// Checking the "female" box and setting the VoiceGender to Female. Also unchecking the "male" box.
        /// </summary>
        public void Female_C()
        {
            Dispatcher.Invoke(() =>
            {
                this.Male.IsChecked = false;
                this.Female.IsChecked = true;
                this.tts.VoiceGender = VoiceGender.Female;
            });
            
        }

        private void enUS_Checked(object sender, RoutedEventArgs e)
        {
            this.enUS_C();
        }

        /// <summary>
        /// Checking the "american English" box and setting the Culture to en-US. Also unchecking the "French" box.
        /// </summary>
        public void enUS_C()
        {
            Dispatcher.Invoke(() =>
            {
                this.frFR.IsChecked = false;
                this.enUS.IsChecked = true;
                this.Male.IsEnabled = true;
                this.tts.Culture = new CultureInfo("en-US");
            });
        }

        private void frFR_Checked(object sender, RoutedEventArgs e)
        {
            this.frFR_C();
        }

        /// <summary>
        /// Checking the "French" box and setting the Culture to fr-FR and forces the VoiceGender to Female. Also unchecking the "american English" box.
        /// </summary>
        public void frFR_C()
        {
            Dispatcher.Invoke(() =>
            {
                this.enUS.IsChecked = false;
                this.frFR.IsChecked = true;
                this.Female.IsChecked = true;
                this.Male.IsEnabled = false;
                this.tts.Culture = new CultureInfo("fr-FR");
            });
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
    }
}
