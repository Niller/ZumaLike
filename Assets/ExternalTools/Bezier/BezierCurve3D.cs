﻿using System.Collections.Generic;
using UnityEngine;

namespace ExternalTools.Bezier
{
    public class BezierCurve3D : MonoBehaviour
    {
        // Serializable Fields
        [SerializeField] [Tooltip("The color used to render the curve")]
        private Color curveColor = Color.green;

        [SerializeField] [Tooltip("The color used to render the end point of the curve")]
        private Color endPointColor = Color.blue;

        [SerializeField] [HideInInspector] private List<BezierPoint3D> keyPoints = new List<BezierPoint3D>();

        [SerializeField] [Range(0f, 1f)] private float normalizedTime = 0.5f;

        [SerializeField] [Tooltip("The number of segments that the curve has. Affects calculations and performance")]
        private int sampling = 25;

        [SerializeField] [Tooltip("The color used to render the start point of the curve")]
        private Color startPointColor = Color.red;

        // Properties        
        public int Sampling
        {
            get => sampling;
            set => sampling = value;
        }

        public List<BezierPoint3D> KeyPoints => keyPoints;
        public int KeyPointsCount => KeyPoints.Count;

        // Public Methods

        /// <summary>
        ///     Adds a key point at the end of the curve
        /// </summary>
        /// <returns>The new key point</returns>
        public BezierPoint3D AddKeyPoint()
        {
            return AddKeyPointAt(KeyPointsCount);
        }

        /// <summary>
        ///     Add a key point at a specified index
        /// </summary>
        /// <param name="index">The index at which the key point will be added</param>
        /// <returns>The new key point</returns>
        public BezierPoint3D AddKeyPointAt(int index)
        {
            var newPoint = new GameObject("Point " + KeyPoints.Count, typeof(BezierPoint3D)).GetComponent<BezierPoint3D>();
            newPoint.Curve = this;
            var newPointTransform = newPoint.transform;
            newPointTransform.parent = transform;
            newPointTransform.localRotation = Quaternion.identity;

            if (KeyPointsCount == 0 || KeyPointsCount == 1)
            {
                newPoint.LocalPosition = Vector3.zero;
            }
            else
            {
                if (index == 0)
                    newPoint.Position = (KeyPoints[0].Position - KeyPoints[1].Position).normalized + KeyPoints[0].Position;
                else if (index == KeyPointsCount)
                    newPoint.Position = (KeyPoints[index - 1].Position - KeyPoints[index - 2].Position).normalized + KeyPoints[index - 1].Position;
                else
                    newPoint.Position = GetPointOnCubicCurve(0.5f, KeyPoints[index - 1], KeyPoints[index]);
            }

            KeyPoints.Insert(index, newPoint);

            return newPoint;
        }

        /// <summary>
        ///     Removes a key point at a specified index
        /// </summary>
        /// <param name="index">The index of the key point that will be removed</param>
        /// <returns>true - if the point was removed, false - otherwise</returns>
        public bool RemoveKeyPointAt(int index)
        {
            if (KeyPointsCount < 2) return false;

            var point = KeyPoints[index];
            KeyPoints.RemoveAt(index);

            Destroy(point.gameObject);

            return true;
        }

        /// <summary>
        ///     Evaluates a position along the curve at a specified normalized time [0, 1]
        /// </summary>
        /// <param name="time">The normalized length at which we want to get a position [0, 1]</param>
        /// <returns>The evaluated Vector3 position</returns>
        public Vector3 GetPoint(float time)
        {
            // The evaluated points is between these two points
            GetCubicSegment(time, out var startPoint, out var endPoint, out var timeRelativeToSegment);

            return GetPointOnCubicCurve(timeRelativeToSegment, startPoint, endPoint);
        }

        public Quaternion GetRotation(float time, Vector3 up)
        {
            GetCubicSegment(time, out var startPoint, out var endPoint, out var timeRelativeToSegment);

            return GetRotationOnCubicCurve(timeRelativeToSegment, up, startPoint, endPoint);
        }

        public Vector3 GetTangent(float time)
        {
            GetCubicSegment(time, out var startPoint, out var endPoint, out var timeRelativeToSegment);

            return GetTangentOnCubicCurve(timeRelativeToSegment, startPoint, endPoint);
        }

        public Vector3 GetBinormal(float time, Vector3 up)
        {
            BezierPoint3D startPoint;
            BezierPoint3D endPoint;
            float timeRelativeToSegment;

            GetCubicSegment(time, out startPoint, out endPoint, out timeRelativeToSegment);

            return GetBinormalOnCubicCurve(timeRelativeToSegment, up, startPoint, endPoint);
        }

        public Vector3 GetNormal(float time, Vector3 up)
        {
            GetCubicSegment(time, out var startPoint, out var endPoint, out var timeRelativeToSegment);

            return GetNormalOnCubicCurve(timeRelativeToSegment, up, startPoint, endPoint);
        }

        public float GetApproximateLength()
        {
            float length = 0;
            var subCurveSampling = Sampling / (KeyPointsCount - 1) + 1;
            for (var i = 0; i < KeyPointsCount - 1; i++) length += GetApproximateLengthOfCubicCurve(KeyPoints[i], KeyPoints[i + 1], subCurveSampling);

            return length;
        }

        public void GetCubicSegment(float time, out BezierPoint3D startPoint, out BezierPoint3D endPoint, out float timeRelativeToSegment)
        {
            startPoint = null;
            endPoint = null;
            timeRelativeToSegment = 0f;

            var subCurvePercent = 0f;
            var totalPercent = 0f;
            var approximateLength = GetApproximateLength();
            var subCurveSampling = Sampling / (KeyPointsCount - 1) + 1;

            for (var i = 0; i < KeyPointsCount - 1; i++)
            {
                subCurvePercent = GetApproximateLengthOfCubicCurve(KeyPoints[i], KeyPoints[i + 1], subCurveSampling) / approximateLength;
                if (subCurvePercent + totalPercent > time)
                {
                    startPoint = KeyPoints[i];
                    endPoint = KeyPoints[i + 1];

                    break;
                }

                totalPercent += subCurvePercent;
            }

            if (endPoint == null)
            {
                // If the evaluated point is very near to the end of the curve
                startPoint = KeyPoints[KeyPointsCount - 2];
                endPoint = KeyPoints[KeyPointsCount - 1];

                totalPercent -= subCurvePercent; // We remove the percentage of the last sub-curve
            }

            timeRelativeToSegment = (time - totalPercent) / subCurvePercent;
        }

        public static Vector3 GetPointOnCubicCurve(float time, BezierPoint3D startPoint, BezierPoint3D endPoint)
        {
            return GetPointOnCubicCurve(time, startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition);
        }

        public static Vector3 GetPointOnCubicCurve(float time, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
        {
            var t = time;
            var u = 1f - t;
            var t2 = t * t;
            var u2 = u * u;
            var u3 = u2 * u;
            var t3 = t2 * t;

            var result =
                u3 * startPosition +
                3f * u2 * t * startTangent +
                3f * u * t2 * endTangent +
                t3 * endPosition;

            return result;
        }

        public static Quaternion GetRotationOnCubicCurve(float time, Vector3 up, BezierPoint3D startPoint, BezierPoint3D endPoint)
        {
            return GetRotationOnCubicCurve(time, up, startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition);
        }

        public static Quaternion GetRotationOnCubicCurve(float time, Vector3 up, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent,
            Vector3 endTangent)
        {
            var tangent = GetTangentOnCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
            var normal = GetNormalOnCubicCurve(time, up, startPosition, endPosition, startTangent, endTangent);

            return Quaternion.LookRotation(tangent, normal);
        }

        public static Vector3 GetTangentOnCubicCurve(float time, BezierPoint3D startPoint, BezierPoint3D endPoint)
        {
            return GetTangentOnCubicCurve(time, startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition);
        }

        public static Vector3 GetTangentOnCubicCurve(float time, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
        {
            var t = time;
            var u = 1f - t;
            var u2 = u * u;
            var t2 = t * t;

            var tangent =
                -u2 * startPosition +
                u * (u - 2f * t) * startTangent -
                t * (t - 2f * u) * endTangent +
                t2 * endPosition;

            return tangent.normalized;
        }

        public static Vector3 GetBinormalOnCubicCurve(float time, Vector3 up, BezierPoint3D startPoint, BezierPoint3D endPoint)
        {
            return GetBinormalOnCubicCurve(time, up, startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition);
        }

        public static Vector3 GetBinormalOnCubicCurve(float time, Vector3 up, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent,
            Vector3 endTangent)
        {
            var tangent = GetTangentOnCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
            var binormal = Vector3.Cross(up, tangent);

            return binormal.normalized;
        }

        public static Vector3 GetNormalOnCubicCurve(float time, Vector3 up, BezierPoint3D startPoint, BezierPoint3D endPoint)
        {
            return GetNormalOnCubicCurve(time, up, startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition);
        }

        public static Vector3 GetNormalOnCubicCurve(float time, Vector3 up, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent,
            Vector3 endTangent)
        {
            var tangent = GetTangentOnCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
            var binormal = GetBinormalOnCubicCurve(time, up, startPosition, endPosition, startTangent, endTangent);
            var normal = Vector3.Cross(tangent, binormal);

            return normal.normalized;
        }

        public static float GetApproximateLengthOfCubicCurve(BezierPoint3D startPoint, BezierPoint3D endPoint, int sampling)
        {
            return GetApproximateLengthOfCubicCurve(startPoint.Position, endPoint.Position, startPoint.RightHandlePosition, endPoint.LeftHandlePosition,
                sampling);
        }

        public static float GetApproximateLengthOfCubicCurve(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int sampling)
        {
            var length = 0f;
            var fromPoint = GetPointOnCubicCurve(0f, startPosition, endPosition, startTangent, endTangent);

            for (var i = 0; i < sampling; i++)
            {
                var time = (i + 1) / (float) sampling;
                var toPoint = GetPointOnCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
                length += Vector3.Distance(fromPoint, toPoint);
                fromPoint = toPoint;
            }

            return length;
        }

        // Protected Methods

        protected virtual void OnDrawGizmos()
        {
            if (KeyPointsCount > 1)
            {
                // Draw the curve
                var fromPoint = GetPoint(0f);

                for (var i = 0; i < Sampling; i++)
                {
                    var time = (i + 1) / (float) Sampling;
                    var toPoint = GetPoint(time);

                    // Draw segment
                    Gizmos.color = curveColor;
                    Gizmos.DrawLine(fromPoint, toPoint);

                    fromPoint = toPoint;
                }

                // Draw the start and the end of the curve indicators
                Gizmos.color = startPointColor;
                Gizmos.DrawSphere(KeyPoints[0].Position, 0.05f);

                Gizmos.color = endPointColor;
                Gizmos.DrawSphere(KeyPoints[KeyPointsCount - 1].Position, 0.05f);

                // Draw the point at the normalized time
                var point = GetPoint(normalizedTime);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(point, 0.025f);

                var tangent = GetTangent(normalizedTime);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(point, point + tangent / 2f);

                var binormal = GetBinormal(normalizedTime, Vector3.up);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(point, point + binormal / 2f);

                var normal = GetNormal(normalizedTime, Vector3.up);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(point, point + normal / 2f);
            }
        }
    }
}