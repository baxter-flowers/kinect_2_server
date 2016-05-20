# -*- coding: utf-8 -*-

import json
from zmq import ZMQError, Context, SUB, SUBSCRIBE
from threading import Thread

class StreamSubscriber(object):
    def __init__(self, filter, ip, port, params, context=None):
        self._context = Context() if context is None else context
        self._socket = self._context.socket(SUB)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self._socket.setsockopt(SUBSCRIBE, filter)
        self._cb = None
        self._params = params
        self.running = False
          
    def _get(self):
        try:
            msg = self._socket.recv()
        except ZMQError as e:
            if e.errno == EAGAIN:
                return None
        else:
            return " ".join(msg.split(' ')[1:])

    def set_callback(self, callback_func):
        self._cb = callback_func

    def _start_client(self):
        self.running = True
        subscriber = Thread(target=self._threaded_subscriber)
        subscriber.setDaemon(True)
        subscriber.start()
   
    def stop(self):
        self.running = False
    
    def _threaded_subscriber(self):
        while self.running:
            msg = self._get()
            if callable(self._cb) and msg is not None:
                self._cb(msg)
                
class SkeletonSubscriber(StreamSubscriber):
    def __init__(self, filter, ip, port, params, context=None):
        StreamSubscriber.__init__(self, filter, ip, port, params, context)
    
    @property
    def params(self):
        return self._params.skeleton
    
    def start(self):
        # Trigger the server
        self._params.skeleton.on()
        self._params.send_params()
        # Start the client
        self._start_client()

class SpeechSubscriber(StreamSubscriber):
    def __init__(self, filter, ip, port, params, context=None):
        StreamSubscriber.__init__(self, filter, ip, port, params, context)
    
    @property
    def params(self):
        return self._params.speech
    
    def start(self):
        # Trigger the server
        self._params.speech.on()
        self._params.send_params()
        # Start the client
        self._start_client()
