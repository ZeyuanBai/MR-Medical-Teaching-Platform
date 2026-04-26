using System.Collections;
using System.Collections.Generic;
using PaintCore;
using PaintIn3D;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class SkillTrainingManager : MonoBehaviour
{
    [Header("UI Control")]
    public CentralUIController centralUIController;
    public TrainingReportManager trainingReportManager;

    [Header("Models")]
    public GameObject SkillTrainingModelTable;
    public GameObject Skin;
    public GameObject Bone;
    public GameObject Airway;
    public GameObject Brain;
    public GameObject Tissue;
    public GameObject SkinCut;
    public GameObject AirwayCut;
    public GameObject TissueCut;
    public GameObject NeckSkin;
    public GameObject Marker;
    public GameObject Scalpel;
    public GameObject Tracheal;
    public GameObject MarkerPosition;
    public GameObject ScalpelPosition;
    public GameObject TrachealTubePosition;
    public GameObject MarkerTipVisual;
    public GameObject MarkerBodyVisual;
    public GameObject ScalpelVisual;
    public GameObject TrachealVisual;

    [Header("UI Elements")]
    public TMP_Text Logs;
    public Button SkillTrainingStartBtn;
    public Button SkillTrainingResetBtn;
    public Button TransparentModeBtn;
    public TMP_Text TransparentModeBtnText;
    public Button ViewReportBtn;
    public Button DrawTextureClearBtn;
    public Button DetermineDrawPositionBtn;
    public Button CutSkinRetryBtn;
    public Button CutSkinOverBtn;
    public Button CutAirwayRetryBtn;
    public Button CutAirwayOverBtn;
    public Button TrachealReplaceBtn;
    public Button InsertionOverBtn;

    [Header("PaintIn3D")]
    public CwButtonClearAll CwClearAll;

    [Header("Step Status")]
    [HideInInspector]
    public TrainingStep CurrentStep = TrainingStep.Idle;
    [HideInInspector]
    public bool TransparentMode = false;
    [HideInInspector]
    public float TrainingTime = 0f;

    [Header("Step 1 Components")]
    public PositionDetermination positionDetermination;
    public GameObject DrawRegion;

    [Header("Step 2 Components")]
    public CutSkin cutSkin;
    public GameObject CutRegion;

    [Header("Step 3 Components")]
    public CutAirway cutAirway;
    public GameObject AirwayRegion;

    [Header("Step 4 Components")]
    public InsertTracheal insertTracheal;
    public GameObject TrachealRegion;

    [Header("Materials")]
    public Material SkinMat;
    public Material BoneMat;
    public Material AirwayMat;
    public Material BrainMat;
    public Material TissueMat;
    public Material TranspSkinMat;
    public Material TranspBoneMat;
    public Material TranspAirwayMat;
    public Material TranspBrainMat;
    public Material TransTissueMat;
    public Material HighLightMat;

    [HideInInspector]
    public bool isModelPositioned = false;
    [HideInInspector]
    public Vector3 ModelPosition = Vector3.zero;
    [HideInInspector]
    public Quaternion ModelRotation = Quaternion.identity;

    private readonly Queue<string> _logQueue = new Queue<string>();
    private const int MaxLogCount = 5;
    private bool _isTimerActive;
    private Vector3 _markerInitPos;
    private Vector3 _scalpelInitPos;
    private Vector3 _trachealTubeInitPos;

    private const string ScenarioName = "气管切开训练";
    private const string StartHint = "按下“开始”按键以开始练习";
    private const string ReportBlockedHint = "请完成练习后再查看报告";

    public enum TrainingStep
    {
        Idle = 0,
        Step1_PositionDetermination,
        Step2_CutSkinAndTissue,
        Step3_CutAirway,
        Step4_InsertTracheal
    }

    private void Start()
    {
        SkillTrainingStartBtn.onClick.AddListener(OnBtnPressedStartSkillTraining);
        SkillTrainingResetBtn.onClick.AddListener(OnBtnPressedResetSkillTraining);
        TransparentModeBtn.onClick.AddListener(OnBtnPressedTransparentMode);
        ViewReportBtn.onClick.AddListener(OnBtnPressedViewReport);

        DrawTextureClearBtn.onClick.AddListener(OnBtnPressedClearDrawTexture);
        DetermineDrawPositionBtn.onClick.AddListener(OnBtnPressedDetermineDrawPosition);
        CutSkinRetryBtn.onClick.AddListener(OnBtnPressedCutSkinRetry);
        CutSkinOverBtn.onClick.AddListener(OnBtnPressedCutSkinOver);
        CutAirwayRetryBtn.onClick.AddListener(OnBtnPressedCutAirwayRetry);
        CutAirwayOverBtn.onClick.AddListener(OnBtnPressedCutAirwayOver);
        TrachealReplaceBtn.onClick.AddListener(OnBtnPressedTrachealReplace);
        InsertionOverBtn.onClick.AddListener(OnBtnPressedInsertionOver);

        if (positionDetermination == null && DrawRegion != null)
        {
            positionDetermination = DrawRegion.GetComponent<PositionDetermination>();
        }

        ResetTraining();
    }

    private void Update()
    {
        UpdateModelPosition();

        if (MarkerPosition != null) _markerInitPos = MarkerPosition.transform.position;
        if (ScalpelPosition != null) _scalpelInitPos = ScalpelPosition.transform.position;
        if (TrachealTubePosition != null) _trachealTubeInitPos = TrachealTubePosition.transform.position;

        if (_isTimerActive)
        {
            TrainingTime += Time.deltaTime;
        }
    }

    private void UpdateModelPosition()
    {
        if (isModelPositioned && SkillTrainingModelTable != null)
        {
            SkillTrainingModelTable.transform.position = ModelPosition;
            SkillTrainingModelTable.transform.rotation = ModelRotation;
        }
    }

    private IEnumerator TrainingFlow()
    {
        while ((int)CurrentStep <= (int)TrainingStep.Step4_InsertTracheal)
        {
            switch (CurrentStep)
            {
                case TrainingStep.Step1_PositionDetermination:
                    SetLogInfo("步骤1：确定切割位置");
                    SetLogInfo("选择环状软骨下方第2-3气管软骨环为气管切开位置，用笔画出纵向切割位置。");
                    yield return StartCoroutine(ExecuteStep1());
                    break;

                case TrainingStep.Step2_CutSkinAndTissue:
                    SetLogInfo("步骤2：切开皮肤和组织");
                    SetLogInfo("沿颈部正中线做垂直切口，长度约3-4cm。");
                    yield return StartCoroutine(ExecuteStep2());
                    break;

                case TrainingStep.Step3_CutAirway:
                    SetLogInfo("步骤3：切开气管");
                    SetLogInfo("用手术刀水平横向切开气管前壁，切口长度约1-1.5cm。");
                    yield return StartCoroutine(ExecuteStep3());
                    break;

                case TrainingStep.Step4_InsertTracheal:
                    SetLogInfo("步骤4：插入气管套管");
                    SetLogInfo("将气管套管插入气管内，注意保持平行，避免损伤后壁。");
                    yield return StartCoroutine(ExecuteStep4());
                    ResetTraining();
                    yield break;
            }

            NextStep();
        }
    }

    private IEnumerator ExecuteStep1()
    {
        if (Marker != null)
        {
            Marker.SetActive(true);
            Marker.transform.position = _markerInitPos;
        }

        if (Scalpel != null) Scalpel.SetActive(false);
        if (Tracheal != null) Tracheal.SetActive(false);

        if (MarkerBodyVisual != null) MarkerBodyVisual.GetComponent<HighLightDisplay>().Add_Material();
        if (MarkerTipVisual != null) MarkerTipVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(positionDetermination.WaitForCollisionAndCalculate());
    }

    private IEnumerator ExecuteStep2()
    {
        if (Marker != null) Marker.SetActive(false);
        if (Scalpel != null)
        {
            Scalpel.SetActive(true);
            Scalpel.transform.position = _scalpelInitPos;
        }
        if (Tracheal != null) Tracheal.SetActive(false);

        if (ScalpelVisual != null) ScalpelVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(cutSkin.WaitForCollisionAndCut());

        if (Skin != null) Skin.SetActive(false);
        if (Tissue != null) Tissue.SetActive(false);
        if (SkinCut != null) SkinCut.SetActive(true);
        if (TissueCut != null) TissueCut.SetActive(true);
        if (NeckSkin != null) NeckSkin.SetActive(false);
        if (CwClearAll != null) CwClearAll.ClearAll();
    }

    private IEnumerator ExecuteStep3()
    {
        if (ScalpelVisual != null) ScalpelVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(cutAirway.WaitForCollisionAndCut());

        if (Airway != null) Airway.SetActive(false);
        if (AirwayCut != null) AirwayCut.SetActive(true);
    }

    private IEnumerator ExecuteStep4()
    {
        if (Marker != null) Marker.SetActive(false);
        if (Scalpel != null) Scalpel.SetActive(false);
        if (Tracheal != null)
        {
            Tracheal.SetActive(true);
            Tracheal.transform.position = _trachealTubeInitPos;
        }

        if (TrachealVisual != null) TrachealVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(insertTracheal.WaitForCollision());
    }

    public void OnBtnPressedStartSkillTraining()
    {
        if (CurrentStep != TrainingStep.Idle)
        {
            return;
        }

        CurrentStep = TrainingStep.Step1_PositionDetermination;

        if (trainingReportManager != null)
        {
            trainingReportManager.BeginSession(ScenarioName);
            trainingReportManager.isTrainingOver = false;
        }

        TrainingTime = 0f;
        _isTimerActive = true;
        StartCoroutine(TrainingFlow());
    }

    private void NextStep()
    {
        if (CurrentStep < TrainingStep.Step4_InsertTracheal)
        {
            CurrentStep++;
        }
    }

    public void ActiveMedicalInstruments(bool active)
    {
        if (Marker != null) Marker.SetActive(active);
        if (Scalpel != null) Scalpel.SetActive(active);
        if (Tracheal != null) Tracheal.SetActive(active);

        if (!active)
        {
            return;
        }

        if (Marker != null) Marker.transform.position = _markerInitPos;
        if (Scalpel != null) Scalpel.transform.position = _scalpelInitPos;
        if (Tracheal != null) Tracheal.transform.position = _trachealTubeInitPos;
    }

    public void OnBtnPressedClearDrawTexture()
    {
        if (CwClearAll != null) CwClearAll.ClearAll();
        positionDetermination.ResetStep1();
        positionDetermination.isPositionDetermined = false;
    }

    public void OnBtnPressedDetermineDrawPosition()
    {
        if (CurrentStep != TrainingStep.Step1_PositionDetermination)
        {
            return;
        }

        positionDetermination.isPositionDetermined = true;
        cutSkin.isCutOver = false;
        cutSkin.ResetStep2();
    }

    public void OnBtnPressedCutSkinRetry()
    {
        if (trainingReportManager != null)
        {
            trainingReportManager.RecordRetry("step2", "步骤二重新切开皮肤和组织。");
        }

        cutSkin.isCutOver = false;
        cutSkin.ResetStep2();
    }

    public void OnBtnPressedCutSkinOver()
    {
        if (CurrentStep != TrainingStep.Step2_CutSkinAndTissue)
        {
            return;
        }

        cutSkin.isCutOver = true;
        cutAirway.isCutOver = false;
        cutAirway.ResetStep3();
    }

    public void OnBtnPressedCutAirwayRetry()
    {
        if (trainingReportManager != null)
        {
            trainingReportManager.RecordRetry("step3", "步骤三重新切开气管。");
        }

        cutAirway.isCutOver = false;
        cutAirway.ResetStep3();
    }

    public void OnBtnPressedCutAirwayOver()
    {
        if (CurrentStep != TrainingStep.Step3_CutAirway)
        {
            return;
        }

        cutAirway.isCutOver = true;
        insertTracheal.isInsertionOver = false;
        insertTracheal.ResetStep4();
    }

    public void OnBtnPressedTrachealReplace()
    {
        if (trainingReportManager != null)
        {
            trainingReportManager.RecordRetry("step4", "步骤四重新放置气管套管。");
        }

        insertTracheal.isTrachealInserted = false;
        if (Tracheal != null)
        {
            Tracheal.transform.position = _trachealTubeInitPos;
        }
    }

    public void OnBtnPressedInsertionOver()
    {
        if (CurrentStep != TrainingStep.Step4_InsertTracheal)
        {
            return;
        }

        insertTracheal.isInsertionOver = true;
        _isTimerActive = false;

        if (trainingReportManager != null)
        {
            trainingReportManager.isTrainingOver = true;
            trainingReportManager.TrainingTime = TrainingTime;
            trainingReportManager.ShowReport();
        }
    }

    public void OnBtnPressedResetSkillTraining()
    {
        ResetTraining();
    }

    public void ResetTraining()
    {
        CurrentStep = TrainingStep.Idle;
        ActiveMedicalInstruments(true);

        if (NeckSkin != null) NeckSkin.SetActive(true);

        positionDetermination.ResetStep1();
        positionDetermination.isPositionDetermined = false;

        if (CwClearAll != null) CwClearAll.ClearAll();

        cutSkin.ResetStep2();
        cutSkin.isCutOver = false;
        cutAirway.ResetStep3();
        cutAirway.isCutOver = false;
        insertTracheal.ResetStep4();
        insertTracheal.isTrachealInserted = false;

        if (Skin != null) Skin.SetActive(true);
        if (Airway != null) Airway.SetActive(true);
        if (Tissue != null) Tissue.SetActive(true);
        if (SkinCut != null) SkinCut.SetActive(false);
        if (AirwayCut != null) AirwayCut.SetActive(false);
        if (TissueCut != null) TissueCut.SetActive(false);
        if (NeckSkin != null) NeckSkin.SetActive(true);

        TrainingTime = 0f;
        _isTimerActive = false;

        if (trainingReportManager != null)
        {
            trainingReportManager.TrainingTime = 0f;
            if (!trainingReportManager.isTrainingOver)
            {
                trainingReportManager.ResetReportState();
            }
        }

        SetLogInfo(StartHint);
    }

    public void OnBtnPressedTransparentMode()
    {
        TransparentMode = !TransparentMode;
        if (TransparentModeBtnText != null)
        {
            TransparentModeBtnText.text = TransparentMode
                ? "透明模式：开"
                : "透明模式：关";
        }

        SetTransparentModeMaterials(TransparentMode);
    }

    public void OnBtnPressedViewReport()
    {
        if (CurrentStep == TrainingStep.Idle)
        {
            centralUIController.OnBtnPressedViewReport();
        }
        else
        {
            SetLogInfo(ReportBlockedHint);
        }
    }

    public void SetLogInfo(string log)
    {
        if (_logQueue.Count >= MaxLogCount)
        {
            _logQueue.Dequeue();
        }

        _logQueue.Enqueue(log);

        if (Logs != null)
        {
            Logs.text = string.Join("\n", _logQueue.ToArray());
        }
    }

    public void Add_Material(GameObject obj, Material mat)
    {
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Add(mat);
        meshRenderer.materials = materialList.ToArray();
    }

    public void Remove_Material(GameObject obj, Material mat)
    {
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Remove(mat);
        meshRenderer.materials = materialList.ToArray();
    }

    private void SetTransparentModeMaterials(bool transparent)
    {
        if (!transparent)
        {
            AssignMaterial(Skin, SkinMat);
            AssignMaterial(SkinCut, SkinMat);
            AssignMaterial(NeckSkin, SkinMat);
            AssignMaterial(Bone, BoneMat);
            AssignMaterial(Airway, AirwayMat);
            AssignMaterial(AirwayCut, AirwayMat);
            AssignMaterial(Brain, BrainMat);
            AssignMaterial(Tissue, TissueMat);
            AssignMaterial(TissueCut, TissueMat);
            return;
        }

        AssignMaterial(Skin, TranspSkinMat);
        AssignMaterial(SkinCut, TranspSkinMat);
        AssignMaterial(NeckSkin, TranspSkinMat);
        AssignMaterial(Bone, TranspBoneMat);
        AssignMaterial(Airway, TranspAirwayMat);
        AssignMaterial(AirwayCut, TranspAirwayMat);
        AssignMaterial(Brain, TranspBrainMat);
        AssignMaterial(Tissue, TransTissueMat);
        AssignMaterial(TissueCut, TransTissueMat);
    }

    private void AssignMaterial(GameObject target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }
}
