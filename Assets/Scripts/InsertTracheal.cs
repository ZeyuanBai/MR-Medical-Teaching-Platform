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





    void Start()
    {
        if (skillTrainingManager == null)
        {
            skillTrainingManager = GameObject.Find("SkillTrainingManager").GetComponent<SkillTrainingManager>();
        }
    }

    void Update()
    {
        
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
            isPositionValid = true;
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
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "TrachealTip")
        {
            isTrachealInserted = false;
        }
    }

    public void ResetStep4()
    {
        isTrachealInserted = false;
        isInsertionOver = false;
        //skillTrainingManager.SetLogInfo("Step4 Reset Done");
        //Logs.text += "\nStep4 Reset";
    }
}
