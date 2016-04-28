using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Kinect2Server
{
    public class SkeletonTracking
    {
        private NetworkPublisher network;
        private Body[] bodies;
        private KinectSensor kinectSensor;
        private BodyFrameReader bodyFrameReader;
        private CoordinateMapper coordinateMapper;
        private const float InferredZPositionClamp = 0.1f;

        public SkeletonTracking(KinectSensor kinect, NetworkPublisher network)
        {
            this.kinectSensor = kinect;
            this.network = network;

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
        }

        public void addGRListener(EventHandler<BodyFrameArrivedEventArgs> f1)
        {
            this.bodyFrameReader.FrameArrived += f1;
        }

        public void removeGRListener(EventHandler<BodyFrameArrivedEventArgs> f1)
        {
            this.bodyFrameReader.FrameArrived -= f1;
        }

        public Body[] Bodies
        {
            get
            {
                return this.bodies;
            }
            set
            {
                this.bodies = value;
            }
        }

        public CoordinateMapper CoordinateMapper
        {
            get
            {
                return this.coordinateMapper;
            }
        }

        public NetworkPublisher NetworkPublisher
        {
            get
            {
                return this.network;
            }
        }

        public Dictionary<JointType, Vector4> chainQuat(Body body)
        {
            Dictionary<JointType, Vector4> dicoPos = new Dictionary<JointType, Vector4>();

            // Really really really ugly
            dicoPos[JointType.SpineBase] = body.JointOrientations[JointType.SpineBase].Orientation;
            dicoPos[JointType.Neck] = changeQuaternion(JointType.Neck, JointType.SpineShoulder, body);
            dicoPos[JointType.SpineShoulder] = changeQuaternion(JointType.SpineShoulder, JointType.SpineMid, body);
            dicoPos[JointType.SpineMid] = changeQuaternion(JointType.SpineMid, JointType.SpineBase, body);
            dicoPos[JointType.HandRight] = changeQuaternion(JointType.HandRight, JointType.WristRight, body);
            dicoPos[JointType.WristRight] = changeQuaternion(JointType.WristRight, JointType.ElbowRight, body);
            dicoPos[JointType.ElbowRight] = changeQuaternion(JointType.ElbowRight, JointType.ShoulderRight, body);
            dicoPos[JointType.ShoulderRight] = changeQuaternion(JointType.ShoulderRight, JointType.SpineShoulder, body);
            dicoPos[JointType.HandLeft] = changeQuaternion(JointType.HandLeft, JointType.WristLeft, body);
            dicoPos[JointType.WristLeft] = changeQuaternion(JointType.WristLeft, JointType.ElbowLeft, body);
            dicoPos[JointType.ElbowLeft] = changeQuaternion(JointType.ElbowLeft, JointType.ShoulderLeft, body);
            dicoPos[JointType.ShoulderLeft] = changeQuaternion(JointType.ShoulderLeft, JointType.SpineShoulder, body);
            dicoPos[JointType.AnkleRight] = changeQuaternion(JointType.AnkleRight, JointType.KneeRight, body);
            dicoPos[JointType.KneeRight] = changeQuaternion(JointType.KneeRight, JointType.HipRight, body);
            dicoPos[JointType.HipRight] = changeQuaternion(JointType.HipRight, JointType.SpineBase, body);
            dicoPos[JointType.AnkleLeft] = changeQuaternion(JointType.AnkleLeft, JointType.KneeLeft, body);
            dicoPos[JointType.KneeLeft] = changeQuaternion(JointType.KneeLeft, JointType.HipLeft, body);
            dicoPos[JointType.HipLeft] = changeQuaternion(JointType.HipLeft, JointType.SpineBase, body);
            dicoPos[JointType.HandTipRight] = dicoPos[JointType.HandRight];
            dicoPos[JointType.ThumbRight] = dicoPos[JointType.WristRight];
            dicoPos[JointType.HandTipLeft] = dicoPos[JointType.HandLeft];
            dicoPos[JointType.ThumbLeft] = dicoPos[JointType.WristLeft];
            dicoPos[JointType.FootRight] = dicoPos[JointType.AnkleRight];
            dicoPos[JointType.FootLeft] = dicoPos[JointType.AnkleLeft];
            dicoPos[JointType.Head] = dicoPos[JointType.Neck];

            return dicoPos;
            
        }

        private Vector4 changeQuaternion(JointType jChild, JointType jParent, Body body)
        {
            Vector4 orientation = body.JointOrientations[jChild].Orientation;
            Quaternion q = new Quaternion();
            q.X = orientation.X;
            q.Y = orientation.Y;
            q.Z = orientation.Z;
            q.W = orientation.W;

            Vector4 orientation2 = body.JointOrientations[jParent].Orientation;
            Quaternion q2 = new Quaternion();
            q2.X = orientation2.X;
            q2.Y = orientation2.Y;
            q2.Z = orientation2.Z;
            q2.W = orientation2.W;

            Quaternion final = Quaternion.Multiply(q, q2);
            orientation.X = (float)final.X;
            orientation.Y = (float)final.Y;
            orientation.Z = (float)final.Z;
            orientation.W = (float)final.W;

            return orientation;
        }
    }
}
