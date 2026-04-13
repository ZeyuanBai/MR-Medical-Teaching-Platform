using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    public Vector3 StartPointPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 EndPointPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 MiddlePointPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 StartPointLocalPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 EndPointLocalPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 MiddlePointLocalPosition = new Vector3(0, 0, 0);

    [HideInInspector]
    public bool isPositionDetermined = false;
    [HideInInspector]
    public bool isPositionValid = true;
    [HideInInspector]
    public bool isParallel = true;
    [HideInInspector]
    float distance = 0f;

    private GameObject startPosition;
    private GameObject endPosition;

    private float MinDistance = 1.5f;
    private float MaxDistance = 5.5f;
    private float ParallelThreshold = 0.75f;

    [Header("test")]
    public GameObject TestPrefab;
    public Material Red;
    public Material Green;

    public TMP_Text test_log;
    void Start()
    {
        if (skillTrainingManager == null)
        {
            skillTrainingManager = GameObject.Find("SkillTrainingManager").GetComponent<SkillTrainingManager>();
        }
    }

    void Update()
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
        if (isPositionDetermined)
        {
            isParallel = true;
            isPositionValid = true;
            Vector3 StandardLineA = transform.InverseTransformPoint(StandardA.transform.position);
            Vector3 StandardLineB = transform.InverseTransformPoint(StandardB.transform.position);
            //Vector3 StandardLineMiddle = (StandardLineA + StandardLineB) / 2;
            Vector3 StandardDir = (StandardLineB - StandardLineA).normalized;
            //Vector3 CutPositionA = new Vector3(StartPointLocalPosition.x, 0, StartPointLocalPosition.z);
            //Vector3 CutPositionB = new Vector3(EndPointLocalPosition.x, 0, EndPointLocalPosition.z);
            //Vector3 CutPositionMiddle = new Vector3(MiddlePointPosition.x, 0, MiddlePointPosition.z);
            //Vector3 CutDir = CutPositionB - CutPositionA;
            Vector3 CutDir = (StartPointLocalPosition - EndPointLocalPosition).normalized; // 计算切割方向
            float dot = Math.Abs(Vector3.Dot(StandardDir, CutDir));
            if (dot < ParallelThreshold)
            {
                isParallel = false;
                //skillTrainingManager.SetLogInfo("位置错误：不竖直。" + dot);
                //Logs.text += "\nPosition Invalid: Not Parallel. " + dot;
            }
            else
            {
                //skillTrainingManager.SetLogInfo("位置正确：竖直。" + dot);
                //Logs.text += "\nPosition Valid: Parallel. " + dot;
            }
            //float distance = Vector3.Distance(StandardLineMiddle, CutPositionMiddle);
            //float distance = CutDir.magnitude;
            //float distance = 100f * Vector3.Distance(StartPointPosition, EndPointPosition); // 计算切割距离
            Vector3 StardardMiddle = StandardLine.transform.position;
            Vector3 CutMiddle = (StartPointPosition + EndPointPosition) / 2;
            distance = 100f * Vector3.Distance(StardardMiddle, CutMiddle); // 计算切割距离
            if (distance < MinDistance || distance > MaxDistance)
            {
                isPositionValid = false;
                //skillTrainingManager.SetLogInfo("位置错误：距离不在范围内。" + distance + "cm");
                //Logs.text += "\nPosition Invalid: Distance out of range. " + distance;
            }
            else
            {
                //skillTrainingManager.SetLogInfo("位置正确：距离在范围内。" + distance + "cm");
                //Logs.text += "\nPosition Valid: Distance in range. " + distance;
            }
        }
    }

    private void Step1Test()
    {
        startPosition = Instantiate(TestPrefab, transform);
        startPosition.transform.localPosition = StartPointPosition;
        startPosition.GetComponent<Renderer>().material = Red;


        endPosition = Instantiate(TestPrefab, transform);
        endPosition.transform.localPosition = EndPointPosition;
        endPosition.GetComponent<Renderer>().material = Green;
        //skillTrainingManager.SetLogInfo("Step1 Test Done: " + StartPointPosition + " " + EndPointPosition + " " + MiddlePointPosition);
        //Logs.text += "\nStep1 Test Done";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "MarkerTip")
        {
            StartPointPosition = MarkerTip.transform.position;
            StartPointLocalPosition = transform.InverseTransformPoint(MarkerTip.transform.position);
            StartPointLocalPosition.y = 0;
            //skillTrainingManager.SetLogInfo(" Start: " + StartPointPosition);
            //Logs.text += "\nLocal Start: " + StartPointPosition;
            isPositionDetermined = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "MarkerTip")
        {
            EndPointPosition = MarkerTip.transform.position;
            EndPointLocalPosition = transform.InverseTransformPoint(MarkerTip.transform.position);
            EndPointLocalPosition.y = 0;
            //skillTrainingManager.SetLogInfo("End: " + EndPointPosition);
            //Logs.text += "\nLocal End: " + EndPointPosition;
            MiddlePointPosition = (StartPointPosition + EndPointPosition) / 2;
        }
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
        StartPointPosition = new Vector3(0, 0, 0);
        EndPointPosition = new Vector3(0, 0, 0);
        MiddlePointPosition = new Vector3(0, 0, 0);
        isPositionDetermined = false;
        //skillTrainingManager.SetLogInfo("Step1 Reset Done");
        //Logs.text += "\nStep1 Reset";
    }
}
