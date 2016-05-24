# -*- coding: utf-8 -*-

import json
from zmq import Context, PUB

class TTSPublisher(object):
    def __init__(self, ip, port, params, context=None):
        self._context = Context() if context is None else context
        self._socket = self._context.socket(PUB)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self._params = params

    @property
    def params(self):
        return self._params.tts

    def say(self, sentence):
        self._socket.send("{} {}".format("tts", json.dumps([sentence])))

    def start(self):
        self._params.send_params()
