# -*- coding: utf-8 -*-

import json, numpy, cv2, time, math
from zmq import ZMQError, Context, SUB, SUBSCRIBE, CONFLATE, RCVHWM, EAGAIN
from threading import Thread, RLock
from .params import SkeletonParams, SpeechParams, RGBDParams, MicParams


class StreamSubscriber(object):
    def __init__(self, context, filter, ip, port, conflate = False):
        self._context = context
        self._socket = self._context.socket(SUB)
        self._socket.setsockopt(SUBSCRIBE, filter)
        if conflate:
            self._socket.setsockopt(CONFLATE, 1)
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self._cb = None
        self._filter = filter
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

    def get(self):
        if callable(self._cb):
            raise RuntimeError("[{}] Cannot use frame-by-frame reading while a callback is set, use the callback instead".format(self._filter))
        return self._get()

    def set_callback(self, callback_func):
        self._cb = callback_func

    def _start_client(self):
        if callable(self._cb):  # Do not start the daemon if no callback is set
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


class RGBDSubscriber(object):
    def __init__(self, context, ip, color_port, mapping_port, mask_port, config_port):
        self._context = context
        self._socket_color = self._context.socket(SUB)
        self._socket_mapping = self._context.socket(SUB)
        self._socket_mask = self._context.socket(SUB)
        self._socket_color.setsockopt(SUBSCRIBE, "")
        self._socket_mapping.setsockopt(SUBSCRIBE, "")
        self._socket_mask.setsockopt(SUBSCRIBE, "")
        self._socket_color.setsockopt(CONFLATE, 1)
        self._socket_mapping.setsockopt(CONFLATE, 1)
        self._socket_mask.setsockopt(CONFLATE, 1)
        #self._socket_color.setsockopt(RCVHWM, 1)
        #self._socket_mapping.setsockopt(RCVHWM, 1)
        #self._socket_mask.setsockopt(RCVHWM, 1)
        self._socket_color.connect('tcp://{}:{}'.format(ip, color_port))
        self._socket_mapping.connect('tcp://{}:{}'.format(ip, mapping_port))
        self._socket_mask.connect('tcp://{}:{}'.format(ip, mask_port))
        self._cb = None
        self._factor = 0.2547
        self.running = False
        self.continuous = False
        self._socket_color_lock = RLock()
        self._socket_mapping_lock = RLock()
        self._socket_mask_lock = RLock()
        self.params = RGBDParams(context, ip, config_port)

    def _get_color(self):
        try:
            with self._socket_color_lock:
                msg = self._socket_color.recv()
        except ZMQError as e:
            if e.errno == EAGAIN:
                return None
        else:
            return msg

    def _get_mapping(self):
        try:
            with self._socket_mapping_lock:
                msg = self._socket_mapping.recv()
        except ZMQError as e:
            if e.errno == EAGAIN:
                return None
        else:
            return msg

    def _get_mask(self):
        try:
            with self._socket_mask_lock:
                msg = self._socket_mask.recv()
        except ZMQError as e:
            if e.errno == EAGAIN:
                return None
        else:
            return msg

    def _transform_msg_into_cv2images(self, msg_color, msg_mapping, msg_mask):

        rgb_frame_numpy = numpy.fromstring(msg_color, numpy.uint8).reshape(1080, 1920,2)
        frame_rgb = cv2.cvtColor(rgb_frame_numpy, cv2.COLOR_YUV2BGR_YUY2)  # YUY2 to BGR
        mapped_frame_numpy = numpy.fromstring(msg_mapping, numpy.uint8).reshape(1080*self._factor, 1920*self._factor)
        mapped_image = numpy.uint8(cv2.normalize(mapped_frame_numpy, None, 0, 255, cv2.NORM_MINMAX))
        mask_numpy = numpy.fromstring(msg_mask, numpy.uint8).reshape(1080*self._factor, 1920*self._factor)
        mask = numpy.uint8(cv2.normalize(mask_numpy, None, 0, 255, cv2.NORM_MINMAX))

        return frame_rgb, mapped_image, mask
        

    def set_callback(self, callback_func):
        self._cb = callback_func

    def _start_client(self):
        self.running = True
        subscriber = Thread(target=self._threaded_subscriber)
        subscriber.setDaemon(True)
        subscriber.start()

    def stop(self):
        self.running = False

    def enable_continuous_stream(self):
        """
        Enable the continuous stream. When starting the client, continuous stream is disabled.
        """
        self.continuous = True
        self.params.continuous_stream_on()
        self.params.send_params()

    def enable_frame_grabbing(self):
        """
        Enable the continuous stream. When starting the client, frame grabbing is enabled. Also disable continuous stream.
        """
        self.continuous = False
        self.params.continuous_stream_off()
        self.params.send_params()

    def grab_frame(self):
        """
        Get a set of frame that contains rgb image, mapped image between depth & rgb and a mask. Rgb: 1920 * 1080, pixel format is Bgr32 (32 bits-per-pixel, each color channel (blue, green, red) is allocated 8 bits-per-pixels). Mapping & mask: 489 * 275, pixel format is Gray8 (8 bits-per-pixels, 256 shades of gray).
        Returns in order rgb image, mapped image and mask in openCV image format.
        """
        self.params.one_frame()
        self.params.send_params()
        self.params.no_frame()
        self.params.send_params()
        msg_color = self._get_color()
        msg_mapping = self._get_mapping()
        msg_mask = self._get_mask()

        return self._transform_msg_into_cv2images(msg_color, msg_mapping, msg_mask)
        

    def _threaded_subscriber(self):
        while self.running and self.continuous:
            msg_color = self._get_color()
            msg_mapping = self._get_mapping()
            msg_mask = self._get_mask()

            self._transform_msg_into_cv2images(msg_color, msg_mapping, msg_mask)
            
            if callable(self._cb) and msg_color is not None and msg_mapping is not None and msg_mask is not None:
                self._cb({msg_color, msg_mapping, msg_mask})

    def start(self):
        """
        Take into account the parameters and start the RGBD Streaming.The default streaming mode is frame by frame.
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise.
        """
        # Trigger the server
        self.params.on()
        msg = self.params.send_params()
        # Start the client
        self._start_client()
        return msg
               
                
class SkeletonSubscriber(StreamSubscriber):
    def __init__(self, context, ip, port, config_port):
        StreamSubscriber.__init__(self, context, 'skeleton', ip, port, conflate = True)
        self.params = SkeletonParams(context, ip, config_port)
    
    def start(self):
        """
        Take into account the parameters and start the Skeleton publisher
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        # Trigger the server
        self.params.on()
        msg = self.params.send_params()
        # Start the client
        self._start_client()
        return msg

class SpeechSubscriber(StreamSubscriber):
    def __init__(self, context, ip, port, config_port):
        StreamSubscriber.__init__(self, context, 'recognized_speech', ip, port)
        self.params = SpeechParams(context, ip, config_port)
   
    def start(self):
        """
        Take into account the parameters (grammar, semantics, sentence, ...) and start the Speech Recognizer
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        # Trigger the server
        self.params.on()
        msg = self.params.send_params()
        # Start the client
        self._start_client()
        return msg

class MicrophoneSubscriber(StreamSubscriber):
    def __init__(self, context, ip, port, config_port):
        StreamSubscriber.__init__(self, context, '', ip, port, conflate = False)
        self.params = MicParams(context, ip, config_port)

    def start(self):
        """
        Take into account the parameters and start the Microphone
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        # Trigger the server
        self.params.on()
        msg = self.params.send_params()
        # Start the client
        self._start_client()
        return msg

