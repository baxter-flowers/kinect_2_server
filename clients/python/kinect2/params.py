# -*- coding: utf-8 -*-
"""
Created on Tue May 17 15:12:43 2016

@author: perception
"""

import json
import zmq
import sys

class Params:
    
    speech_recognition={}
    skeleton_tracking={}
    text_to_speech={}
    port = None
    context = None
    socket = None
    
    def __init__(self):
        if len(sys.argv) > 1:
            port = sys.argv[1]
            int(port)
        else:
            port='33410'
            
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.REQ)
        self.socket.connect('tcp://BAXTERFLOWERS.local:%s' %port)
    
    def change_status(self, feature, status, param='start'):
        '''
        :param feature: Type: str (sr for Speech Recognition, st for Skeleton Tracking)
                        Represent the feature we want to change a setting
        :param status:  Type: str (on,off)
                        
        :param param:   Type: str (start, sentence, semantic)
                        Represent the param we want to change : start: starting or stoping the feature
                                                                sentence: displaying or not the full sentence of the recognized speech
                                                                semantic: displaying or not the semantic of the recognized speech
        '''
        if status == 'on':
            self.set_status_on(feature,param)
        elif status == 'off':
            self.set_status_off(feature,param)
    
    def set_status_on(self, feature, param):
        '''
        :param feature: See :param feature of change_status
        :param param: See :param param of change_status
        '''
        if feature == 'st':
            self.skeleton_tracking['on'] = True
        elif feature == 'sr':
            if param == 'start':
                self.speech_recognition['on'] = True
            elif param == 'sentence':
                self.speech_recognition['sentence'] = True
            elif param == 'semantic':
                self.speech_recognition['semantic'] = True
    
    def set_status_off(self, feature, param):
        '''
        :param feature: See :param feature of change_status
        :param param: See :param param of change_status
        '''
        if feature == 'st':
            self.skeleton_tracking['on'] = False
        elif feature == 'sr':
            if param == 'start':
               self.speech_recognition['on'] = False
            elif param == 'sentence':
                self.speech_recognition['sentence'] = False
            elif param == 'semantic':
                self.speech_recognition['semantic'] = False
                
    def change_grammar(self,grammar):
        self.speech_recognition['grammar'] = grammar
    
    def change_confidence_or_smoothing(self, param, value):
        '''
        :param param: Type: str (conf for Confidence of Speech Recognition, smoo for Smoothing of Skeleton Tracking)
                            Represente the param we want to change the value
        :param value  Type: float (from 0.0 to 1.0)
                            Represent the new value of the confidence or the smoothing
        '''
        if float(value)>=0.0 and float(value)<=1.0:
            if param == 'conf':
                self.speech_recognition['confidence'] = value
            elif param == 'smoo':
                self.skeleton_tracking['smoothing'] = value
                
    def change_queue(self, status):
        '''
        :param status:  Type: str (on or off)
                        Enable of disable queued messages for Text To Speech
        '''
        if status == 'on':
            self.text_to_speech['queue'] = True
        elif status == 'off':
            self.text_to_speech['queue'] = False
    
    def change_gender(self,gender):
        if gender == 'male':
            self.text_to_speech['gender'] = 'male'
        elif gender =='female':
            self.text_to_speech['gender'] = 'female'
            
    def change_language(self,language):
        if language == 'english':
            self.text_to_speech['language'] = 'english'
        elif language == 'french':
            self.text_to_speech['language'] = 'french'
    
    def send_json(self):
        data ={'speech_recognition' : self.speech_recognition, 'skeleton_tracking' : self.skeleton_tracking, 'text_to_speech' : self.text_to_speech}
        json_str = json.dumps(data)
        print('Sending: ', json_str)
        self.socket.send(json_str)
        message = self.socket.recv()
        print('Received: ', message)