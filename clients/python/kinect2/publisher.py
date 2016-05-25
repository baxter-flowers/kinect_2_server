# -*- coding: utf-8 -*-

import json
from zmq import Context, PUB
from .params import TextToSpeechParams

class TTSPublisher(object):
    def __init__(self, context, ip, port, config_port):
        self._context = context
        self._socket = self._context.socket(PUB)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self.params = TextToSpeechParams(context, ip, config_port)

    def say(self, sentence):
        self._socket.send("{} {}".format("tts", json.dumps([sentence])))

    def start(self):
        """
        Take into account the parameters and start the TTS system
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        return self.params.send_params()
