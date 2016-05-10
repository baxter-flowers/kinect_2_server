using System.Globalization;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;

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
            this.tts.addTTSListener(updateText);
        }

        private void updateText(object sender, SpeakProgressEventArgs e)
        {
            this.text.Text = this.tts.SpokenText;
        }

        private void Male_Checked(object sender, RoutedEventArgs e)
        {
            this.Female.IsChecked = false;
            this.tts.VoiceGender = VoiceGender.Male;
        }

        private void Female_Checked(object sender, RoutedEventArgs e)
        {
            this.Male.IsChecked = false;
            this.tts.VoiceGender = VoiceGender.Female;
        }

        private void enUS_Checked(object sender, RoutedEventArgs e)
        {
            this.frFR.IsChecked = false;
            this.Male.IsEnabled = true;
            this.tts.Culture = new CultureInfo("en-US");
        }

        private void frFR_Checked(object sender, RoutedEventArgs e)
        {
            this.enUS.IsChecked = false;
            this.Female.IsChecked = true;
            this.Male.IsEnabled = false;
            this.tts.Culture = new CultureInfo("fr-FR");
        }
    }
}
