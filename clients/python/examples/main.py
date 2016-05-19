# -*- coding: utf-8 -*-
"""
Created on Tue May 17 15:47:54 2016

@author: Lucas Gueze
"""

from kinect2.params import *

p = Params()
p.speech=p.SpeechParams()
p.skeleton=p.SkeletonParams()
p.tts=p.TextToSpeechParams()

while True:
    var = raw_input('> ')
    args = var.split()
    #Speech recognition
    if var == 'sr_start':
        p.speech.on()
    elif var == 'sr_stop':
        p.speech.off()
    elif len(args)>1 and args[0] =='sr_grammar':
        grammarFile = args[1]
        grammar = open(grammarFile)
        grammar = grammar.read()
        p.change_grammar(grammar)
    elif len(args)>1 and args[0] =='sr_confidence':
        value = args[1]
        p.speech.set_confidence(value)
    elif var == 'sr_sen_on':
        p.speech.sentence_on()
    elif var == 'sr_sen_off':
        p.speech.sentence_off()
    elif var == 'sr_sem_on':
        p.speech.semantic_on()
    elif var == 'sr_sem_off':
        p.speech.semantic_off()
    #Skeleton tracking
    elif var == 'st_start':
        p.skeleton.on()
    elif var == 'st_stop':
        p.skeleton.off()
    elif len(args)>1 and args[0] =='st_smoothing':
        value = args[1]
        p.skeleton.set_smoothing(value)
    #Text to speech
    elif var == 'tts_queue_on':
        p.tts.queue_on
    elif var == 'tts_queue_off':
        p.tts.queue_off
    elif var =='tts_gender_male':
        p.tts.set_gender('male')
    elif var == 'tts_gender_female':
        p.tts.set_gender('female')
    elif var == 'tts_language_english':
        p.tts.set_language('english')
    elif var == 'tts_language_french':
        p.tss.set_language('french')
    elif var == 'send_params':
        p.send_params()
