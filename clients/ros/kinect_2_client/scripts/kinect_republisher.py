#!/usr/bin/env python
# -*- coding: utf-8 -*-
import rospy, zmq
from sensor_msgs.msg import Image
from cv_bridge import CvBridge
from threading import Thread
from numpy import fromstring, uint8, uint16
from cv2 import cvtColor, COLOR_YUV2BGR_YUY2, imshow, waitKey

class KinectRepublisher(object):
    def __init__(self, ip, json_port=33405, requests_port=33406, rgb_port=33407, d_port=33406):
        rospy.init_node('kinect_republisher')
        self.bridge = CvBridge()
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.SUB)
        self.socket.setsockopt(zmq.CONFLATE, 1)
        self.socket.setsockopt(zmq.SUBSCRIBE, "")
        self.socket.connect("tcp://{}:{}".format(ip, rgb_port))

        self.rgb_pub = rospy.Publisher('/kinect2/rgb', Image, queue_size=1)

    def run(self):
        threads = [Thread(target=self.threaded_rgb_republisher)]
        for t in threads:
            t.setDaemon(True)
            t.start()
        rospy.spin()

    def threaded_rgb_republisher(self):
        while not rospy.is_shutdown():
            frame = self.socket.recv()  # Blocking call
            frame_numpy = fromstring(frame, uint8).reshape(1080, 1920, 2)
            frame_rgb = cvtColor(frame_numpy, COLOR_YUV2BGR_YUY2)  # YUY2 to BGR
            image = self.bridge.cv2_to_imgmsg(frame_rgb, encoding='bgr8')
            self.rgb_pub.publish(image)
            #imshow("rgb", frame_rgb)
            #waitKey(1)

    def threaded_depth_republisher(self):
        while not rospy.is_shutdown():
            frame = self.socket.recv()  # Blocking call
            frame_numpy = fromstring(frame, uint16).reshape(424, 512, 2)
            frame_depth = cvtColor(frame_numpy, COLOR_YUV2BGR_YUY2)  # YUY2 to BGR
            image = self.bridge.cv2_to_imgmsg(frame_depth, encoding='bgr8')
            self.rgb_pub.publish(image)
            #imshow("depth", frame_rgb)
            #waitKey(1)

KinectRepublisher("BAXTERFLOWERS.local").run()