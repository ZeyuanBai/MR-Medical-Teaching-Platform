using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;   

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
    public Vector3 StartPointLocalPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 EndPointLocalPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 MiddlePointLocalPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 StartPointPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 EndPointPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 MiddlePointPosition = new Vector3(0, 0, 0);

    [HideInInspector]
    public bool isCutOver = false;
    [HideInInspector]
    public bool isPositionValid = true;
    [HideInInspector]
    public bool isParallel = true;
    [HideInInspector]
    float distance = 0f;

    private GameObject startPosition;
    private GameObject endPosition;

    private float MinDistance = 1.5f;
    private float MaxDistance = 3.5f;
    private float ParallelThreshold = 0.75f;

    void Start()
    {
        if (ScalpelTip == null)
        {
            ScalpelTip = GameObject.Find("ScalpelTip");
        }

    }

    void Update()
    {

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
        //Step2Test();
    }

    private void CalculateStep2Result()
    {
        if (isCutOver)
        {
            isParallel = true;
            isPositionValid = true;
            Vector3 StandardLineA = transform.InverseTransformPoint(StandardA.transform.position);
            Vector3 StandardLineB = transform.InverseTransformPoint(StandardB.transform.position);
            //Vector3 StandardLineMiddle = (StandardLineA + StandardLineB) / 2;
            Vector3 StandardDir = (StandardLineB - StandardLineA).normalized;
            //Vector3 CutPositionA = new Vector3(StartPointPosition.x, 0, StartPointPosition.z);
            //Vector3 CutPositionB = new Vector3(EndPointPosition.x, 0, EndPointPosition.z);
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
            distance = 100f * Vector3.Distance(StartPointPosition, EndPointPosition); // 计算切割距离
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

    private void Step2Test()
    {
        startPosition = Instantiate(TestPrefab, transform);
        startPosition.transform.localPosition = StartPointPosition;
        startPosition.transform.localScale = new Vector3(2f, 2f, 2f);

        endPosition = Instantiate(TestPrefab, transform);
        endPosition.transform.localPosition = EndPointPosition;
        endPosition.transform.localScale = new Vector3(2f, 2f, 2f);
        skillTrainingManager.SetLogInfo("Step2 Test Done: " + StartPointPosition + " " + EndPointPosition);
        //Logs.text += "\nStep2 Test Done";
    }





    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "ScalpelTip")
        {
            if (skillTrainingManager.CurrentStep != SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue) return;
            StartPointPosition = ScalpelTip.transform.position;
            StartPointLocalPosition = transform.InverseTransformPoint(ScalpelTip.transform.position);
            StartPointPosition.y = 0;
            //skillTrainingManager.SetLogInfo("Start: " + StartPointPosition);
            //Logs.text += "\nLocal Start: " + StartPointPosition;
            isCutOver = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "ScalpelTip")
        {
            if (skillTrainingManager.CurrentStep != SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue) return;
            EndPointPosition = ScalpelTip.transform.position;   
            EndPointLocalPosition = transform.InverseTransformPoint(ScalpelTip.transform.position);
            EndPointPosition.y = 0;
            //skillTrainingManager.SetLogInfo("End: " + EndPointPosition);
            //Logs.text += "\nLocal End: " + EndPointPosition;
            MiddlePointPosition = (StartPointPosition + EndPointPosition) / 2;
        }
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
        StartPointPosition = new Vector3(0, 0, 0);
        EndPointPosition = new Vector3(0, 0, 0);
        MiddlePointPosition = new Vector3(0, 0, 0);
        isCutOver = false;
        //skillTrainingManager.SetLogInfo("Step2 Reset Done");
        //Logs.text += "\nStep2 Reset";
    }   
}
