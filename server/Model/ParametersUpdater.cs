using Kinect2Server.View;
using Microsoft.Speech.Recognition;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Kinect2Server
{
    class ParametersUpdater
    {
        private Thread json_thread;
        private Boolean isOn = false;
        private NetworkResponder responder;
        private TextToSpeech tts;
        private SpeechRecognition sr;
        private SkeletonFaceTracking st;
        private MultiSourceImage msi;
        private AudioFrame af;
        private SpeechRecognitionView srv;
        private SkeletonTrackingView stv;
        private TextToSpeechView ttsv;
        private RGBDplusMic rgbdmicv;
        private MainWindow mw;

        public ParametersUpdater(SpeechRecognition sr, SkeletonFaceTracking st, TextToSpeech tts, MultiSourceImage msi, AudioFrame af, SpeechRecognitionView srv, SkeletonTrackingView stv, TextToSpeechView ttsv, RGBDplusMic rgbdmicv)
        {
            this.sr = sr;
            this.st = st;
            this.tts = tts;
            this.msi = msi;
            this.af = af;
            this.srv = srv;
            this.stv = stv;
            this.ttsv = ttsv;
            this.rgbdmicv = rgbdmicv;
            this.mw = (MainWindow)Application.Current.MainWindow;

            this.responder = new NetworkResponder();
            this.responder.Bind("33412");

            this.json_thread = new Thread(new ThreadStart(this.ReceiveAndUpdate));
            this.json_thread.SetApartmentState(ApartmentState.STA);
            this.json_thread.IsBackground = true;
            this.json_thread.Start();

        }

        private void ReceiveAndUpdate()
        {
            String request = null;

            while (this.json_thread.IsAlive)
            {
                request = this.responder.Receive();
                this.UpdateParams(request);
            }
        }

        private void UpdateParams(String parameters)
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
                Nullable<Boolean> systemMic = (Nullable<Boolean>)json_params["speech_recognition"]["systemMic"];
                if (systemMic != null && (Boolean)systemMic)
                {
                    this.sr.IsSystemMicSet = true;
                    this.RefreshStatus("systemMic", true);
                }
                else
                {
                    this.sr.IsSystemMicSet = false;
                    this.RefreshStatus("systemMic", false);
                }

                if (grammar != null)
                {
                    this.sr.UnloadGrammars();
                    reply += this.sr.CreateGrammar(null, grammarFile, grammar);
                    this.srv.RefreshGrammarFile();
                    if (!this.sr.IsSpeechEngineSet())
                    {
                        this.responder.Reply(reply);
                        return;
                    }
                    this.srv.addlist();
                    if (srOn != null && (Boolean)srOn)
                    {
                        this.RefreshStatus("speech", true);
                        this.isOn = true;
                    }
                }
                if (srOn != null)
                {
                    if (this.sr.IsSpeechEngineSet())
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
                        this.responder.Reply(reply);
                        return;
                    }
                }


                // Confidence
                Nullable<float> confidence = (Nullable<float>)json_params["speech_recognition"]["confidence"];
                if (confidence != null && confidence != this.sr.ConfidenceThreshold)
                {
                    this.sr.ConfidenceThreshold = (float)confidence;
                    this.srv.RefreshConfidenceSelectorValue();
                }

                // Display
                Nullable<Boolean> display = (Nullable<Boolean>)json_params["speech_recognition"]["display"];
                
                if (display != null && (Boolean)display)
                {
                    this.mw.ChangeTabDisplay(0);
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
                    this.stv.RefreshSmoothingSelectorValue();
                }

                // Display
                Nullable<Boolean> display = (Nullable<Boolean>)json_params["skeleton_tracking"]["display"];
                if (display != null && (Boolean)display)
                {
                    this.mw.ChangeTabDisplay(1);
                }
            }

            if (json_params["text_to_speech"] != null)
            {
                // Text To Speech
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

                // Display
                Nullable<Boolean> display = (Nullable<Boolean>)json_params["text_to_speech"]["display"];
                if (display != null && (Boolean)display)
                {
                    this.mw.ChangeTabDisplay(2);
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

                if (sendFrame != null && (Boolean)sendFrame)
                    this.msi.ResetFrameBooleans();

                // Display
                Nullable<Boolean> display = (Nullable<Boolean>)json_params["rgbd"]["display"];
                if (display != null && (Boolean)display)
                {
                    this.mw.ChangeTabDisplay(3);
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


            this.responder.Reply(reply);
        }

        private void RefreshStatus(String feature, Boolean state)
        {
            if (feature.Equals("speech"))
            {
                if (state)
                    this.srv.SetButtonOn(this.srv.stackSR);
                else
                    this.srv.SetButtonOff(this.srv.stackSR);
            }
            else if (feature.Equals("skeleton"))
            {
                if (state)
                    this.stv.SetButtonOn(this.stv.stackGR);
                else
                    this.stv.SetButtonOff(this.stv.stackGR);
            }
            else if (feature.Equals("rgbd"))
            {
                if (state)
                    this.rgbdmicv.SetButtonOn(this.rgbdmicv.stackDisplay, "rgbd");
                else
                    this.rgbdmicv.SetButtonOff(this.rgbdmicv.stackDisplay, "rgbd");
            }
            else if (feature.Equals("mic"))
            {
                if (state)
                    this.rgbdmicv.SetButtonOn(this.rgbdmicv.stackMic, "mic");
                else
                    this.rgbdmicv.SetButtonOff(this.rgbdmicv.stackMic, "mic");
            }
            else if (feature.Equals("systemMic"))
            {
                if (state)
                    this.srv.SetButtonOn(this.srv.stackMic);
                else
                    this.srv.SetButtonOff(this.srv.stackMic);
            }
            else if (feature.Equals("continuousStream"))
            {
                if (state)
                    this.rgbdmicv.SetButtonOn(this.rgbdmicv.stackSending, "sending");
                else
                    this.rgbdmicv.SetButtonOff(this.rgbdmicv.stackSending, "sending");
            }

        }
    }

}
