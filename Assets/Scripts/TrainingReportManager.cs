using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrainingReportManager : MonoBehaviour
{
    [Header("Training Controllers")]
    public SkillTrainingManager skillTrainingManager;
    public PositionDetermination positionDetermination;
    public CutSkin cutSkin;
    public CutAirway cutAirway;
    public InsertTracheal insertTracheal;

    [Header("UI Elements")]
    public TMP_Text ReportText;

    [HideInInspector]
    public bool isTrainingOver = false;
    [HideInInspector]
    public int Score;
    [HideInInspector]
    public float TrainingTime;

    void Start()
    {
        Score = 100;
    }

    
    void Update()
    {
        if (isTrainingOver)
        {
            ShowReport();
        }
    }

    public void ShowReport()
    {
        ReportText.text = "本次训练报告:\n";
        ReportText.text += "训练时间: " + TrainingTime.ToString("F2") + "秒\n";
        ReportText.text += "步骤一：确定切割位置\n";
        if (positionDetermination.isParallel && positionDetermination.isPositionValid)
        {
            ReportText.text += "位置有效\n";
        }
        else if (positionDetermination.isParallel && !positionDetermination.isPositionValid)
        {
            ReportText.text += "切割位置偏差较大\n";
        }
        else if (!positionDetermination.isParallel && positionDetermination.isPositionValid)
        {
            ReportText.text += "切割位置不竖直\n";
        }
        else if (!positionDetermination.isParallel && !positionDetermination.isPositionValid)
        {
            ReportText.text += "切割位置偏差较大且不竖直\n";
        }

        ReportText.text += "步骤二：切开皮肤与组织\n";
        if (cutSkin.isParallel && cutSkin.isPositionValid)
        {
            ReportText.text += "切割位置有效\n";
        }
        else if (cutSkin.isParallel && !cutSkin.isPositionValid)
        {
            ReportText.text += "切割位置偏差较大\n";
        }
        else if (!cutSkin.isParallel && cutSkin.isPositionValid)
        {
            ReportText.text += "切割位置不竖直\n";
        }
        else if (!cutSkin.isParallel && !cutSkin.isPositionValid)
        {
            ReportText.text += "切割位置偏差较大且不竖直\n";
        }

        ReportText.text += "步骤三：切开气管\n";
        if (cutAirway.isParallel && cutAirway.isPositionValid)
        {
            ReportText.text += "切割位置有效\n";
        }
        else if (cutAirway.isParallel && !cutAirway.isPositionValid)
        {
            ReportText.text += "切割位置偏差较大\n";
        }
        else if (!cutAirway.isParallel && cutAirway.isPositionValid)
        {
            ReportText.text += "切割位置不水平\n";
        }
        else if (!cutAirway.isParallel && !cutAirway.isPositionValid)
        {
            ReportText.text += "切割位置偏差较大且不水平\n";
        }

        ReportText.text += "步骤四：插入气管套管\n";
        if (insertTracheal.isPositionValid)
        {
            ReportText.text += "插入位置有效\n";
        }
        else
        {
            ReportText.text += "插入位置偏差较大\n";
        }
    }
}
