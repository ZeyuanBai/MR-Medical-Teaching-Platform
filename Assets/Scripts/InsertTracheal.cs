using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class InsertTracheal : MonoBehaviour
{
    public GameObject TrachealTip;
    public SkillTrainingManager skillTrainingManager;
    public TMP_Text Logs;

    public GameObject TestPrefab;

    [HideInInspector]
    public bool isTrachealInserted = false;
    [HideInInspector]
    public bool isInsertionOver = false;    
    [HideInInspector]
    public bool isPositionValid = true;
    [HideInInspector]
    public float stableHoldDuration = 0f;

    private float insertedSinceTime = -1f;





    void Start()
    {
        if (skillTrainingManager == null)
        {
            skillTrainingManager = GameObject.Find("SkillTrainingManager").GetComponent<SkillTrainingManager>();
        }
    }

    void Update()
    {
        if (isTrachealInserted)
        {
            if (insertedSinceTime < 0f)
            {
                insertedSinceTime = Time.time;
            }

            stableHoldDuration = Time.time - insertedSinceTime;
        }
        else
        {
            insertedSinceTime = -1f;
            stableHoldDuration = 0f;
        }
    }


    public IEnumerator WaitForCollision()
    {
        while (!isInsertionOver)
        {
            yield return null;
        }

        CalculateStep4Result();
        //Step4Test();

    }

    private void CalculateStep4Result()
    {
        if (isInsertionOver)
        {
            isPositionValid = isTrachealInserted;
            if (isTrachealInserted)
            {
                //skillTrainingManager.SetLogInfo("Tracheal Inserted");   
                //Logs.text += "\nTracheal Inserted";
            }
            else
            {
                //skillTrainingManager.SetLogInfo("Tracheal Not Inserted");
                //Logs.text += "\nTracheal Not Inserted";
            }
        }
    }

    private void Step4Test()
    {
        Instantiate(TestPrefab, TrachealTip.transform.position, Quaternion.identity);
    }

    public void ReplaceTrachealPosition()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "TrachealTip")
        {
            isTrachealInserted = true;
            if (insertedSinceTime < 0f)
            {
                insertedSinceTime = Time.time;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "TrachealTip")
        {
            isTrachealInserted = false;
            insertedSinceTime = -1f;
            stableHoldDuration = 0f;
        }
    }

    public void ResetStep4()
    {
        isTrachealInserted = false;
        isInsertionOver = false;
        isPositionValid = true;
        insertedSinceTime = -1f;
        stableHoldDuration = 0f;
        //skillTrainingManager.SetLogInfo("Step4 Reset Done");
        //Logs.text += "\nStep4 Reset";
    }
}
