using System.Collections;
using Microsoft.Kinect;
using System;

namespace Kinect2Server
{
    public class KinectJointFilter
    {
        public KinectJointFilter(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
        {
            m_pFilteredJoints = new CameraSpacePoint[Body.JointCount];
            m_pHistory = new FilterDoubleExponentialData[Body.JointCount];
            for (int i = 0; i < Body.JointCount; i++)
            {
                m_pHistory[i] = new FilterDoubleExponentialData();
            }

            Init(fSmoothing, fCorrection, fPrediction, fJitterRadius, fMaxDeviationRadius);
        }

        public struct TRANSFORM_SMOOTH_PARAMETERS
        {
            public float fSmoothing;             // [0..1], lower values closer to raw data
            public float fCorrection;            // [0..1], lower values slower to correct towards the raw data
            public float fPrediction;            // [0..n], the number of frames to predict into the future
            public float fJitterRadius;          // The radius in meters for jitter reduction
            public float fMaxDeviationRadius;    // The maximum radius in meters that filtered positions are allowed to deviate from raw data
        }

        public class FilterDoubleExponentialData
        {
            public CameraSpacePoint m_vRawPosition;
            public CameraSpacePoint m_vFilteredPosition;
            public CameraSpacePoint m_vTrend;
            public int m_dwFrameCount;
        }

        // Holt Double Exponential Smoothing filter
        CameraSpacePoint[] m_pFilteredJoints;
        FilterDoubleExponentialData[] m_pHistory;
        float m_fSmoothing;
        float m_fCorrection;
        float m_fPrediction;
        float m_fJitterRadius;
        float m_fMaxDeviationRadius;

        public KinectJointFilter()
        {
            m_pFilteredJoints = new CameraSpacePoint[Body.JointCount];
            m_pHistory = new FilterDoubleExponentialData[Body.JointCount];
            for (int i = 0; i < Body.JointCount; i++)
            {
                m_pHistory[i] = new FilterDoubleExponentialData();
            }

            Init();
        }

        ~KinectJointFilter()
        {
            Shutdown();
        }

        public void Init(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
        {
            Reset(fSmoothing, fCorrection, fPrediction, fJitterRadius, fMaxDeviationRadius);
        }

        public void Shutdown()
        {
        }

        public void Reset(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
        {
            if (m_pFilteredJoints == null || m_pHistory == null)
            {
                return;
            }

            m_fMaxDeviationRadius = fMaxDeviationRadius; // Size of the max prediction radius Can snap back to noisy data when too high
            m_fSmoothing = fSmoothing;                   // How much smothing will occur.  Will lag when too high
            m_fCorrection = fCorrection;                 // How much to correct back from prediction.  Can make things springy
            m_fPrediction = fPrediction;                 // Amount of prediction into the future to use. Can over shoot when too high
            m_fJitterRadius = fJitterRadius;             // Size of the radius where jitter is removed. Can do too much smoothing when too high

            for (int i = 0; i < Body.JointCount; i++)
            {
                m_pFilteredJoints[i].X = 0.0f;
                m_pFilteredJoints[i].Y = 0.0f;
                m_pFilteredJoints[i].Z = 0.0f;

                m_pHistory[i].m_vFilteredPosition.X = 0.0f;
                m_pHistory[i].m_vFilteredPosition.Y = 0.0f;
                m_pHistory[i].m_vFilteredPosition.Z = 0.0f;

                m_pHistory[i].m_vRawPosition.X = 0.0f;
                m_pHistory[i].m_vRawPosition.Y = 0.0f;
                m_pHistory[i].m_vRawPosition.Z = 0.0f;

                m_pHistory[i].m_vTrend.X = 0.0f;
                m_pHistory[i].m_vTrend.Y = 0.0f;
                m_pHistory[i].m_vTrend.Z = 0.0f;

                m_pHistory[i].m_dwFrameCount = 0;
            }
        }

        //--------------------------------------------------------------------------------------
        // Implementation of a Holt Double Exponential Smoothing filter. The double exponential
        // smooths the curve and predicts.  There is also noise jitter removal. And maximum
        // prediction bounds.  The paramaters are commented in the init function.
        //--------------------------------------------------------------------------------------
        public void UpdateFilter(Body pBody)
        {
            if (pBody == null)
            {
                return;
            }

            // Check for divide by zero. Use an epsilon of a 10th of a millimeter
            m_fJitterRadius = Math.Max(0.0001f, m_fJitterRadius);

            TRANSFORM_SMOOTH_PARAMETERS SmoothingParams;

            for (JointType jt = JointType.SpineBase; jt <= JointType.ThumbRight; jt++)
            {
                SmoothingParams.fSmoothing = m_fSmoothing;
                SmoothingParams.fCorrection = m_fCorrection;
                SmoothingParams.fPrediction = m_fPrediction;
                SmoothingParams.fJitterRadius = m_fJitterRadius;
                SmoothingParams.fMaxDeviationRadius = m_fMaxDeviationRadius;

                // If inferred, we smooth a bit more by using a bigger jitter radius
                Microsoft.Kinect.Joint joint = pBody.Joints[jt];
                if (joint.TrackingState == TrackingState.Inferred)
                {
                    SmoothingParams.fJitterRadius *= 2.0f;
                    SmoothingParams.fMaxDeviationRadius *= 2.0f;
                }

                UpdateJoint(pBody, jt, SmoothingParams);
            }
        }

        //--------------------------------------------------------------------------------------
        // if joint is 0 it is not valid.
        //--------------------------------------------------------------------------------------
        bool JointPositionIsValid(CameraSpacePoint vJointPosition)
        {
            return (vJointPosition.X != 0.0f ||
                        vJointPosition.Y != 0.0f ||
                        vJointPosition.Z != 0.0f);
        }

        public CameraSpacePoint[] GetFilteredJoints()
        {
            return m_pFilteredJoints;
        }

        CameraSpacePoint CSVectorSet(float x, float y, float z)
        {
            CameraSpacePoint point = new CameraSpacePoint();

            point.X = x;
            point.Y = y;
            point.Z = z;

            return point;
        }

        CameraSpacePoint CSVectorZero()
        {
            CameraSpacePoint point = new CameraSpacePoint();

            point.X = 0.0f;
            point.Y = 0.0f;
            point.Z = 0.0f;

            return point;
        }

        CameraSpacePoint CSVectorAdd(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            CameraSpacePoint sum = new CameraSpacePoint();

            sum.X = p1.X + p2.X;
            sum.Y = p1.Y + p2.Y;
            sum.Z = p1.Z + p2.Z;

            return sum;
        }

        CameraSpacePoint CSVectorScale(CameraSpacePoint p, float scale)
        {
            CameraSpacePoint point = new CameraSpacePoint();

            point.X = p.X * scale;
            point.Y = p.Y * scale;
            point.Z = p.Z * scale;

            return point;
        }

        CameraSpacePoint CSVectorSubtract(CameraSpacePoint p1, CameraSpacePoint p2)
        {
            CameraSpacePoint diff = new CameraSpacePoint();

            diff.X = p1.X - p2.X;
            diff.Y = p1.Y - p2.Y;
            diff.Z = p1.Z - p2.Z;

            return diff;
        }

        float CSVectorLength(CameraSpacePoint p)
        {
            return Convert.ToSingle(Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z));
        }

        void UpdateJoint(Body body, JointType jt, TRANSFORM_SMOOTH_PARAMETERS smoothingParams)
        {
            CameraSpacePoint vPrevRawPosition;
            CameraSpacePoint vPrevFilteredPosition;
            CameraSpacePoint vPrevTrend;
            CameraSpacePoint vRawPosition;
            CameraSpacePoint vFilteredPosition;
            CameraSpacePoint vPredictedPosition;
            CameraSpacePoint vDiff;
            CameraSpacePoint vTrend;
            float fDiff;
            bool bJointIsValid;

            Microsoft.Kinect.Joint joint = body.Joints[jt];

            vRawPosition = joint.Position;
            vPrevFilteredPosition = m_pHistory[(int)jt].m_vFilteredPosition;
            vPrevTrend = m_pHistory[(int)jt].m_vTrend;
            vPrevRawPosition = m_pHistory[(int)jt].m_vRawPosition;
            bJointIsValid = JointPositionIsValid(vRawPosition);

            // If joint is invalid, reset the filter
            if (!bJointIsValid)
            {
                m_pHistory[(int)jt].m_dwFrameCount = 0;
            }

            // Initial start values
            if (m_pHistory[(int)jt].m_dwFrameCount == 0)
            {
                vFilteredPosition = vRawPosition;
                vTrend = CSVectorZero();
                m_pHistory[(int)jt].m_dwFrameCount++;
            }
            else if (m_pHistory[(int)jt].m_dwFrameCount == 1)
            {
                vFilteredPosition = CSVectorScale(CSVectorAdd(vRawPosition, vPrevRawPosition), 0.5f);
                vDiff = CSVectorSubtract(vFilteredPosition, vPrevFilteredPosition);
                vTrend = CSVectorAdd(CSVectorScale(vDiff, smoothingParams.fCorrection), CSVectorScale(vPrevTrend, 1.0f - smoothingParams.fCorrection));
                m_pHistory[(int)jt].m_dwFrameCount++;
            }
            else
            {
                // First apply jitter filter
                vDiff = CSVectorSubtract(vRawPosition, vPrevFilteredPosition);
                fDiff = CSVectorLength(vDiff);

                if (fDiff <= smoothingParams.fJitterRadius)
                {
                    vFilteredPosition = CSVectorAdd(CSVectorScale(vRawPosition, fDiff / smoothingParams.fJitterRadius),
                        CSVectorScale(vPrevFilteredPosition, 1.0f - fDiff / smoothingParams.fJitterRadius));
                }
                else
                {
                    vFilteredPosition = vRawPosition;
                }

                // Now the double exponential smoothing filter
                vFilteredPosition = CSVectorAdd(CSVectorScale(vFilteredPosition, 1.0f - smoothingParams.fSmoothing),
                    CSVectorScale(CSVectorAdd(vPrevFilteredPosition, vPrevTrend), smoothingParams.fSmoothing));


                vDiff = CSVectorSubtract(vFilteredPosition, vPrevFilteredPosition);
                vTrend = CSVectorAdd(CSVectorScale(vDiff, smoothingParams.fCorrection), CSVectorScale(vPrevTrend, 1.0f - smoothingParams.fCorrection));
            }

            // Predict into the future to reduce latency
            vPredictedPosition = CSVectorAdd(vFilteredPosition, CSVectorScale(vTrend, smoothingParams.fPrediction));

            // Check that we are not too far away from raw data
            vDiff = CSVectorSubtract(vPredictedPosition, vRawPosition);
            fDiff = CSVectorLength(vDiff);

            if (fDiff > smoothingParams.fMaxDeviationRadius)
            {
                vPredictedPosition = CSVectorAdd(CSVectorScale(vPredictedPosition, smoothingParams.fMaxDeviationRadius / fDiff),
                    CSVectorScale(vRawPosition, 1.0f - smoothingParams.fMaxDeviationRadius / fDiff));
            }

            // Save the data from this frame
            m_pHistory[(int)jt].m_vRawPosition = vRawPosition;
            m_pHistory[(int)jt].m_vFilteredPosition = vFilteredPosition;
            m_pHistory[(int)jt].m_vTrend = vTrend;

            // Output the data
            m_pFilteredJoints[(int)jt] = vPredictedPosition;
        }
    }
}