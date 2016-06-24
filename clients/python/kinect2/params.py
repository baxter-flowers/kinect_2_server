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

    def on(self):
        self._params['on'] = True
    
    def off(self):
        self._params['on'] = False

    def display(self):
        self._params['display'] = True
        self.send_params()
        self._params['display'] 

class SpeechParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'speech_recognition'

    def set_confidence(self, value):
        if float(value)>=0.0 and float(value)<=1.0:
            self._params['confidence'] = value
    
    def set_grammar(self, grammar, grammar_file = None):
        """
        Setting a new grammar from a full copy/past grammar file.
        """
        if isinstance(grammar, file):
            grammar = grammar.read()
        self._params['grammar'] = grammar
        if grammar_file is not None:
            self._params['fileName'] = grammar_file

    def set_vocabulary(self, vocabulary, language="en-US"):
        """
        Create a grammar from a list or dictionary. The list only contains words (the semantic is going to be equal to the word). The dictionary contains words and their associated semantics. Default language is american English, insert the "Language Culture Name" ("fr-FR", "en-US",...) of the language you want to change it.
        """
        grammar = "<grammar version=\"1.0\" xml:lang=\""
        grammar += language
        grammar += "\" root=\"rule\" xmlns=\"http://www.w3.org/2001/06/grammar\" tag-format=\"semantics/1.0\"> "
        grammar += "<rule id=\"rule\" scope=\"public\"> <one-of> "
        
        if isinstance(vocabulary,list):
            for word in vocabulary:
                grammar += "<item> "
                grammar += word
                grammar += " <tag> out.word = \""
                grammar += word
                grammar += "\"; </tag> </item> "
        
        if isinstance(vocabulary, dict):
            for key in vocabulary:
                grammar += "<item> "
                grammar += key
                grammar += " <tag> out.word = \""
                grammar += vocabulary[key]
                grammar += "\"; </tag> </item> "

        grammar +="</one-of> </rule> </grammar>"
        
        self._params['grammar'] = grammar
        return grammar
        


class SkeletonParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'skeleton_tracking'
    
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
        self._params['language'] = language
        

class RGBDParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'rgbd'

    def continuous_stream_on(self):
        self._params['continuousStream'] = True
         
    def continuous_stream_off(self):
        self._params['continuousStream'] = False

    def one_frame(self):
        self._params['send'] = True

    def no_frame(self):
        self._params['send'] = False


class MicParams(Params):
    def __init__(self, context, ip, port=33410):
        Params.__init__(self, context, ip, port)
        self._feature = 'mic'

    def on(self):
         self._params['on'] = True
         
    def off(self):
         self._params['on'] = False
