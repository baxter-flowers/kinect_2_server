using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.Windows;
using Newtonsoft.Json;

namespace Kinect2Server
{
    public class SkeletonTracking
    {
        private NetworkPublisher network;
        private Body[] bodies;
        private KinectSensor kinectSensor;
        private BodyFrameReader bodyFrameReader;
        private CoordinateMapper coordinateMapper;
        private Dictionary<JointType, object> dicoPos;
        private Dictionary<JointType, Vector4> dicoOr;
        private Dictionary<ulong, Dictionary<JointType, object>> dicoBodies;
        private Dictionary<JointType, Point> jointPoints;
        private Quaternion qChild;
        private Quaternion qParent;
        private const float InferredZPositionClamp = 0.1f;

        private KinectJointFilter filter;
        private float smoothingParam = 0.5f;

        public SkeletonTracking(KinectSensor kinect, NetworkPublisher network)
        {
            this.kinectSensor = kinect;
            this.network = network;

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            this.filter = new KinectJointFilter(smoothingParam, smoothingParam, smoothingParam);
            this.filter.Init(smoothingParam, smoothingParam, smoothingParam);

            this.dicoPos = new Dictionary<JointType, object>(25);
            this.jointPoints = new Dictionary<JointType, Point>(25);
            this.dicoBodies = new Dictionary<ulong, Dictionary<JointType, object>>(25);
            this.dicoOr = new Dictionary<JointType, Vector4>(25);
            this.qChild = new Quaternion();
            this.qParent = new Quaternion();
        }

        public void addSTListener(EventHandler<BodyFrameArrivedEventArgs> f1)
        {
            this.bodyFrameReader.FrameArrived += f1;
        }

        public BodyFrameReader BodyFrameReader
        {
            get
            {
                return this.bodyFrameReader;
            }
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

        public float SmoothingParam
        {
            get
            {
                return this.smoothingParam;
            }
            set
            {
                this.smoothingParam = value;
                this.filter.Init(smoothingParam, smoothingParam, smoothingParam);
            }
        }

        public KinectJointFilter Filter
        {
            get
            {
                return this.filter;
            }
        }

        public Dictionary<ulong, Dictionary<JointType, object>> DicoBodies
        {
            get
            {
                return this.dicoBodies;
            }
        }

        public void chainQuat(Body body)
        {

            // Really really really ugly
            this.dicoOr[JointType.SpineBase] = body.JointOrientations[JointType.SpineBase].Orientation;
            this.dicoOr[JointType.Neck] = changeQuaternion(JointType.Neck, JointType.SpineShoulder, body);
            this.dicoOr[JointType.SpineShoulder] = changeQuaternion(JointType.SpineShoulder, JointType.SpineMid, body);
            this.dicoOr[JointType.SpineMid] = changeQuaternion(JointType.SpineMid, JointType.SpineBase, body);
            this.dicoOr[JointType.HandRight] = changeQuaternion(JointType.HandRight, JointType.WristRight, body);
            this.dicoOr[JointType.WristRight] = changeQuaternion(JointType.WristRight, JointType.ElbowRight, body);
            this.dicoOr[JointType.ElbowRight] = changeQuaternion(JointType.ElbowRight, JointType.ShoulderRight, body);
            this.dicoOr[JointType.ShoulderRight] = changeQuaternion(JointType.ShoulderRight, JointType.SpineShoulder, body);
            this.dicoOr[JointType.HandLeft] = changeQuaternion(JointType.HandLeft, JointType.WristLeft, body);
            this.dicoOr[JointType.WristLeft] = changeQuaternion(JointType.WristLeft, JointType.ElbowLeft, body);
            this.dicoOr[JointType.ElbowLeft] = changeQuaternion(JointType.ElbowLeft, JointType.ShoulderLeft, body);
            this.dicoOr[JointType.ShoulderLeft] = changeQuaternion(JointType.ShoulderLeft, JointType.SpineShoulder, body);
            this.dicoOr[JointType.AnkleRight] = changeQuaternion(JointType.AnkleRight, JointType.KneeRight, body);
            this.dicoOr[JointType.KneeRight] = changeQuaternion(JointType.KneeRight, JointType.HipRight, body);
            this.dicoOr[JointType.HipRight] = changeQuaternion(JointType.HipRight, JointType.SpineBase, body);
            this.dicoOr[JointType.AnkleLeft] = changeQuaternion(JointType.AnkleLeft, JointType.KneeLeft, body);
            this.dicoOr[JointType.KneeLeft] = changeQuaternion(JointType.KneeLeft, JointType.HipLeft, body);
            this.dicoOr[JointType.HipLeft] = changeQuaternion(JointType.HipLeft, JointType.SpineBase, body);
            this.dicoOr[JointType.HandTipRight] = this.dicoOr[JointType.HandRight];
            this.dicoOr[JointType.ThumbRight] = this.dicoOr[JointType.WristRight];
            this.dicoOr[JointType.HandTipLeft] = this.dicoOr[JointType.HandLeft];
            this.dicoOr[JointType.ThumbLeft] = this.dicoOr[JointType.WristLeft];
            this.dicoOr[JointType.FootRight] = this.dicoOr[JointType.AnkleRight];
            this.dicoOr[JointType.FootLeft] = this.dicoOr[JointType.AnkleLeft];
            this.dicoOr[JointType.Head] = this.dicoOr[JointType.Neck];
            
        }

        private Vector4 changeQuaternion(JointType jChild, JointType jParent, Body body)
        {
            Vector4 orientation = body.JointOrientations[jChild].Orientation;
            this.qChild.X = orientation.X;
            this.qChild.Y = orientation.Y;
            this.qChild.Z = orientation.Z;
            this.qChild.W = orientation.W;

            Vector4 orientation2 = body.JointOrientations[jParent].Orientation;
            this.qParent.X = orientation2.X;
            this.qParent.Y = orientation2.Y;
            this.qParent.Z = orientation2.Z;
            this.qParent.W = orientation2.W;

            Quaternion final = Quaternion.Multiply(this.qChild, this.qParent);
            orientation.X = (float)final.X;
            orientation.Y = (float)final.Y;
            orientation.Z = (float)final.Z;
            orientation.W = (float)final.W;

            return orientation;
        }

        public Dictionary<JointType, Point> frameTreatement(IReadOnlyDictionary<JointType, Joint> joints, Body body)
        {
            this.chainQuat(body);

            foreach (JointType jointType in joints.Keys)
            {
                // sometimes the depth(Z) of an inferred joint may show as negative
                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                CameraSpacePoint point = joints[jointType].Position;

                if (point.Z < 0)
                {
                    point.Z = InferredZPositionClamp;
                }

                object ob;
                if (jointType == JointType.HandRight)
                {
                    ob = new { Position = point, Orientation = dicoOr[jointType], HandState = body.HandRightState.ToString() };
                }
                else if (jointType == JointType.HandLeft)
                {
                    ob = new { Position = point, Orientation = dicoOr[jointType], HandState = body.HandLeftState.ToString() };
                }
                else
                {
                    ob = new { Position = point, Orientation = dicoOr[jointType] };
                }
                this.dicoPos[jointType] = ob;

                DepthSpacePoint depthSpacePoint = this.CoordinateMapper.MapCameraPointToDepthSpace(point);
                this.jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);

                this.dicoBodies[body.TrackingId] = this.dicoPos;
                
            }
            string json = JsonConvert.SerializeObject(this.dicoBodies);
            this.NetworkPublisher.SendString(json, "skeleton");
            return this.jointPoints;
        }
    }
}
