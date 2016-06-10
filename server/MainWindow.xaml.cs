using Kinect2Server.View;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Kinect2Server
{
    public partial class MainWindow : Window
    {
        private NetworkPublisher speechPublisher;
        private NetworkPublisher skeletonPublisher;
        private NetworkPublisher colorPublisher;
        private NetworkPublisher depthPublisher;
        private NetworkPublisher audioPublisher;
        private NetworkPublisher mappingPublisher;
        private NetworkPublisher maskPublisher;
        private NetworkSubscriber ttsSubscriber;
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
        private AudioFrame af;

        public MainWindow()
        {
            setKinectSensor();
            this.speechPublisher = new NetworkPublisher();
            this.speechPublisher.Bind("33405");
            this.skeletonPublisher = new NetworkPublisher();
            this.skeletonPublisher.Bind("33406");
            this.skeletonPublisher.SetConflate();
            this.ttsSubscriber = new NetworkSubscriber();
            this.ttsSubscriber.Bind("33407");
            this.colorPublisher = new NetworkPublisher();
            this.colorPublisher.Bind("33408");
            this.colorPublisher.SetConflate();
            this.mappingPublisher = new NetworkPublisher();
            this.mappingPublisher.Bind("33409");
            this.mappingPublisher.SetConflate();
            this.maskPublisher = new NetworkPublisher();
            this.maskPublisher.Bind("33410");
            this.maskPublisher.SetConflate();
            this.audioPublisher = new NetworkPublisher();
            this.audioPublisher.Bind("33411");
            this.audioPublisher.SetConflate();

            this.sr = new SpeechRecognition(this.kinectSensor, this.speechPublisher, this.convertStream);
            this.st = new SkeletonTracking(this.kinectSensor, this.skeletonPublisher);
            this.tts = new TextToSpeech(this.ttsSubscriber);
            this.af = new AudioFrame(this.kinectSensor, this.audioPublisher);
            this.msi = new MultiSourceImage(this.kinectSensor, this.colorPublisher, this.mappingPublisher, this.maskPublisher);

            
            InitializeComponent();

            this.srv = this.srview;
            this.stv = this.stview;
            this.ttsv = this.ttsview;
            this.rgbdplusmicv = this.rgbdmicview;

            // Need to create the responder after models because it's using instance of sr, srw, st & tts
            this.responder = new NetworkResponder();
            this.responder.Bind("33412");
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

        public AudioFrame AudioFrame
        {
            get
            {
                return this.af;
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