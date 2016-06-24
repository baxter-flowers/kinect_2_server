# -*- coding: utf-8 -*-

import json
from zmq import Context, DEALER
from .params import TextToSpeechParams

class TTSRequester(object):
    def __init__(self, context, ip, port, config_port):
        self._context = context
        self._socket = self._context.socket(DEALER)
        self._socket.identity = "client"
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self.params = TextToSpeechParams(context, ip, config_port)

    def say(self, sentence, blocking = True):
        if blocking is True:
            final = "t"
        else:
            final = "f"
        #HACK : adding a space for each non ascii character
        final += sentence
        for i in range(len(sentence)):
            if ord(sentence[i])<128:
                sentence+= ' '
        self._socket.send_multipart([final])
        message = self._socket.recv_multipart()
        return None

    def start(self):
        """
        Take into account the parameters and start the TTS system
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        return self.params.send_params()
