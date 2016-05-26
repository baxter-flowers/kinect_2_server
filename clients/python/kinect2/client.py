# -*- coding: utf-8 -*-

from zmq import Context
from .subscriber import SkeletonSubscriber, SpeechSubscriber
from .publisher import TTSPublisher

class Kinect2Client(object):
    def __init__(self, ip, params_port=33410, skel_speech_port=33405, tts_port=33406, param_port=33410):
        self.context = Context()
        self.skeleton = SkeletonSubscriber(self.context, ip, skel_speech_port, param_port)
        self.speech = SpeechSubscriber(self.context, ip, skel_speech_port, param_port)
        self.tts = TTSPublisher(self.context, ip, tts_port, param_port)
        
    def start_all(self):
        self.skeleton.start()
        self.speech.start()
        self.tts.start()
