using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Kinect2Server
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            this.initializeSR();
            InitializeComponent();
        }

        private NetworkPublisher publisher;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognition sr;

        private void initializeSR()
        {
            setKinectSensor();
            this.publisher = new NetworkPublisher();
            this.publisher.Bind("33405");
            this.sr = new SpeechRecognition(this.kinectSensor, this.publisher, this.convertStream);
        }

        private void setKinectSensor()
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
                // on failure, set the status text and close the application
                //this.statusBarText.Text = Properties.Resources.NoKinectReady;
                System.Threading.Thread.Sleep(10000);
            }
        }

        public SpeechRecognition getSRInstance(){
            return this.sr;
        }

        public void addSRList(EventHandler<SpeechRecognizedEventArgs> f1, EventHandler<SpeechRecognitionRejectedEventArgs> f2)
        {
            sr.addSRListener(f1,f2);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (sr.isSpeechEngineSet())
            {
                sr.disableSpeechEngine();
            }

            if (null != this.kinectSensor)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
    }
}