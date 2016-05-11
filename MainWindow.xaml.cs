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
        private NetworkPublisher colorPublisher;
        private NetworkPublisher depthPublisher;
        private NetworkSubscriber subscriber;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognition sr;
        private SkeletonTracking st;
        private TextToSpeech tts;
        private ColorImages ci;
        private DepthImage di;

        public MainWindow()
        {
            setKinectSensor();
            this.publisher = new NetworkPublisher();
            this.publisher.Bind("33405");
            this.subscriber = new NetworkSubscriber();
            this.subscriber.Bind("33406");
            this.colorPublisher = new NetworkPublisher();
            this.colorPublisher.Bind("33407");
            this.depthPublisher = new NetworkPublisher();
            this.colorPublisher.Bind("33408");

            this.sr = new SpeechRecognition(this.kinectSensor, this.publisher, this.convertStream);
            this.st = new SkeletonTracking(this.kinectSensor, this.publisher);
            this.tts = new TextToSpeech(this.subscriber);
            this.ci = new ColorImages(this.kinectSensor, this.colorPublisher);
            this.di = new DepthImage(this.kinectSensor, this.depthPublisher);
            
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
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

        public SkeletonTracking SkeletonTracking
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