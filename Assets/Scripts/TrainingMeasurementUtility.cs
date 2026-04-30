using System.Collections.Generic;
using UnityEngine;

public static class TrainingMeasurementUtility
{
    public const float MetersToCentimeters = 100f;
    private const float DirectionEpsilon = 0.000001f;

    public struct CutPathMeasurementResult
    {
        public bool hasMeasurement;
        public Vector3 startLocalPosition;
        public Vector3 endLocalPosition;
        public Vector3 middleLocalPosition;
        public float lengthCentimeters;
        public float pathLengthCentimeters;
        public float angleErrorDegrees;
        public float centerOffsetCentimeters;
        public float lateralOffsetCentimeters;
        public float cranioCaudalOffsetCentimeters;
        public float axisMatchScore;
        public int pointCount;
    }

    public static Vector3 ToUnscaledLocalPosition(Transform referenceFrame, Vector3 worldPosition)
    {
        if (referenceFrame == null)
        {
            return worldPosition;
        }

        return Quaternion.Inverse(referenceFrame.rotation) * (worldPosition - referenceFrame.position);
    }

    public static Vector3 FlattenToMeasurementPlane(Vector3 localPosition)
    {
        localPosition.y = 0f;
        return localPosition;
    }

    public static Vector3 ToMeasurementPlaneLocalPosition(Transform referenceFrame, Vector3 worldPosition)
    {
        return FlattenToMeasurementPlane(ToUnscaledLocalPosition(referenceFrame, worldPosition));
    }

    public static float DistanceMeters(Vector3 startLocalPosition, Vector3 endLocalPosition)
    {
        return Vector3.Distance(startLocalPosition, endLocalPosition);
    }

    public static float DistanceCentimeters(Vector3 startLocalPosition, Vector3 endLocalPosition)
    {
        return DistanceMeters(startLocalPosition, endLocalPosition) * MetersToCentimeters;
    }

    public static Vector3 GetReferenceCenterLocal(Transform referenceFrame, GameObject standardLine, GameObject standardA, GameObject standardB)
    {
        if (standardA != null && standardB != null)
        {
            return (ToMeasurementPlaneLocalPosition(referenceFrame, standardA.transform.position)
                + ToMeasurementPlaneLocalPosition(referenceFrame, standardB.transform.position)) * 0.5f;
        }

        if (standardLine != null)
        {
            return ToMeasurementPlaneLocalPosition(referenceFrame, standardLine.transform.position);
        }

        return Vector3.zero;
    }

    public static float CenterOffsetMeters(
        Transform referenceFrame,
        GameObject standardLine,
        GameObject standardA,
        GameObject standardB,
        Vector3 startLocalPosition,
        Vector3 endLocalPosition)
    {
        Vector3 referenceCenter = GetReferenceCenterLocal(referenceFrame, standardLine, standardA, standardB);
        Vector3 cutCenter = (startLocalPosition + endLocalPosition) * 0.5f;
        return Vector3.Distance(referenceCenter, cutCenter);
    }

    public static float DirectionErrorDegrees(
        Transform referenceFrame,
        GameObject standardA,
        GameObject standardB,
        Vector3 startLocalPosition,
        Vector3 endLocalPosition,
        AllowedAxes allowedAxes)
    {
        if (standardA == null || standardB == null)
        {
            return 0f;
        }

        Vector3 standardDirection = (ToMeasurementPlaneLocalPosition(referenceFrame, standardB.transform.position)
            - ToMeasurementPlaneLocalPosition(referenceFrame, standardA.transform.position)).normalized;
        Vector3 cutDirection = (endLocalPosition - startLocalPosition).normalized;

        if (standardDirection == Vector3.zero || cutDirection == Vector3.zero)
        {
            return 90f;
        }

        Vector3 horizontalReference = Vector3.Cross(Vector3.up, standardDirection).normalized;
        if (horizontalReference == Vector3.zero)
        {
            horizontalReference = Vector3.right;
        }

        float verticalError = UndirectedAngleDegrees(cutDirection, standardDirection);
        float horizontalError = UndirectedAngleDegrees(cutDirection, horizontalReference);
        return Mathf.Abs(ScoreEngine.SelectDirectionError(horizontalError, verticalError, allowedAxes));
    }

    public static float AxisMatchScore(float angleErrorDegrees)
    {
        return Mathf.Clamp01(1f - Mathf.Abs(angleErrorDegrees) / 90f);
    }

    public static float PathLengthCentimeters(IList<Vector3> localPoints)
    {
        if (localPoints == null || localPoints.Count < 2)
        {
            return 0f;
        }

        float pathLengthMeters = 0f;
        for (int i = 1; i < localPoints.Count; i++)
        {
            pathLengthMeters += Vector3.Distance(localPoints[i - 1], localPoints[i]);
        }

        return pathLengthMeters * MetersToCentimeters;
    }

    public static CutPathMeasurementResult MeasureCutPath(
        Transform referenceFrame,
        GameObject standardLine,
        GameObject standardA,
        GameObject standardB,
        IList<Vector3> localPoints,
        AllowedAxes allowedAxes,
        int smoothingRadius = 1,
        float minPointSpacingMeters = 0.0005f,
        float trimFraction = 0.05f)
    {
        CutPathMeasurementResult result = new CutPathMeasurementResult();
        List<Vector3> filteredPoints = BuildFilteredLocalPath(localPoints, smoothingRadius, minPointSpacingMeters);
        result.pointCount = filteredPoints.Count;

        if (filteredPoints.Count == 0)
        {
            return result;
        }

        if (filteredPoints.Count == 1)
        {
            result.hasMeasurement = true;
            result.startLocalPosition = filteredPoints[0];
            result.endLocalPosition = filteredPoints[0];
            result.middleLocalPosition = filteredPoints[0];
            result.axisMatchScore = 0f;
            return result;
        }

        Vector3 direction = EstimateDominantPlaneDirection(filteredPoints);
        if (direction.sqrMagnitude <= DirectionEpsilon)
        {
            return result;
        }

        Vector3 observedDirection = filteredPoints[filteredPoints.Count - 1] - filteredPoints[0];
        if (observedDirection.sqrMagnitude > DirectionEpsilon && Vector3.Dot(direction, observedDirection) < 0f)
        {
            direction = -direction;
        }

        Vector3 center = ComputeMean(filteredPoints);
        List<float> projections = new List<float>(filteredPoints.Count);
        for (int i = 0; i < filteredPoints.Count; i++)
        {
            projections.Add(Vector3.Dot(filteredPoints[i] - center, direction));
        }

        projections.Sort();
        int trimCount = Mathf.Clamp(Mathf.FloorToInt(projections.Count * Mathf.Clamp01(trimFraction)), 0, Mathf.Max(0, (projections.Count - 2) / 2));
        float minProjection = projections[trimCount];
        float maxProjection = projections[projections.Count - 1 - trimCount];

        result.hasMeasurement = true;
        result.startLocalPosition = center + direction * minProjection;
        result.endLocalPosition = center + direction * maxProjection;
        result.middleLocalPosition = (result.startLocalPosition + result.endLocalPosition) * 0.5f;
        result.lengthCentimeters = Mathf.Max(0f, maxProjection - minProjection) * MetersToCentimeters;
        result.pathLengthCentimeters = PathLengthCentimeters(filteredPoints);
        result.angleErrorDegrees = DirectionErrorDegrees(
            referenceFrame,
            standardA,
            standardB,
            result.startLocalPosition,
            result.endLocalPosition,
            allowedAxes);
        result.centerOffsetCentimeters = CenterOffsetMeters(
            referenceFrame,
            standardLine,
            standardA,
            standardB,
            result.startLocalPosition,
            result.endLocalPosition) * MetersToCentimeters;
        CalculateAxisOffsetsCentimeters(
            referenceFrame,
            standardLine,
            standardA,
            standardB,
            result.middleLocalPosition,
            out result.lateralOffsetCentimeters,
            out result.cranioCaudalOffsetCentimeters);
        result.axisMatchScore = AxisMatchScore(result.angleErrorDegrees);
        return result;
    }

    public static void CalculateAxisOffsetsCentimeters(
        Transform referenceFrame,
        GameObject standardLine,
        GameObject standardA,
        GameObject standardB,
        Vector3 measuredCenterLocalPosition,
        out float lateralOffsetCentimeters,
        out float cranioCaudalOffsetCentimeters)
    {
        lateralOffsetCentimeters = 0f;
        cranioCaudalOffsetCentimeters = 0f;

        Vector3 referenceCenter = GetReferenceCenterLocal(referenceFrame, standardLine, standardA, standardB);
        Vector3 offset = FlattenToMeasurementPlane(measuredCenterLocalPosition - referenceCenter);

        Vector3 axialReference = Vector3.forward;
        if (standardA != null && standardB != null)
        {
            axialReference = (ToMeasurementPlaneLocalPosition(referenceFrame, standardB.transform.position)
                - ToMeasurementPlaneLocalPosition(referenceFrame, standardA.transform.position)).normalized;
        }

        if (axialReference.sqrMagnitude <= DirectionEpsilon)
        {
            axialReference = Vector3.forward;
        }

        Vector3 lateralReference = Vector3.Cross(Vector3.up, axialReference).normalized;
        if (lateralReference.sqrMagnitude <= DirectionEpsilon)
        {
            lateralReference = Vector3.right;
        }

        lateralOffsetCentimeters = Vector3.Dot(offset, lateralReference) * MetersToCentimeters;
        cranioCaudalOffsetCentimeters = Vector3.Dot(offset, axialReference) * MetersToCentimeters;
    }

    private static List<Vector3> BuildFilteredLocalPath(IList<Vector3> localPoints, int smoothingRadius, float minPointSpacingMeters)
    {
        List<Vector3> smoothed = SmoothLocalPath(localPoints, smoothingRadius);
        if (smoothed.Count < 2 || minPointSpacingMeters <= 0f)
        {
            return smoothed;
        }

        List<Vector3> filtered = new List<Vector3>();
        filtered.Add(smoothed[0]);

        for (int i = 1; i < smoothed.Count; i++)
        {
            Vector3 point = smoothed[i];
            if (Vector3.Distance(filtered[filtered.Count - 1], point) >= minPointSpacingMeters)
            {
                filtered.Add(point);
            }
        }

        if (filtered.Count == 1 && smoothed.Count > 1)
        {
            filtered.Add(smoothed[smoothed.Count - 1]);
        }

        return filtered;
    }

    private static List<Vector3> SmoothLocalPath(IList<Vector3> localPoints, int smoothingRadius)
    {
        List<Vector3> result = new List<Vector3>();
        if (localPoints == null)
        {
            return result;
        }

        int radius = Mathf.Max(0, smoothingRadius);
        for (int i = 0; i < localPoints.Count; i++)
        {
            int start = Mathf.Max(0, i - radius);
            int end = Mathf.Min(localPoints.Count - 1, i + radius);
            Vector3 sum = Vector3.zero;
            int count = 0;

            for (int j = start; j <= end; j++)
            {
                sum += FlattenToMeasurementPlane(localPoints[j]);
                count++;
            }

            result.Add(count > 0 ? sum / count : FlattenToMeasurementPlane(localPoints[i]));
        }

        return result;
    }

    private static Vector3 EstimateDominantPlaneDirection(IList<Vector3> localPoints)
    {
        Vector3 fallbackDirection = FindFarthestPairDirection(localPoints);
        if (localPoints == null || localPoints.Count < 3)
        {
            return fallbackDirection.normalized;
        }

        Vector3 mean = ComputeMean(localPoints);
        float xx = 0f;
        float xz = 0f;
        float zz = 0f;

        for (int i = 0; i < localPoints.Count; i++)
        {
            Vector3 delta = localPoints[i] - mean;
            xx += delta.x * delta.x;
            xz += delta.x * delta.z;
            zz += delta.z * delta.z;
        }

        if (Mathf.Abs(xx) + Mathf.Abs(xz) + Mathf.Abs(zz) <= DirectionEpsilon)
        {
            return fallbackDirection.normalized;
        }

        float angle = 0.5f * Mathf.Atan2(2f * xz, xx - zz);
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
        return direction.sqrMagnitude > DirectionEpsilon ? direction : fallbackDirection.normalized;
    }

    private static Vector3 FindFarthestPairDirection(IList<Vector3> localPoints)
    {
        if (localPoints == null || localPoints.Count < 2)
        {
            return Vector3.zero;
        }

        int startIndex = 0;
        int endIndex = localPoints.Count - 1;
        float maxDistanceSqr = -1f;

        for (int i = 0; i < localPoints.Count; i++)
        {
            for (int j = i + 1; j < localPoints.Count; j++)
            {
                float distanceSqr = (localPoints[j] - localPoints[i]).sqrMagnitude;
                if (distanceSqr > maxDistanceSqr)
                {
                    maxDistanceSqr = distanceSqr;
                    startIndex = i;
                    endIndex = j;
                }
            }
        }

        return FlattenToMeasurementPlane(localPoints[endIndex] - localPoints[startIndex]);
    }

    private static Vector3 ComputeMean(IList<Vector3> localPoints)
    {
        if (localPoints == null || localPoints.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < localPoints.Count; i++)
        {
            sum += FlattenToMeasurementPlane(localPoints[i]);
        }

        return sum / localPoints.Count;
    }

    private static float UndirectedAngleDegrees(Vector3 a, Vector3 b)
    {
        if (a.sqrMagnitude <= DirectionEpsilon || b.sqrMagnitude <= DirectionEpsilon)
        {
            return 90f;
        }

        float dot = Mathf.Clamp(Mathf.Abs(Vector3.Dot(a.normalized, b.normalized)), 0f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }
}
