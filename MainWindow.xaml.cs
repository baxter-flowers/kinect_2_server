using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Speech.Synthesis;
using System.Windows;

namespace Kinect2Server
{
    public partial class MainWindow : Window
    {
        private NetworkPublisher publisher;
        private NetworkSubscriber subscriber;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognition sr;
        private SkeletonTracking st;
        private TextToSpeech tts;

        public MainWindow()
        {
            setKinectSensor();
            this.publisher = new NetworkPublisher();
            this.publisher.Bind("33405");
            this.subscriber = new NetworkSubscriber();
            this.subscriber.Bind("33406");

            this.initializeSR();
            this.initializeGR();
            this.tts = new TextToSpeech(this.subscriber);
            
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void initializeSR()
        {
            this.sr = new SpeechRecognition(this.kinectSensor, this.publisher, this.convertStream);
        }

        private void initializeGR()
        {
            this.st = new SkeletonTracking(this.kinectSensor, this.publisher);
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
        }

        public SpeechRecognition SpeechRecogniton
        {
            get
            {
                return this.sr;
            }
        }

        public SkeletonTracking GestureRecognition
        {
            get
            {
                return this.st;
            }
        }

        public KinectSensor KinectSensor
        {
            get
            {
                return this.kinectSensor;
            }
        }

        public TextToSpeech TextToSpeech
        {
            get
            {
                return this.tts;
            }
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