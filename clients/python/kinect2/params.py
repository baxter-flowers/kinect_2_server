# -*- coding: utf-8 -*-
"""
Created on Thu May 19 15:21:18 2016

@author: Lucas Gueze
"""

import json
import zmq

class SpeechParams(object):
    
    def __init__(self):
        self._speech_recognition = {}
    
    def get_params(self):
        return self._speech_recognition
        
    def set_params(self,value):
        self._speech_recognition={}
    
    def on(self):
        self._speech_recognition['on'] = True
    
    def off(self):
        self._speech_recognition['on'] = False
        
    def sentence_on(self):
        self._speech_recognition['sentence'] = True
        
    def sentence_off(self):
        self._speech_recognition['sentence'] = False
    
    def semantic_on(self):
        self._speech_recognition['semantic'] = True
        
    def semantic_off(self):
        self._speech_recognition['semantic'] = False
        
    def set_confidence(self, value):
        if float(value)>=0.0 and float(value)<=1.0:
            self._speech_recognition['confidence'] = value
    
    def set_grammar(self, grammar, grammarfile = None):
        self._speech_recognition['grammar'] = grammar
        if grammarFile is not None:
            self._speech_recognition['fileName'] = grammarFile

class SkeletonParams(object):
    
    def __init__(self):
        self._skeleton_tracking = {}
    
    def get_params(self):
        return self._skeleton_tracking
        
    def set_params(self,value):
        self._skeleton_tracking={}
     
    def on(self):
         self._skeleton_tracking['on'] = True
         
    def off(self):
         self._skeleton_tracking['on'] = False
    
    def set_smoothing(self, value):
        if float(value)>=0.0 and float(value)<=0.9:
            self._skeleton_tracking['smoothing'] = value
     
class TextToSpeechParams(object):
    
    def __init__(self):
        self._text_to_speech = {}
    
    def get_params(self):
        return self._text_to_speech
        
    def set_params(self,value):
        self._text_to_speech={}
    
    def queue_on(self):
        self._text_to_speech['queue'] = True
        
    def queue_off(self):
        self._text_to_speech['queue'] = False
    
    def set_gender(self, gender):
        '''
        :param gender: str, values can be 'male' or 'female'
        '''
        if gender == 'male':
            self._text_to_speech['gender'] = 'male'
        elif gender =='female':
            self._text_to_speech['gender'] = 'female'
    
    def set_language(self, language):
        '''
        :param language: str, values can be 'english' or 'french'
        '''
        if language == 'english':
            self._text_to_speech['language'] = 'english'
        elif language == 'french':
            self._text_to_speech['language'] = 'french'
            
class RGBDMicParams(object):
    
    def __init__(self):
        self._rgbd_mic ={}
        
    def get_params(self):
        return self._rgbd_mic
        
    def set_params(self,value):
        self._rgbd_mic={}
        
    def on(self):
         self._rgbd_mic['on'] = True
         
    def off(self):
         self._rgbd_mic['on'] = False
        

class Params(object):
    def __init__(self, ip, port=33410, context=None):          
        self._context = zmq.Context() if context is None else context
        self._socket = self._context.socket(zmq.REQ)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self.speech = SpeechParams()
        self.skeleton = SkeletonParams()
        self.tts = TextToSpeechParams()
        self.rgbd_mic = RGBDMicParams()
        
    def send_params(self):
        data = {'speech_recognition' : self.speech.get_params(), 'skeleton_tracking' : self.skeleton.get_params(), 'text_to_speech' : self.tts.get_params(), 'rgbd_mic' : self.rgbd_mic.get_params()}
        json_str = json.dumps(data)
        self._socket.send(json_str)
        message = self._socket.recv()
        return message
        
    def reset_params(self):
        self.speech.set_params({})
        self.skeleton.set_params({})
        self.tts.set_params({})
        self.rgbd_mic.set_params({})
    
        
