using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Kinect;
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

            // Really ugly
            dicoPos[JointType.SpineBase] = body.JointOrientations[JointType.SpineBase].Orientation;
            dicoPos[JointType.Neck] = qFeuilles(JointType.Neck, JointType.SpineShoulder, body);
            dicoPos[JointType.SpineShoulder] = qFeuilles(JointType.SpineShoulder, JointType.SpineMid, body);
            dicoPos[JointType.SpineMid] = qFeuilles(JointType.SpineMid, JointType.SpineBase, body);
            dicoPos[JointType.HandRight] = qFeuilles(JointType.HandRight, JointType.WristRight, body);
            dicoPos[JointType.WristRight] = qFeuilles(JointType.WristRight, JointType.ElbowRight, body);
            dicoPos[JointType.ElbowRight] = qFeuilles(JointType.ElbowRight, JointType.ShoulderRight, body);
            dicoPos[JointType.ShoulderRight] = qFeuilles(JointType.ShoulderRight, JointType.SpineShoulder, body);
            dicoPos[JointType.HandLeft] = qFeuilles(JointType.HandLeft, JointType.WristLeft, body);
            dicoPos[JointType.WristLeft] = qFeuilles(JointType.WristLeft, JointType.ElbowLeft, body);
            dicoPos[JointType.ElbowLeft] = qFeuilles(JointType.ElbowLeft, JointType.ShoulderLeft, body);
            dicoPos[JointType.ShoulderLeft] = qFeuilles(JointType.ShoulderLeft, JointType.SpineShoulder, body);
            dicoPos[JointType.AnkleRight] = qFeuilles(JointType.AnkleRight, JointType.KneeRight, body);
            dicoPos[JointType.KneeRight] = qFeuilles(JointType.KneeRight, JointType.HipRight, body);
            dicoPos[JointType.HipRight] = qFeuilles(JointType.HipRight, JointType.SpineBase, body);
            dicoPos[JointType.FootLeft] = qFeuilles(JointType.FootLeft, JointType.AnkleLeft, body);
            dicoPos[JointType.AnkleLeft] = qFeuilles(JointType.AnkleLeft, JointType.KneeLeft, body);
            dicoPos[JointType.KneeLeft] = qFeuilles(JointType.KneeLeft, JointType.HipLeft, body);
            dicoPos[JointType.HipLeft] = qFeuilles(JointType.HipLeft, JointType.SpineBase, body);
            dicoPos[JointType.HandTipRight] = dicoPos[JointType.HandRight];
            dicoPos[JointType.ThumbRight] = dicoPos[JointType.HandRight];
            dicoPos[JointType.HandTipLeft] = dicoPos[JointType.HandLeft];
            dicoPos[JointType.ThumbLeft] = dicoPos[JointType.HandLeft];
            dicoPos[JointType.FootRight] = dicoPos[JointType.AnkleRight];
            dicoPos[JointType.FootLeft] = dicoPos[JointType.AnkleLeft];
            dicoPos[JointType.Head] = dicoPos[JointType.Neck];

            return dicoPos;
            
        }

        private Vector4 qFeuilles(JointType j, JointType jPere, Body body)
        {
            Vector4 orientation = body.JointOrientations[j].Orientation;
            Quaternion q = new Quaternion();
            q.X = orientation.X;
            q.Y = orientation.Y;
            q.Z = orientation.Z;
            q.W = orientation.W;

            Vector4 orientation2 = body.JointOrientations[jPere].Orientation;
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
