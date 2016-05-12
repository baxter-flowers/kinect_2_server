#!/usr/bin/env python
# -*- coding: utf-8 -*-
import rospy, zmq
from sensor_msgs.msg import Image
from cv_bridge import CvBridge
from threading import Thread
from numpy import fromstring, uint8, uint16
from cv2 import cvtColor, COLOR_YUV2BGR_YUY2, imshow, waitKey, normalize, NORM_MINMAX

class KinectRepublisher(object):
    def __init__(self, ip, json_port=33405, requests_port=33406, rgb_port=33407, d_port=33408):
        rospy.init_node('kinect_republisher')
        self.bridge = CvBridge()
        self.context = zmq.Context()

        # RGB
        self.rgb_socket = self.context.socket(zmq.SUB)
        self.rgb_socket.setsockopt(zmq.CONFLATE, 1)
        self.rgb_socket.setsockopt(zmq.SUBSCRIBE, "")
        self.rgb_socket.connect("tcp://{}:{}".format(ip, rgb_port))

        # DEPTH
        self.depth_socket = self.context.socket(zmq.SUB)
        self.depth_socket.setsockopt(zmq.CONFLATE, 1)
        self.depth_socket.setsockopt(zmq.SUBSCRIBE, "")
        self.depth_socket.connect("tcp://{}:{}".format(ip, d_port))

        self.rgb_pub = rospy.Publisher('/kinect2/rgb', Image, queue_size=1)
        self.depth_pub = rospy.Publisher('/kinect2/depth', Image, queue_size=1)

    def run(self):
        threads = [Thread(target=self.threaded_rgb_republisher),
                   Thread(target=self.threaded_depth_republisher)]
        for t in threads:
            t.setDaemon(True)
            t.start()
        rospy.spin()

    def threaded_rgb_republisher(self):
        while not rospy.is_shutdown():
            frame = self.rgb_socket.recv()  # Blocking call
            frame_numpy = fromstring(frame, uint8).reshape(1080, 1920, 2)
            frame_rgb = cvtColor(frame_numpy, COLOR_YUV2BGR_YUY2)  # YUY2 to BGR
            image = self.bridge.cv2_to_imgmsg(frame_rgb, encoding='bgr8')
            self.rgb_pub.publish(image)
            #imshow("rgb", frame_rgb)
            #waitKey(10)

    def threaded_depth_republisher(self):
        while not rospy.is_shutdown():
            frame = self.depth_socket.recv()  # Blocking call
            frame_numpy = fromstring(frame, uint16).reshape(424, 512, 1)
            frame_depth = uint8(normalize(frame_numpy, None, 0, 255, NORM_MINMAX))
            image = self.bridge.cv2_to_imgmsg(frame_depth, encoding='mono8')
            self.depth_pub.publish(image)
            #imshow("depth", frame_depth)
            #waitKey(10)

KinectRepublisher("BAXTERFLOWERS.local").run()