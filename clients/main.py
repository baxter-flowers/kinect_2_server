# -*- coding: utf-8 -*-
"""
Created on Tue May 17 15:47:54 2016

@author: perception
"""

from changing_params import *

p = Params()

while True:
    var = raw_input()
    #Speech recognition
    if var == 'sr_start':
        p.change_status('sr','on')
    elif var == 'sr_stop':
        p.change_status('sr','on')
    elif var == 'sr_sen_on':
        p.change_status('sr','on','sentence')
    elif var == 'sr_sen_off':
        p.change_status('sr','off','sentence')
    elif var == 'sr_sem_on':
        p.change_status('sr','on','semantic')
    elif var == 'sr_sem_off':
        p.change_status('sr','off','semantic')
    #TODO Confidence
    #Skeleton tracking
    elif var == 'st_start':
        p.change_status('st','on')
    elif var == 'st_stop':
        p.change_status('st','off')
    #TODO Smoothing
    #Text to speech
    elif var == 'tts_queue_on':
        p.change_queue('on')
    elif var == 'tts_queue_off':
        p.change_queue('off')
    elif var == 'send_json':
        p.send_json()