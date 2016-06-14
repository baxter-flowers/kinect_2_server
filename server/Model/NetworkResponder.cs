using Kinect2Server.View;
using Microsoft.Speech.Recognition;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkResponder
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;
        private Boolean isOn = false;
        private Thread json_thread;
        private MainWindow mw;
        private TextToSpeech tts;
        private SpeechRecognition sr;
        private SkeletonTracking st;
        private MultiSourceImage msi;
        private AudioFrame af;
        private SpeechRecognitionView srv;
        private SkeletonTrackingView stv;
        private TextToSpeechView ttsv;
        private RGBDplusMic rgbdmicv;

        public NetworkResponder()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.tts = this.mw.TextToSpeech;
            this.sr = this.mw.SpeechRecogniton;
            this.st = this.mw.SkeletonTracking;
            this.msi = this.mw.MultiSourceImage;
            this.af = this.mw.AudioFrame;
            this.context = new ZContext();
            this.socket = new ZSocket(this.context, ZSocketType.REP);
            this.binded = false;
            this.srv = this.mw.SpeechRecognitionView;
            this.stv = this.mw.SkeletonTrackingView;
            this.ttsv = this.mw.TextToSpeechView;
            this.rgbdmicv = this.mw.RGBDplusMic;
            this.json_thread = new Thread(new ThreadStart(this.ReceiveJson));
            this.json_thread.SetApartmentState(ApartmentState.STA);
            this.json_thread.IsBackground = true;
            this.json_thread.Start();
        }

        public void Bind(String listeningPort)
        {
            String status = null;
            try
            {
                this.socket.Bind("tcp://*:" + listeningPort);
                this.binded = true;
            }
            catch (ZException e)
            {
                status = ("Socket connection failed, server cannot listen on port " + listeningPort + ": " + e.Message);
            }
        }

        public void ReceiveJson()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                String request = null;
                if (this.binded)
                {
                    try 
                    {
                        ZFrame frame = this.socket.ReceiveFrame();
                        request = frame.ReadString();
                        String reply= this.UpdateParams(request);
                        ZFrame message = new ZFrame(reply);
                        this.socket.Send(message);
                        
                    }
                    catch (ZException e)
                    {
                        request = "Cannot receive message: " + e.Message;
                    }
                }
                else
                {
                    request = "Cannot receive message: Not connected";
                }
            }
        }

        private String UpdateParams(String parameters)
        {
            String reply = "";

            JObject json_params = JObject.Parse(parameters);

            if (json_params["speech_recognition"] != null)
            {
                // Speech Recognition
                // On/off + grammar
                Nullable<Boolean> srOn = (Nullable<Boolean>)json_params["speech_recognition"]["on"];
                String grammarFile = (String)json_params["speech_recognition"]["fileName"];
                String grammar = (String)json_params["speech_recognition"]["grammar"];
                if (grammar != null)
                {
                    this.sr.createGrammar(null, grammarFile, grammar);
                    if (!this.sr.isSpeechEngineSet())
                    {
                        reply += "Failed to load grammar file.";
                        return reply;
                    }
                    if (grammarFile != null)
                        this.srv.RefreshGrammarFile();
                    this.srv.addlist();
                    if ((Boolean)srOn)
                    {
                        this.RefreshStatus("speech", true);
                        this.isOn = true;
                    }
                }
                if (srOn != null)
                {
                    if (this.sr.isSpeechEngineSet())
                    {
                        if ((Boolean)srOn && !this.isOn)
                        {
                            this.sr.SpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                            this.RefreshStatus("speech", true);
                            this.isOn = true;
                        }

                        else if (!(Boolean)srOn)
                        {
                            this.sr.SpeechRecognitionEngine.RecognizeAsyncStop();
                            this.RefreshStatus("speech", false);
                            this.isOn = false;
                        }

                    }
                    else if (grammar == null)
                    {
                        reply += "Speech engine not set, you have to send grammar file.";
                        return reply;
                    }
                }


                // Confidence
                Nullable<float> confidence = (Nullable<float>)json_params["speech_recognition"]["confidence"];
                if (confidence != null && confidence != this.sr.ConfidenceThreshold)
                {
                    this.sr.ConfidenceThreshold = (float)confidence;
                    this.srv.RefreshConfidenceSelectorValue();
                }
            }

            if (json_params["skeleton_tracking"] != null)
            {
                // Skeleton Tracking
                // On/off
                Nullable<Boolean> stOn = (Nullable<Boolean>)json_params["skeleton_tracking"]["on"];
                if (stOn != null)
                {
                    if ((Boolean)stOn)
                        this.st.BodyFrameReader.IsPaused = false;
                    else
                        this.st.BodyFrameReader.IsPaused = true;
                    this.RefreshStatus("skeleton", (Boolean)stOn);
                }

                // Smoothing
                Nullable<float> smoothing = (Nullable<float>)json_params["skeleton_tracking"]["smoothing"];
                if (smoothing != null && smoothing != this.st.SmoothingParam)
                {
                    this.st.SmoothingParam = (float)smoothing;
                    this.stv.refreshSmoothingSelectorValue();
                }
            }

            if (json_params["text_to_speech"] != null)
            {
                // Text To Speech
                // Queue on/off
                Nullable<Boolean> queue = (Nullable<Boolean>)json_params["text_to_speech"]["queue"];
                if (queue != null)
                {
                    if ((Boolean)queue)
                        this.tts.QueuedMessages = true;
                    else
                        this.tts.QueuedMessages = false;
                    this.RefreshStatus("tts", (Boolean)queue);
                }

                // Gender
                String gender = (String)json_params["text_to_speech"]["gender"];
                if (gender != null)
                {
                    if (gender.Equals("male"))
                    {
                        this.tts.VoiceGender = VoiceGender.Male;
                        this.ttsv.Male_C();
                    }

                    else if (gender.Equals("female"))
                    {
                        this.tts.VoiceGender = VoiceGender.Female;
                        this.ttsv.Female_C();
                    }

                }

                // Language
                String language = (String)json_params["text_to_speech"]["language"];
                if (language != null)
                {
                    if (language.Equals("english"))
                    {
                        this.tts.Culture = new CultureInfo("en-US");
                        this.ttsv.enUS_C();
                    }
                    else if (language.Equals("french"))
                    {
                        this.tts.Culture = new CultureInfo("fr-FR");
                        this.ttsv.frFR_C();
                    }
                }
            }

            if (json_params["rgbd"] != null)
            {
                // RGB-D
                // On/off
                Nullable<Boolean> rOn = (Nullable<Boolean>)json_params["rgbd"]["on"];
                Nullable<Boolean> continuousStream = (Nullable<Boolean>)json_params["rgbd"]["continuousStream"];
                Nullable<Boolean> sendFrame = (Nullable<Boolean>)json_params["rgbd"]["send"];
                if (rOn != null)
                    this.RefreshStatus("rgbd", (Boolean)rOn);

                if (continuousStream != null)
                    this.RefreshStatus("continuousStream", (Boolean)continuousStream);

                if (sendFrame != null)
                {
                    if ((Boolean)sendFrame)
                        this.msi.ResetFrameBooleans();
                }
            }

            if (json_params["mic"] != null)
            {
                // Microphone
                // On/off
                Nullable<Boolean> mOn = (Nullable<Boolean>)json_params["mic"]["on"];
                if (mOn != null)
                    this.RefreshStatus("mic", (Boolean)mOn);
            }
            return reply;
        }

        private void RefreshStatus(String feature, Boolean state)
        {
            if (feature.Equals("speech"))
            {
                if (state)
                    this.srv.setButtonOn(this.srv.stackSR);
                else
                    this.srv.setButtonOff(this.srv.stackSR);
            }
            else if(feature.Equals("skeleton"))
            {
                if (state)
                    this.stv.setButtonOn(this.stv.stackGR);
                else
                    this.stv.setButtonOff(this.stv.stackGR);
            }
            else if (feature.Equals("tts"))
            {
                if (state)
                    this.ttsv.setButtonOn(this.ttsv.stackQueue);
                else
                    this.ttsv.setButtonOff(this.ttsv.stackQueue);
            }
            else if (feature.Equals("rgbd"))
            {
                if (state)
                    this.rgbdmicv.setButtonOn(this.rgbdmicv.stackDisplay ,"rgbd");
                else
                    this.rgbdmicv.setButtonOff(this.rgbdmicv.stackDisplay, "rgbd");
            }
            else if (feature.Equals("mic"))
            {
                if (state)
                    this.rgbdmicv.setButtonOn(this.rgbdmicv.stackMic, "mic");
                else
                    this.rgbdmicv.setButtonOff(this.rgbdmicv.stackMic, "mic");
            }
            else if (feature.Equals("continuousStream"))
            {
                if (state)
                    this.rgbdmicv.setButtonOn(this.rgbdmicv.stackSending, "sending");
                else
                    this.rgbdmicv.setButtonOff(this.rgbdmicv.stackSending, "sending");
            }

        }

        public void Close()
        {
            this.socket.Close();
            this.binded = false;
        }

        ~NetworkResponder()
        {
            this.context.Dispose();
        }
    }
}
