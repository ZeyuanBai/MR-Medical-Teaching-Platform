using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CutSkin : MonoBehaviour
{
    public GameObject ScalpelTip;
    public SkillTrainingManager skillTrainingManager;
    public TMP_Text Logs;
    public GameObject NeckSkin;

    public GameObject StandardLine;
    public GameObject StandardA;
    public GameObject StandardB;

    public GameObject TestPrefab;

    [HideInInspector]
    public Vector3 StartPointLocalPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 EndPointLocalPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 MiddlePointLocalPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 StartPointPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 EndPointPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 MiddlePointPosition = Vector3.zero;

    [HideInInspector]
    public bool isCutOver = false;
    [HideInInspector]
    public bool isPositionValid = true;
    [HideInInspector]
    public bool isParallel = true;
    [HideInInspector]
    public float MeasuredLengthCm = 0f;
    [HideInInspector]
    public float MeasuredPathLengthCm = 0f;
    [HideInInspector]
    public float MeasuredAngleErrorDeg = 0f;
    [HideInInspector]
    public float MeasuredCenterOffsetCm = 0f;
    [HideInInspector]
    public int MeasuredSampleCount = 0;

    private float distance = 0f;
    private GameObject startPosition;
    private GameObject endPosition;
    private readonly List<Vector3> sampledWorldPoints = new List<Vector3>();
    private readonly List<Vector3> sampledLocalPoints = new List<Vector3>();

    private const float MinDistance = 1.5f;
    private const float MaxDistance = 3.5f;
    private const float ParallelThreshold = 0.75f;
    private const float SampleMinDistanceMeters = 0.0005f;
    private const int MeasurementSmoothingRadius = 1;

    private void Start()
    {
        if (ScalpelTip == null)
        {
            ScalpelTip = GameObject.Find("ScalpelTip");
        }
    }

    public IEnumerator WaitForCollisionAndCut()
    {
        while (!isCutOver)
        {
            yield return null;
        }

        CalculateStep2Result();
        if (NeckSkin != null && NeckSkin.activeSelf)
        {
            NeckSkin.SetActive(false);
        }
    }

    private void CalculateStep2Result()
    {
        if (!isCutOver)
        {
            return;
        }

        isParallel = true;
        isPositionValid = true;
        ResolveRepresentativeCutPoints();

        if (TrainingMeasurementUtility.AxisMatchScore(MeasuredAngleErrorDeg) < ParallelThreshold)
        {
            isParallel = false;
        }

        distance = MeasuredLengthCm;
        if (MeasuredLengthCm < MinDistance || MeasuredLengthCm > MaxDistance)
        {
            isPositionValid = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "ScalpelTip")
        {
            return;
        }

        if (skillTrainingManager.CurrentStep != SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue)
        {
            return;
        }

        BeginSampling(ScalpelTip.transform.position);
        isCutOver = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag != "ScalpelTip")
        {
            return;
        }

        if (skillTrainingManager.CurrentStep != SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue)
        {
            return;
        }

        AppendSample(ScalpelTip.transform.position);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "ScalpelTip")
        {
            return;
        }

        if (skillTrainingManager.CurrentStep != SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue)
        {
            return;
        }

        AppendSample(ScalpelTip.transform.position);
        EndPointPosition = ScalpelTip.transform.position;
        EndPointLocalPosition = TrainingMeasurementUtility.ToMeasurementPlaneLocalPosition(transform, ScalpelTip.transform.position);
        MiddlePointPosition = GetWorldPointFromMeasurementLocal((StartPointLocalPosition + EndPointLocalPosition) * 0.5f);
    }

    public void CutRetry()
    {
    }

    public void ResetStep2()
    {
        if (startPosition != null)
        {
            Destroy(startPosition);
        }

        if (endPosition != null)
        {
            Destroy(endPosition);
        }

        StartPointPosition = Vector3.zero;
        EndPointPosition = Vector3.zero;
        MiddlePointPosition = Vector3.zero;
        StartPointLocalPosition = Vector3.zero;
        EndPointLocalPosition = Vector3.zero;
        MiddlePointLocalPosition = Vector3.zero;
        MeasuredLengthCm = 0f;
        MeasuredPathLengthCm = 0f;
        MeasuredAngleErrorDeg = 0f;
        MeasuredCenterOffsetCm = 0f;
        MeasuredSampleCount = 0;
        sampledWorldPoints.Clear();
        sampledLocalPoints.Clear();
        isCutOver = false;
    }

    private void BeginSampling(Vector3 worldPoint)
    {
        sampledWorldPoints.Clear();
        sampledLocalPoints.Clear();
        AppendSample(worldPoint);
        StartPointPosition = worldPoint;
        StartPointLocalPosition = TrainingMeasurementUtility.ToMeasurementPlaneLocalPosition(transform, worldPoint);
    }

    private void AppendSample(Vector3 worldPoint)
    {
        Vector3 flattenedLocalPoint = TrainingMeasurementUtility.ToMeasurementPlaneLocalPosition(transform, worldPoint);

        if (sampledWorldPoints.Count > 0 &&
            Vector3.Distance(sampledLocalPoints[sampledLocalPoints.Count - 1], flattenedLocalPoint) < SampleMinDistanceMeters)
        {
            return;
        }

        sampledWorldPoints.Add(worldPoint);
        sampledLocalPoints.Add(flattenedLocalPoint);
        MeasuredSampleCount = sampledLocalPoints.Count;
    }

    private void ResolveRepresentativeCutPoints()
    {
        TrainingMeasurementUtility.CutPathMeasurementResult measurement =
            TrainingMeasurementUtility.MeasureCutPath(
                transform,
                StandardLine,
                StandardA,
                StandardB,
                sampledLocalPoints,
                AllowedAxes.Both,
                MeasurementSmoothingRadius,
                SampleMinDistanceMeters);

        if (!measurement.hasMeasurement)
        {
            MeasuredPathLengthCm = 0f;
            return;
        }

        StartPointLocalPosition = measurement.startLocalPosition;
        EndPointLocalPosition = measurement.endLocalPosition;
        MiddlePointLocalPosition = measurement.middleLocalPosition;
        StartPointPosition = GetWorldPointFromMeasurementLocal(StartPointLocalPosition);
        EndPointPosition = GetWorldPointFromMeasurementLocal(EndPointLocalPosition);
        MiddlePointPosition = GetWorldPointFromMeasurementLocal(MiddlePointLocalPosition);
        MeasuredLengthCm = measurement.lengthCentimeters;
        MeasuredPathLengthCm = measurement.pathLengthCentimeters;
        MeasuredAngleErrorDeg = measurement.angleErrorDegrees;
        MeasuredCenterOffsetCm = measurement.centerOffsetCentimeters;
        MeasuredSampleCount = measurement.pointCount;
    }

    private Vector3 GetWorldPointFromMeasurementLocal(Vector3 localPoint)
    {
        return transform.position + transform.rotation * localPoint;
    }
}
