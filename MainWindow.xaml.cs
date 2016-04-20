using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2Server
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            this.InitializeComponent();
        }

/*---------------------------------------------------------------------------------------
* 
*                                WINDOW 
* 
*----------------------------------------------------------------------------------------*/



        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //setKinectSensor(sender, e);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            /*if (null != sr.convertStream)
            {
                sr.convertStream.SpeechActive = false;
            }

            if (null != sr.speechEngine)
            {
                sr.speechEngine.SpeechRecognized -= sr.SpeechRecognized;
                sr.speechEngine.SpeechRecognitionRejected -= sr.SpeechRejected;
                sr.speechEngine.RecognizeAsyncStop();
            }

            if (null != sr.kinectSensor)
            {
                sr.kinectSensor.Close();
                sr.kinectSensor = null;
            }*/
        }
    }
}