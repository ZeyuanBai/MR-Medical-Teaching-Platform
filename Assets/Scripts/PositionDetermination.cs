using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PositionDetermination : MonoBehaviour
{
    public GameObject MarkerTip;
    public SkillTrainingManager skillTrainingManager;
    public TMP_Text Logs;

    public GameObject StandardLine;
    public GameObject StandardA;
    public GameObject StandardB;
    public GameObject CutLine;
    public GameObject CutA;
    public GameObject CutB;

    [HideInInspector]
    public Vector3 StartPointPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 EndPointPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 MiddlePointPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 StartPointLocalPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 EndPointLocalPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 MiddlePointLocalPosition = Vector3.zero;

    [HideInInspector]
    public bool isPositionDetermined = false;
    [HideInInspector]
    public bool isPositionValid = true;
    [HideInInspector]
    public bool isParallel = true;
    [HideInInspector]
    public float MeasuredLengthCm = 0f;
    [HideInInspector]
    public float MeasuredCenterOffsetCm = 0f;
    [HideInInspector]
    public float MeasuredAngleErrorDeg = 0f;
    [HideInInspector]
    public int MeasuredSampleCount = 0;

    private GameObject startPosition;
    private GameObject endPosition;
    private readonly List<Vector3> sampledWorldPoints = new List<Vector3>();
    private readonly List<Vector3> sampledLocalPoints = new List<Vector3>();

    private const float MinDistance = 1.5f;
    private const float MaxDistance = 5.5f;
    private const float ParallelThreshold = 0.75f;
    private const float SampleMinDistanceMeters = 0.0005f;
    private const int MeasurementSmoothingRadius = 1;

    [Header("test")]
    public GameObject TestPrefab;
    public Material Red;
    public Material Green;

    public TMP_Text test_log;

    private void Start()
    {
        if (skillTrainingManager == null)
        {
            GameObject managerObject = GameObject.Find("SkillTrainingManager");
            if (managerObject != null)
            {
                skillTrainingManager = managerObject.GetComponent<SkillTrainingManager>();
            }
        }
    }

    private void Update()
    {
        //test_log.text = "MarkerTip: " + MarkerTip.transform.position;
    }

    public IEnumerator WaitForCollisionAndCalculate()
    {
        while (!isPositionDetermined)
        {
            yield return null;
        }

        CalculateStep1Result();
        //Step1Test();
    }

    private void CalculateStep1Result()
    {
        if (!isPositionDetermined)
        {
            return;
        }

        isParallel = true;
        isPositionValid = true;
        ResolveRepresentativeMarkerPoints();

        MeasuredAngleErrorDeg = TrainingMeasurementUtility.DirectionErrorDegrees(
            transform,
            StandardA,
            StandardB,
            StartPointLocalPosition,
            EndPointLocalPosition,
            AllowedAxes.Both);

        if (TrainingMeasurementUtility.AxisMatchScore(MeasuredAngleErrorDeg) < ParallelThreshold)
        {
            isParallel = false;
        }

        MeasuredLengthCm = TrainingMeasurementUtility.DistanceCentimeters(
            StartPointLocalPosition,
            EndPointLocalPosition);
        MeasuredCenterOffsetCm = TrainingMeasurementUtility.CenterOffsetMeters(
            transform,
            StandardLine,
            StandardA,
            StandardB,
            StartPointLocalPosition,
            EndPointLocalPosition) * TrainingMeasurementUtility.MetersToCentimeters;

        if (MeasuredLengthCm < MinDistance || MeasuredLengthCm > MaxDistance)
        {
            isPositionValid = false;
        }
    }

    private void Step1Test()
    {
        startPosition = Instantiate(TestPrefab, transform);
        startPosition.transform.localPosition = StartPointLocalPosition;
        startPosition.GetComponent<Renderer>().material = Red;

        endPosition = Instantiate(TestPrefab, transform);
        endPosition.transform.localPosition = EndPointLocalPosition;
        endPosition.GetComponent<Renderer>().material = Green;
        //skillTrainingManager.SetLogInfo("Step1 Test Done: " + StartPointPosition + " " + EndPointPosition + " " + MiddlePointPosition);
        //Logs.text += "\nStep1 Test Done";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsMarkerTip(other) || !IsStep1Active())
        {
            return;
        }

        BeginSampling(MarkerTip.transform.position);
        isPositionDetermined = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsMarkerTip(other) || !IsStep1Active())
        {
            return;
        }

        AppendSample(MarkerTip.transform.position);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsMarkerTip(other) || !IsStep1Active())
        {
            return;
        }

        AppendSample(MarkerTip.transform.position);
        EndPointPosition = MarkerTip.transform.position;
        EndPointLocalPosition = TrainingMeasurementUtility.ToMeasurementPlaneLocalPosition(transform, MarkerTip.transform.position);
        MiddlePointLocalPosition = (StartPointLocalPosition + EndPointLocalPosition) * 0.5f;
        MiddlePointPosition = GetWorldPointFromMeasurementLocal(MiddlePointLocalPosition);
    }

    public void ResetStep1()
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
        MeasuredCenterOffsetCm = 0f;
        MeasuredAngleErrorDeg = 0f;
        MeasuredSampleCount = 0;
        sampledWorldPoints.Clear();
        sampledLocalPoints.Clear();
        isPositionDetermined = false;
        isPositionValid = true;
        isParallel = true;
        //skillTrainingManager.SetLogInfo("Step1 Reset Done");
        //Logs.text += "\nStep1 Reset";
    }

    private bool IsMarkerTip(Collider other)
    {
        return other != null && other.gameObject.CompareTag("MarkerTip") && MarkerTip != null;
    }

    private bool IsStep1Active()
    {
        return skillTrainingManager == null ||
            skillTrainingManager.CurrentStep == SkillTrainingManager.TrainingStep.Step1_PositionDetermination;
    }

    private void BeginSampling(Vector3 worldPoint)
    {
        sampledWorldPoints.Clear();
        sampledLocalPoints.Clear();
        AppendSample(worldPoint);
        StartPointPosition = worldPoint;
        StartPointLocalPosition = TrainingMeasurementUtility.ToMeasurementPlaneLocalPosition(transform, worldPoint);
        EndPointPosition = worldPoint;
        EndPointLocalPosition = StartPointLocalPosition;
        MiddlePointPosition = worldPoint;
        MiddlePointLocalPosition = StartPointLocalPosition;
    }

    private void AppendSample(Vector3 worldPoint)
    {
        Vector3 localPoint = TrainingMeasurementUtility.ToMeasurementPlaneLocalPosition(transform, worldPoint);

        if (sampledWorldPoints.Count > 0 &&
            Vector3.Distance(sampledLocalPoints[sampledLocalPoints.Count - 1], localPoint) < SampleMinDistanceMeters)
        {
            return;
        }

        sampledWorldPoints.Add(worldPoint);
        sampledLocalPoints.Add(localPoint);
        MeasuredSampleCount = sampledLocalPoints.Count;
    }

    private void ResolveRepresentativeMarkerPoints()
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
            return;
        }

        StartPointLocalPosition = measurement.startLocalPosition;
        EndPointLocalPosition = measurement.endLocalPosition;
        MiddlePointLocalPosition = measurement.middleLocalPosition;
        StartPointPosition = GetWorldPointFromMeasurementLocal(StartPointLocalPosition);
        EndPointPosition = GetWorldPointFromMeasurementLocal(EndPointLocalPosition);
        MiddlePointPosition = GetWorldPointFromMeasurementLocal(MiddlePointLocalPosition);
        MeasuredLengthCm = measurement.lengthCentimeters;
        MeasuredCenterOffsetCm = measurement.centerOffsetCentimeters;
        MeasuredAngleErrorDeg = measurement.angleErrorDegrees;
        MeasuredSampleCount = measurement.pointCount;
    }

    private Vector3 GetWorldPointFromMeasurementLocal(Vector3 localPoint)
    {
        return transform.position + transform.rotation * localPoint;
    }
}
