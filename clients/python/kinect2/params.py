# -*- coding: utf-8 -*-
"""
Created on Thu May 19 15:21:18 2016

@author: Lucas Gueze
"""

import json
import zmq

class Params(object):
    def __init__(self, context, ip, port):          
        self._context = context
        self._socket = self._context.socket(zmq.REQ)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self._params = {}
   
    def send_params(self):
        """
        Send the parameters on the Kinect server
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        json_str = json.dumps({self._feature: self._params})
        self._socket.send(json_str)
        message = self._socket.recv()
        return message

class SpeechParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'speech_recognition'

    def on(self):
        self._params['on'] = True
    
    def off(self):
        self._params['on'] = False
        
    def sentence_on(self):
        self._params['sentence'] = True
        
    def sentence_off(self):
        self._params['sentence'] = False
    
    def semantic_on(self):
        self._params['semantic'] = True
        
    def semantic_off(self):
        self._params['semantic'] = False
        
    def set_confidence(self, value):
        if float(value)>=0.0 and float(value)<=1.0:
            self._params['confidence'] = value
    
    def set_grammar(self, grammar, grammar_file = None):
        if isinstance(grammar, file):
            grammar = grammar.read()
        self._params['grammar'] = grammar
        if grammar_file is not None:
            self._params['fileName'] = grammar_file


class SkeletonParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'skeleton_tracking'
   
    def on(self):
         self._params['on'] = True
         
    def off(self):
         self._params['on'] = False
    
    def set_smoothing(self, value):
        if float(value)>=0.0 and float(value)<=0.9:
            self._params['smoothing'] = value
     

class TextToSpeechParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'text_to_speech'

    def queue_on(self):
        self._params['queue'] = True
        
    def queue_off(self):
        self._params['queue'] = False
    
    def set_gender(self, gender):
        '''
        :param gender: str, values can be 'male' or 'female'
        '''
        if gender == 'male':
            self._params['gender'] = 'male'
        elif gender =='female':
            self._params['gender'] = 'female'
    
    def set_language(self, language):
        '''
        :param language: str, values can be 'english' or 'french'
        '''
        if language == 'english':
            self._params['language'] = 'english'
        elif language == 'french':
            self._params['language'] = 'french'
        

class RGBDMicParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'rgbd_mic'
               
    def on(self):
         self._params['on'] = True
         
    def off(self):
         self._params['on'] = False

        
    
        
