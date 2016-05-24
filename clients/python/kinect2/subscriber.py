# -*- coding: utf-8 -*-

import json
from zmq import ZMQError, Context, SUB, SUBSCRIBE, CONFLATE
from threading import Thread
from .params import SkeletonParams, SpeechParams

class StreamSubscriber(object):
    def __init__(self, context, filter, ip, port):
        self._context = context
        self._socket = self._context.socket(SUB)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self._socket.setsockopt(SUBSCRIBE, filter)
        self._cb = None
        self.running = False
          
    def _get(self):
        try:
            msg = self._socket.recv()
        except ZMQError as e:
            if e.errno == EAGAIN:
                return None
        else:
            str_msg = " ".join(msg.split(' ')[1:])
            return json.loads(str_msg)

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
    def __init__(self, context, filter, ip, port, config_port):
        StreamSubscriber.__init__(self, context, filter, ip, port)
        self._socket.setsockopt(CONFLATE, 1)
        self.params = SkeletonParams(context, ip, config_port)
    
    def start(self):
        # Trigger the server
        self.params.on()
        self.params.send_params()
        # Start the client
        self._start_client()

class SpeechSubscriber(StreamSubscriber):
    def __init__(self, context, filter, ip, port, config_port):
        StreamSubscriber.__init__(self, context, filter, ip, port)
        self.params = SpeechParams(context, ip, config_port)
   
    def start(self):
        # Trigger the server
        self.params.on()
        self.params.send_params()
        # Start the client
        self._start_client()
