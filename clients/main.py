# -*- coding: utf-8 -*-
"""
Created on Tue May 17 15:47:54 2016

@author: perception
"""

from changing_params import *

p = Params()

while True:
    var = raw_input('> ')
    args = var.split()
    #Speech recognition
    if var == 'sr_start':
        p.change_status('sr','on')
    elif var == 'sr_stop':
        p.change_status('sr','off')
    elif len(args)>1 and args[0] =='sr_grammar':
        grammarFile = args[1]
        grammar = open(grammarFile)
        grammar = grammar.read()
        p.change_grammar(grammar)
    elif len(args)>1 and args[0] =='sr_confidence':
        value = args[1]
        p.change_confidence_or_smoothing('conf',value)
    elif var == 'sr_sen_on':
        p.change_status('sr','on','sentence')
    elif var == 'sr_sen_off':
        p.change_status('sr','off','sentence')
    elif var == 'sr_sem_on':
        p.change_status('sr','on','semantic')
    elif var == 'sr_sem_off':
        p.change_status('sr','off','semantic')
    #Skeleton tracking
    elif var == 'st_start':
        p.change_status('st','on')
    elif var == 'st_stop':
        p.change_status('st','off')
    elif len(args)>1 and args[0] =='st_smoothing':
        value = args[1]
        p.change_confidence_or_smoothing('smoo',value)
    #Text to speech
    elif var == 'tts_queue_on':
        p.change_queue('on')
    elif var == 'tts_queue_off':
        p.change_queue('off')
    elif var =='tts_gender_male':
        p.change_gender('male')
    elif var == 'tts_gender_female':
        p.change_gender('female')
    elif var == 'tts_language_english':
        p.change_language('english')
    elif var == 'tts_language_french':
        p.change_language('french')
    elif var == 'send_json':
        p.send_json()