using Kinect2Server.View;
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
        private NetworkPublisher audioPublisher;
        private NetworkSubscriber subscriber;
        private NetworkResponder responder;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognition sr;
        private SpeechRecognitionView srv;
        private SkeletonTrackingView stv;
        private TextToSpeechView ttsv;
        private RGBDplusMic rgbdplusmicv;
        private SkeletonTracking st;
        private TextToSpeech tts;
        private MultiSourceImage msi;
        private AudioFrame ab;

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
            this.depthPublisher.Bind("33408");
            this.audioPublisher = new NetworkPublisher();
            this.audioPublisher.Bind("33409");

            this.sr = new SpeechRecognition(this.kinectSensor, this.publisher, this.convertStream);
            this.st = new SkeletonTracking(this.kinectSensor, this.publisher);
            this.tts = new TextToSpeech(this.subscriber);
            this.ab = new AudioFrame(this.kinectSensor, this.audioPublisher);
            this.msi = new MultiSourceImage(this.kinectSensor, this.depthPublisher, this.colorPublisher);

            
            InitializeComponent();

            this.srv = this.srview;
            this.stv = this.stview;
            this.ttsv = this.ttsview;
            this.rgbdplusmicv = this.rgbdmicview;

            // Need to create the responder after models because it's using instance of sr, srw, st & tts
            this.responder = new NetworkResponder();
            this.responder.Bind("33410");
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

        public MultiSourceImage MultiSourceImage
        {
            get
            {
                return this.msi;
            }
        }

        public NetworkResponder NetworkResponder
        {
            get
            {
                return this.responder;
            }
        }

        public SpeechRecognitionView SpeechRecognitionView
        {
            get
            {
                return this.srv;
            }
        }

        public TextToSpeechView TextToSpeechView
        {
            get
            {
                return this.ttsv;
            }
        }

        public SkeletonTrackingView SkeletonTrackingView
        {
            get
            {
                return this.stv;
            }
        }

        public RGBDplusMic RGBDplusMic
        {
            get
            {
                return this.rgbdplusmicv;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.kinectSensor)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

    }
}