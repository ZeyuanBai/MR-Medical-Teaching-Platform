using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Button = UnityEngine.UI.Button;
using System.Runtime.CompilerServices;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;

public class AnatomyTeachingManager : MonoBehaviour
{
    [Header("Models and Components")]
    public GameObject AnatomyTeachingModelTable;
    public GameObject AnatomyScrollView;
    public HeadAndNeckAnatomyManager headAndNeckAnatomyManager;
    public CaseOfNeckMassAnatomyManager caseOfNeckMassAnatomyManager;
    public GameObject AnatomyTeachingControlPanel;
    public GameObject HeadAndNeckCTTeachingControlPanel;
    public GameObject CaseOfNeckMassCTTeachingControlPanel;


    [Header("UIelements")]
    public Button HeadAndNeckCTBtn;
    public Button CaseOfNeckMassCTBtn;

    public Button HANCTBackToMenuBtn;
    public Button CONMCTBackToMenuBtn;



    [Header("Anatomy Teaching Arguments")]

    [HideInInspector]
    public float RotationY;

    [HideInInspector]
    public bool isHeadAndNeckCTActive = false;
    [HideInInspector]
    public bool isCaseOfNeckMassCTActive = false;


    void Start()
    {
        HeadAndNeckCTBtn.onClick.AddListener(OnBtnPressedHeadAndNeckCT);
        CaseOfNeckMassCTBtn.onClick.AddListener(OnBtnPressedCaseOfNeckMassCT);

        HANCTBackToMenuBtn.onClick.AddListener(OnBtnPressedBackToMenu);
        CONMCTBackToMenuBtn.onClick.AddListener(OnBtnPressedBackToMenu);
    }


    void Update()
    {
        
    }

    public void OnBtnPressedBackToMenu()
    {
        ResetAllModels();
        HeadAndNeckCTTeachingControlPanel.SetActive(false);
        CaseOfNeckMassCTTeachingControlPanel.SetActive(false);
        AnatomyTeachingControlPanel.SetActive(true);
        isHeadAndNeckCTActive = false;
        isCaseOfNeckMassCTActive = false;
    }

    public void ResetAnatomyTeachingScene()
    {
        ResetAllModels();
        ResetTablePosition();
        HeadAndNeckCTTeachingControlPanel.SetActive(false);
        CaseOfNeckMassCTTeachingControlPanel.SetActive(false);
    }

    public void ResetTablePosition()
    {
        Vector3 cameraForwardXZ = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        float CameraRotationY = Camera.main.transform.eulerAngles.y;
        float TableOffset = 0.75f;
        Vector3 TablePosition = cameraForwardXZ.normalized * TableOffset + new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        TablePosition.y -= 0.2f;
        AnatomyTeachingModelTable.transform.position = TablePosition;
        AnatomyTeachingModelTable.transform.rotation = Quaternion.Euler(0, CameraRotationY, 0);
        RotationY = AnatomyTeachingModelTable.transform.eulerAngles.y;
    }

    public void ResetAllModels()
    {
        AnatomyTeachingModelTable.transform.localScale = new Vector3(1, 1, 1);
        headAndNeckAnatomyManager.ResetAllModels();
        caseOfNeckMassAnatomyManager.ResetAllModels();
    }

    public void OnBtnPressedHeadAndNeckCT()
    {
        ResetAllModels();
        AnatomyTeachingControlPanel.SetActive(false);
        HeadAndNeckCTTeachingControlPanel.SetActive(true);
        isHeadAndNeckCTActive = true;
    }

    public void OnBtnPressedCaseOfNeckMassCT()
    {
        ResetAllModels();
        AnatomyTeachingControlPanel.SetActive(false);
        CaseOfNeckMassCTTeachingControlPanel.SetActive(true);
        isCaseOfNeckMassCTActive = true;
    }
}
