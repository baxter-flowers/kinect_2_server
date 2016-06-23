# -*- coding: utf-8 -*-

import json
from zmq import Context, REQ
from .params import TextToSpeechParams

class TTSRequester(object):
    def __init__(self, context, ip, port, config_port):
        self._context = context
        self._socket = self._context.socket(REQ)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self.params = TextToSpeechParams(context, ip, config_port)

    def say(self, sentence):
        #HACK : adding a space for each non ascii character
        for i in range(len(sentence)):
            if ord(sentence[i])<128:
                sentence+= ' '
        self._socket.send(sentence)
        message = self._socket.recv()
        return None

    def start(self):
        """
        Take into account the parameters and start the TTS system
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        return self.params.send_params()
