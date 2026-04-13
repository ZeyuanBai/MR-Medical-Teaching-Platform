using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class CentralUIController : MonoBehaviour
{
    public GameObject CentralControlUI;

    public GameObject SystemIntroductionPanel;
    public Button SystemIntroductionBtn;    

    public GameObject SceneSettingPanel;
    public Button SceneSettingBtn;

    public GameObject AnatomyTeachingControlPanel;
    public Button AnatomyTeachingBtn;
    public GameObject AnatomyTeachingModelTable;
    public AnatomyTeachingManager anatomyTeachingManager;

    public GameObject SkillTrainingControlPanel;
    public Button SkillTrainingBtn;
    public GameObject SkillTrainingModelTable;
    public SkillTrainingManager skillTrainingManager;

    public GameObject ViewReportPanel;
    public Button ViewReportBtn;

    public Button ResetUIPositionBtn;

    public GameObject DirectionalLight;

    private float UIOffset = 1.5f;
    private float UIHeight = 1.0f;

    // 0: SystemIntroduction, 1: SceneSetting, 2: AnatomyTeaching, 3: SkillTraining, 4: ViewReport
    [HideInInspector]
    public int CurrentState = 0;

    void Start()
    {
        SystemIntroductionBtn.onClick.AddListener(OnBtnPressedSystemIntroduction);
        SceneSettingBtn.onClick.AddListener(OnBtnPressedSceneSetting);
        AnatomyTeachingBtn.onClick.AddListener(OnBtnPressedAnatomyTeaching);
        SkillTrainingBtn.onClick.AddListener(OnBtnPressedSkillTraining);
        ResetUIPositionBtn.onClick.AddListener(OnBtnPressedResetUIPosition);
        ViewReportBtn.onClick.AddListener(OnBtnPressedViewReport);

        CurrentState = 0;

        AnatomyTeachingModelTable.SetActive(false);


        switch (CurrentState)
        {
            case 0:
                SystemIntroductionPanel.SetActive(true);
                SceneSettingPanel.SetActive(false);
                AnatomyTeachingControlPanel.SetActive(false);
                SkillTrainingControlPanel.SetActive(false);
                SkillTrainingModelTable.SetActive(false);
                skillTrainingManager.ActiveMedicalInstruments(false);
                ViewReportPanel.SetActive(false);
                break;
            case 1:
                SystemIntroductionPanel.SetActive(false);
                SceneSettingPanel.SetActive(true);
                AnatomyTeachingControlPanel.SetActive(false);
                SkillTrainingControlPanel.SetActive(false);
                SkillTrainingModelTable.SetActive(false);
                skillTrainingManager.ActiveMedicalInstruments(false);
                ViewReportPanel.SetActive(false);
                break;
            case 2:
                SystemIntroductionPanel.SetActive(false);
                SceneSettingPanel.SetActive(false);
                AnatomyTeachingControlPanel.SetActive(true);
                anatomyTeachingManager.ResetAnatomyTeachingScene();
                SkillTrainingControlPanel.SetActive(false);
                SkillTrainingModelTable.SetActive(false);
                skillTrainingManager.ActiveMedicalInstruments(false);
                ViewReportPanel.SetActive(false);
                break;
            case 3:
                SystemIntroductionPanel.SetActive(false);
                SceneSettingPanel.SetActive(false);
                AnatomyTeachingControlPanel.SetActive(false);
                SkillTrainingControlPanel.SetActive(true);
                SkillTrainingModelTable.SetActive(true);
                skillTrainingManager.ActiveMedicalInstruments(true);
                skillTrainingManager.ResetTraining();
                ViewReportPanel.SetActive(false);
                break;
            case 4:
                SystemIntroductionPanel.SetActive(false);
                SceneSettingPanel.SetActive(false);
                AnatomyTeachingControlPanel.SetActive(false);
                SkillTrainingControlPanel.SetActive(false);
                SkillTrainingModelTable.SetActive(false);
                skillTrainingManager.ActiveMedicalInstruments(false);
                ViewReportPanel.SetActive(true);
                break;
            default:
                Debug.LogError("Invalid state.");
                break;
        }

        ResetUIPosition();
        anatomyTeachingManager.ResetAnatomyTeachingScene();

    }

    void Update()
    {
        
    }

    public void OnBtnPressedSystemIntroduction()
    {
        if (CurrentState == 0)
        {
            return;
        }
        else
        {
            CurrentState = 0;
            SystemIntroductionPanel.SetActive(true);
            SceneSettingPanel.SetActive(false);
            AnatomyTeachingControlPanel.SetActive(false);
            anatomyTeachingManager.ResetAnatomyTeachingScene();
            SkillTrainingControlPanel.SetActive(false);
            SkillTrainingModelTable.SetActive(false);
            skillTrainingManager.ActiveMedicalInstruments(false);
            ViewReportPanel.SetActive(false);
        }
    }

    public void OnBtnPressedSceneSetting()
    {
        if (CurrentState == 1)
        {
            return;
        }
        else
        {
            CurrentState = 1;
            SystemIntroductionPanel.SetActive(false);
            SceneSettingPanel.SetActive(true);
            AnatomyTeachingControlPanel.SetActive(false);
            anatomyTeachingManager.ResetAnatomyTeachingScene();
            SkillTrainingControlPanel.SetActive(false);
            SkillTrainingModelTable.SetActive(false);
            skillTrainingManager.ActiveMedicalInstruments(false);
            ViewReportPanel.SetActive(false);
        }
    }

    public void OnBtnPressedAnatomyTeaching()
    {
        if (CurrentState == 2)
        {
            return;
        }
        else
        {
            CurrentState = 2;
            SystemIntroductionPanel.SetActive(false);
            SceneSettingPanel.SetActive(false);
            AnatomyTeachingControlPanel.SetActive(true);
            anatomyTeachingManager.ResetAnatomyTeachingScene();
            SkillTrainingControlPanel.SetActive(false);
            SkillTrainingModelTable.SetActive(false);
            skillTrainingManager.ActiveMedicalInstruments(false);
            ViewReportPanel.SetActive(false);
        }
    }

    public void OnBtnPressedSkillTraining()
    {
        if (CurrentState == 3)
        {
            return;
        }
        else
        {
            CurrentState = 3;
            SystemIntroductionPanel.SetActive(false);
            SceneSettingPanel.SetActive(false);
            AnatomyTeachingControlPanel.SetActive(false);
            anatomyTeachingManager.ResetAnatomyTeachingScene();
            SkillTrainingControlPanel.SetActive(true);
            SkillTrainingModelTable.SetActive(true);
            skillTrainingManager.ActiveMedicalInstruments(true);
            skillTrainingManager.ResetTraining();
            ViewReportPanel.SetActive(false);
        }
    }

    public void OnBtnPressedViewReport()
    {
        if (CurrentState == 4)
        {
            return;
        }
        else
        {
            CurrentState = 4;
            SystemIntroductionPanel.SetActive(false);
            SceneSettingPanel.SetActive(false);
            AnatomyTeachingControlPanel.SetActive(false);
            anatomyTeachingManager.ResetAnatomyTeachingScene();
            SkillTrainingControlPanel.SetActive(false);
            SkillTrainingModelTable.SetActive(false);
            skillTrainingManager.ActiveMedicalInstruments(false);
            ViewReportPanel.SetActive(true);
        }
    }

    public void OnBtnPressedResetUIPosition()
    {
        ResetUIPosition();
        //anatomyTeachingManager.ResetTablePosition();
    }

    public void ResetUIPosition()
    {
        anatomyTeachingManager.ResetTablePosition();
        //ResetSceneLight();
        Vector3 cameraForwardXZ = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        //Vector3 UIPosition = cameraForwardXZ.normalized * UIOffset + new Vector3(Camera.main.transform.position.x, UIHeight, Camera.main.transform.position.z);
        Vector3 UIPosition = cameraForwardXZ.normalized * UIOffset + new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        CentralControlUI.transform.position = UIPosition;
        CentralControlUI.transform.LookAt(new Vector3(CentralControlUI.transform.position.x * 2 - Camera.main.transform.position.x,
                                                      CentralControlUI.transform.position.y * 2 - Camera.main.transform.position.y,
                                                      CentralControlUI.transform.position.z * 2 - Camera.main.transform.position.z),
                                                      Vector3.up);
    }

    public void ResetSceneLight()
    {
        Quaternion cameraRotation = Camera.main.transform.rotation;
        cameraRotation.x = 0;
        cameraRotation.z = 0;
        Quaternion Offset = Quaternion.Euler(120, 0, 0);
        DirectionalLight.transform.rotation = cameraRotation * Offset;
    }
}
