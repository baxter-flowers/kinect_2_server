using Kinect2Server.View;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Kinect2Server
{
    public partial class MainWindow : Window
    {
        private ParametersUpdater updater;
        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognition sr;
        private SkeletonFaceTracking st;
        private TextToSpeech tts;
        private MultiSourceImage msi;
        private AudioFrame af;

        public MainWindow()
        {
            OpenKinectSensor();

            this.sr = new SpeechRecognition(this.kinectSensor, this.convertStream);
            this.st = new SkeletonFaceTracking(this.kinectSensor);
            this.tts = new TextToSpeech(this.sr);
            this.af = new AudioFrame(this.kinectSensor);
            this.msi = new MultiSourceImage(this.kinectSensor);

            
            InitializeComponent();

            // Need to create the responder after models because it's using instance of sr, srw, st & tts
            this.updater = new ParametersUpdater(this.sr, this.st, this.tts, this.msi, this.af, this.srview, this.stview, this.ttsview, this.rgbdmicview);
            
        }

        private void OpenKinectSensor()
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

        public SkeletonFaceTracking SkeletonTracking
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


        public AudioFrame AudioFrame
        {
            get
            {
                return this.af;
            }
        }

        public void ChangeTabDisplay(int index)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.Tabs.SelectedIndex = index;
            }));
            
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