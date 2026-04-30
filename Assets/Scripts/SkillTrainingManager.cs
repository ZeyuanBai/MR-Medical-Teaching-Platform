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
    public SessionRecorder sessionRecorder;

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
    private Coroutine _trainingFlowCoroutine;
    private Vector3 _markerInitPos;
    private Vector3 _scalpelInitPos;
    private Vector3 _trachealTubeInitPos;
    private readonly float[] _stepStartTimes = new float[5];
    private readonly float[] _stepEndTimes = new float[5];
    private readonly float[] _stepDurations = new float[5];

    private const string ScenarioName = "气管切开训练";
    private const string StartHint = "按下“开始”按键以开始练习";
    private const string ReportBlockedHint = "请完成练习后再查看报告";
    private const string TrainingBusyHint = "训练正在进行中，请先完成当前步骤。";
    private const string StepMismatchHint = "当前不在对应训练步骤，请按流程完成当前操作。";

    private const string TransparentModeOnText = "透明模式：关";
    private const string TransparentModeOffText = "透明模式：开";

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
        BindButton(SkillTrainingStartBtn, OnBtnPressedStartSkillTraining, nameof(SkillTrainingStartBtn));
        BindButton(SkillTrainingResetBtn, OnBtnPressedResetSkillTraining, nameof(SkillTrainingResetBtn));
        BindButton(TransparentModeBtn, OnBtnPressedTransparentMode, nameof(TransparentModeBtn));
        BindButton(ViewReportBtn, OnBtnPressedViewReport, nameof(ViewReportBtn));

        BindButton(DrawTextureClearBtn, OnBtnPressedClearDrawTexture, nameof(DrawTextureClearBtn));
        BindButton(DetermineDrawPositionBtn, OnBtnPressedDetermineDrawPosition, nameof(DetermineDrawPositionBtn));
        BindButton(CutSkinRetryBtn, OnBtnPressedCutSkinRetry, nameof(CutSkinRetryBtn));
        BindButton(CutSkinOverBtn, OnBtnPressedCutSkinOver, nameof(CutSkinOverBtn));
        BindButton(CutAirwayRetryBtn, OnBtnPressedCutAirwayRetry, nameof(CutAirwayRetryBtn));
        BindButton(CutAirwayOverBtn, OnBtnPressedCutAirwayOver, nameof(CutAirwayOverBtn));
        BindButton(TrachealReplaceBtn, OnBtnPressedTrachealReplace, nameof(TrachealReplaceBtn));
        BindButton(InsertionOverBtn, OnBtnPressedInsertionOver, nameof(InsertionOverBtn));

        if (positionDetermination == null && DrawRegion != null)
        {
            positionDetermination = DrawRegion.GetComponent<PositionDetermination>();
        }

        if (cutSkin == null && CutRegion != null)
        {
            cutSkin = CutRegion.GetComponent<CutSkin>();
        }

        if (cutAirway == null && AirwayRegion != null)
        {
            cutAirway = AirwayRegion.GetComponent<CutAirway>();
        }

        if (insertTracheal == null && TrachealRegion != null)
        {
            insertTracheal = TrachealRegion.GetComponent<InsertTracheal>();
        }

        ResolveSessionRecorder();
        ResetStepTimingData();
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
            CaptureActiveStepPoseSample();
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
        while (CurrentStep != TrainingStep.Idle && (int)CurrentStep <= (int)TrainingStep.Step4_InsertTracheal)
        {
            TrainingStep activeStep = CurrentStep;
            BeginStepTracking(activeStep);

            switch (activeStep)
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
                    CompleteStepTracking(activeStep);
                    CurrentStep = TrainingStep.Idle;
                    _trainingFlowCoroutine = null;
                    yield break;
            }

            CompleteStepTracking(activeStep);
            NextStep();
        }

        _trainingFlowCoroutine = null;
    }

    private IEnumerator ExecuteStep1()
    {
        SetNeckSkinVisible(true);

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
        SetNeckSkinVisible(false);
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
            SetLogInfo(TrainingBusyHint);
            return;
        }

        ResetTraining();
        CurrentStep = TrainingStep.Step1_PositionDetermination;
        SetLogInfo("训练开始。");

        if (trainingReportManager != null)
        {
            trainingReportManager.BeginSession(ScenarioName);
            sessionRecorder = trainingReportManager.sessionRecorder;
            trainingReportManager.isTrainingOver = false;
        }
        else
        {
            ResolveSessionRecorder();
            if (sessionRecorder != null)
            {
                sessionRecorder.BeginSession(ScenarioName);
            }
        }

        TrainingTime = 0f;
        _isTimerActive = true;
        _trainingFlowCoroutine = StartCoroutine(TrainingFlow());
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
        if (positionDetermination == null)
        {
            Debug.LogWarning($"{nameof(SkillTrainingManager)} cannot clear step 1 because PositionDetermination is missing.", this);
            SetLogInfo("定位组件未找到，无法清除标记。");
            return;
        }

        positionDetermination.ResetStep1();
        positionDetermination.isPositionDetermined = false;
        SetLogInfo("已清除定位标记，请重新绘制切开位置。");
    }

    public void OnBtnPressedDetermineDrawPosition()
    {
        if (CurrentStep != TrainingStep.Step1_PositionDetermination)
        {
            SetLogInfo(StepMismatchHint);
            return;
        }

        if (positionDetermination != null)
        {
            positionDetermination.isPositionDetermined = true;
        }

        if (cutSkin != null)
        {
            cutSkin.isCutOver = false;
            cutSkin.ResetStep2();
        }

        SetLogInfo("步骤1已确认，准备进入步骤2：切开皮肤和组织。");
    }

    public void OnBtnPressedCutSkinRetry()
    {
        if (trainingReportManager != null)
        {
            trainingReportManager.RecordRetry("step2", "步骤二重新切开皮肤和组织。");
        }
        else if (sessionRecorder != null)
        {
            sessionRecorder.RecordRetry("step2", "步骤二重新切开皮肤和组织。");
        }

        if (cutSkin == null)
        {
            Debug.LogWarning($"{nameof(SkillTrainingManager)} cannot retry step 2 because CutSkin is missing.", this);
            SetLogInfo("皮肤切开组件未找到，无法重试步骤2。");
            return;
        }

        cutSkin.isCutOver = false;
        cutSkin.ResetStep2();
        SetNeckSkinVisible(true);
        SetLogInfo("已重置步骤2，请重新切开皮肤和组织。");
    }

    public void OnBtnPressedCutSkinOver()
    {
        if (CurrentStep != TrainingStep.Step2_CutSkinAndTissue)
        {
            SetLogInfo(StepMismatchHint);
            return;
        }

        if (cutSkin != null)
        {
            cutSkin.isCutOver = true;
        }

        if (cutAirway != null)
        {
            cutAirway.isCutOver = false;
            cutAirway.ResetStep3();
        }

        SetLogInfo("步骤2已完成，准备进入步骤3：切开气管。");
    }

    public void OnBtnPressedCutAirwayRetry()
    {
        if (trainingReportManager != null)
        {
            trainingReportManager.RecordRetry("step3", "步骤三重新切开气管。");
        }
        else if (sessionRecorder != null)
        {
            sessionRecorder.RecordRetry("step3", "步骤三重新切开气管。");
        }

        if (cutAirway == null)
        {
            Debug.LogWarning($"{nameof(SkillTrainingManager)} cannot retry step 3 because CutAirway is missing.", this);
            SetLogInfo("气管切开组件未找到，无法重试步骤3。");
            return;
        }

        cutAirway.isCutOver = false;
        cutAirway.ResetStep3();
        SetLogInfo("已重置步骤3，请重新切开气管。");
    }

    public void OnBtnPressedCutAirwayOver()
    {
        if (CurrentStep != TrainingStep.Step3_CutAirway)
        {
            SetLogInfo(StepMismatchHint);
            return;
        }

        if (cutAirway != null)
        {
            cutAirway.isCutOver = true;
        }

        if (insertTracheal != null)
        {
            insertTracheal.isInsertionOver = false;
            insertTracheal.ResetStep4();
        }

        SetLogInfo("步骤3已完成，准备进入步骤4：插入气管套管。");
    }

    public void OnBtnPressedTrachealReplace()
    {
        if (trainingReportManager != null)
        {
            trainingReportManager.RecordRetry("step4", "步骤四重新放置气管套管。");
        }
        else if (sessionRecorder != null)
        {
            sessionRecorder.RecordRetry("step4", "步骤四重新放置气管套管。");
        }

        if (insertTracheal != null)
        {
            insertTracheal.isTrachealInserted = false;
        }

        if (Tracheal != null)
        {
            Tracheal.transform.position = _trachealTubeInitPos;
        }

        SetLogInfo("已重置气管套管位置，请重新插入。");
    }

    public void OnBtnPressedInsertionOver()
    {
        if (CurrentStep != TrainingStep.Step4_InsertTracheal && CurrentStep != TrainingStep.Idle)
        {
            SetLogInfo(StepMismatchHint);
            return;
        }

        if (insertTracheal == null)
        {
            Debug.LogWarning($"{nameof(SkillTrainingManager)} cannot complete step 4 because InsertTracheal is missing.", this);
            SetLogInfo("插管组件未找到，无法完成步骤4。");
            return;
        }

        CurrentStep = TrainingStep.Step4_InsertTracheal;
        insertTracheal.CompleteInsertion();
        _isTimerActive = false;
        CompleteStepTracking(TrainingStep.Step4_InsertTracheal);
        CompleteSessionRecording();
        CurrentStep = TrainingStep.Idle;

        if (_trainingFlowCoroutine != null)
        {
            StopCoroutine(_trainingFlowCoroutine);
            _trainingFlowCoroutine = null;
        }

        if (trainingReportManager != null)
        {
            trainingReportManager.isTrainingOver = true;
            trainingReportManager.TrainingTime = TrainingTime;
            trainingReportManager.ShowReport();
        }

        SetLogInfo(insertTracheal.isPositionValid
            ? "步骤4已完成，训练报告已生成。"
            : "步骤4已结束，但未检测到有效插管，请查看训练报告。");
    }

    public void OnBtnPressedResetSkillTraining()
    {
        ResetTraining();
        SetLogInfo("训练已重置。");
    }

    public void ResetTraining()
    {
        if (_trainingFlowCoroutine != null)
        {
            StopCoroutine(_trainingFlowCoroutine);
            _trainingFlowCoroutine = null;
        }

        CurrentStep = TrainingStep.Idle;
        _logQueue.Clear();
        ActiveMedicalInstruments(true);
        TransparentMode = false;
        UpdateTransparentModeButtonText();

        if (positionDetermination != null)
        {
            positionDetermination.ResetStep1();
            positionDetermination.isPositionDetermined = false;
        }

        if (CwClearAll != null) CwClearAll.ClearAll();

        if (cutSkin != null)
        {
            cutSkin.ResetStep2();
            cutSkin.isCutOver = false;
        }

        if (cutAirway != null)
        {
            cutAirway.ResetStep3();
            cutAirway.isCutOver = false;
        }

        if (insertTracheal != null)
        {
            insertTracheal.ResetStep4();
            insertTracheal.isTrachealInserted = false;
        }

        if (Skin != null) Skin.SetActive(true);
        if (Airway != null) Airway.SetActive(true);
        if (Tissue != null) Tissue.SetActive(true);
        if (SkinCut != null) SkinCut.SetActive(false);
        if (AirwayCut != null) AirwayCut.SetActive(false);
        if (TissueCut != null) TissueCut.SetActive(false);
        SetTransparentModeMaterials(false);
        SetNeckSkinVisible(true);

        TrainingTime = 0f;
        _isTimerActive = false;
        ResetStepTimingData();

        if (trainingReportManager != null)
        {
            trainingReportManager.TrainingTime = 0f;
            trainingReportManager.ResetReportState();
            trainingReportManager.ResetSessionState();
        }
        else if (sessionRecorder != null)
        {
            sessionRecorder.ResetRecorder();
        }

        SetLogInfo(StartHint);
    }

    public float GetStepStartTime(TrainingStep step)
    {
        int index = (int)step;
        if (index <= 0 || index >= _stepStartTimes.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, _stepStartTimes[index]);
    }

    public float GetStepEndTime(TrainingStep step)
    {
        int index = (int)step;
        if (index <= 0 || index >= _stepEndTimes.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, _stepEndTimes[index]);
    }

    public float GetStepDuration(TrainingStep step)
    {
        int index = (int)step;
        if (index <= 0 || index >= _stepDurations.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, _stepDurations[index]);
    }

    public void OnBtnPressedTransparentMode()
    {
        TransparentMode = !TransparentMode;
        UpdateTransparentModeButtonText();

        SetTransparentModeMaterials(TransparentMode);
    }

    private void UpdateTransparentModeButtonText()
    {
        if (TransparentModeBtnText == null)
        {
            return;
        }

        TransparentModeBtnText.text = TransparentMode
            ? TransparentModeOnText
            : TransparentModeOffText;
    }

    public void OnBtnPressedViewReport()
    {
        if (CurrentStep == TrainingStep.Idle)
        {
            if (centralUIController != null)
            {
                centralUIController.OnBtnPressedViewReport();
                SetLogInfo("正在查看训练报告。");
            }
            else
            {
                SetLogInfo("界面控制器未找到，无法打开报告页面。");
            }
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

    private void SetNeckSkinVisible(bool visible)
    {
        SetObjectAndAncestorsActive(NeckSkin, visible);

        if (cutSkin != null && cutSkin.NeckSkin != null && cutSkin.NeckSkin != NeckSkin)
        {
            SetObjectAndAncestorsActive(cutSkin.NeckSkin, visible);
        }

        SetDirectRenderersEnabled(NeckSkin, visible);

        if (cutSkin != null && cutSkin.NeckSkin != null && cutSkin.NeckSkin != NeckSkin)
        {
            SetDirectRenderersEnabled(cutSkin.NeckSkin, visible);
        }
    }

    private void SetObjectAndAncestorsActive(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        Transform current = target.transform;
        Transform stopAt = SkillTrainingModelTable != null ? SkillTrainingModelTable.transform : null;
        while (current != null && current != stopAt)
        {
            current.gameObject.SetActive(active);
            current = current.parent;
        }
    }

    private void SetDirectRenderersEnabled(GameObject target, bool enabled)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = enabled;
        }
    }

    private void AssignMaterial(GameObject target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = material;
        }
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"{nameof(SkillTrainingManager)} missing button reference: {buttonName}", this);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void BeginStepTracking(TrainingStep step)
    {
        int index = (int)step;
        if (index <= 0 || index >= _stepStartTimes.Length)
        {
            return;
        }

        _stepStartTimes[index] = TrainingTime;
        _stepEndTimes[index] = -1f;
        _stepDurations[index] = 0f;
        if (sessionRecorder != null)
        {
            sessionRecorder.StartStep(GetStepId(step), GetStepName(step), index);
        }
    }

    private void CompleteStepTracking(TrainingStep step)
    {
        int index = (int)step;
        if (index <= 0 || index >= _stepEndTimes.Length || _stepStartTimes[index] < 0f)
        {
            return;
        }

        _stepEndTimes[index] = TrainingTime;
        _stepDurations[index] = Mathf.Max(0f, _stepEndTimes[index] - _stepStartTimes[index]);
        CompleteStepRecording(step, index);
    }

    private void ResetStepTimingData()
    {
        for (int i = 0; i < _stepStartTimes.Length; i++)
        {
            _stepStartTimes[i] = -1f;
            _stepEndTimes[i] = -1f;
            _stepDurations[i] = 0f;
        }
    }

    private void ResolveSessionRecorder()
    {
        if (sessionRecorder != null)
        {
            return;
        }

        if (trainingReportManager != null)
        {
            sessionRecorder = trainingReportManager.sessionRecorder;
        }

        if (sessionRecorder == null)
        {
            sessionRecorder = GetComponent<SessionRecorder>();
        }

        if (sessionRecorder == null)
        {
            sessionRecorder = FindObjectOfType<SessionRecorder>();
        }
    }

    private void CaptureActiveStepPoseSample()
    {
        if (sessionRecorder == null)
        {
            return;
        }

        switch (CurrentStep)
        {
            case TrainingStep.Step1_PositionDetermination:
                sessionRecorder.CapturePoseSample("step1", TrackedToolType.Marker, ResolveToolTransform(MarkerTipVisual, Marker), ResolveStepReference(DrawRegion));
                break;
            case TrainingStep.Step2_CutSkinAndTissue:
                sessionRecorder.CapturePoseSample("step2", TrackedToolType.Scalpel, ResolveToolTransform(ScalpelVisual, Scalpel), ResolveStepReference(CutRegion));
                break;
            case TrainingStep.Step3_CutAirway:
                sessionRecorder.CapturePoseSample("step3", TrackedToolType.Scalpel, ResolveToolTransform(ScalpelVisual, Scalpel), ResolveStepReference(AirwayRegion));
                break;
            case TrainingStep.Step4_InsertTracheal:
                sessionRecorder.CapturePoseSample("step4", TrackedToolType.TrachealTube, ResolveToolTransform(TrachealVisual, Tracheal), ResolveStepReference(TrachealRegion));
                break;
        }
    }

    private Transform ResolveToolTransform(GameObject preferredTip, GameObject fallbackRoot)
    {
        if (preferredTip != null)
        {
            return preferredTip.transform;
        }

        return fallbackRoot != null ? fallbackRoot.transform : null;
    }

    private Transform ResolveStepReference(GameObject stepRegion)
    {
        if (stepRegion != null)
        {
            return stepRegion.transform;
        }

        return SkillTrainingModelTable != null ? SkillTrainingModelTable.transform : transform;
    }

    private void CompleteStepRecording(TrainingStep step, int stepIndex)
    {
        if (sessionRecorder == null)
        {
            return;
        }

        string stepId = GetStepId(step);
        bool completed = IsStepCompleted(step);
        StepEvaluationResult result = new StepEvaluationResult
        {
            stepId = stepId,
            stepName = GetStepName(step),
            stepIndex = stepIndex,
            startTimeSeconds = GetStepStartTime(step),
            endTimeSeconds = GetStepEndTime(step),
            durationSeconds = GetStepDuration(step),
            completed = completed,
            completionRatio = completed ? 1f : 0f,
            status = completed ? TrainingOverallStatus.Passed : TrainingOverallStatus.Incomplete,
            isRequired = true
        };

        sessionRecorder.CompleteStep(result);
    }

    private void CompleteSessionRecording()
    {
        if (sessionRecorder != null)
        {
            sessionRecorder.CompleteSession();
        }
    }

    private bool IsStepCompleted(TrainingStep step)
    {
        switch (step)
        {
            case TrainingStep.Step1_PositionDetermination:
                return positionDetermination != null && positionDetermination.isPositionDetermined;
            case TrainingStep.Step2_CutSkinAndTissue:
                return cutSkin != null && cutSkin.isCutOver;
            case TrainingStep.Step3_CutAirway:
                return cutAirway != null && cutAirway.isCutOver;
            case TrainingStep.Step4_InsertTracheal:
                return insertTracheal != null && insertTracheal.isInsertionOver;
            default:
                return false;
        }
    }

    private string GetStepId(TrainingStep step)
    {
        switch (step)
        {
            case TrainingStep.Step1_PositionDetermination:
                return "step1";
            case TrainingStep.Step2_CutSkinAndTissue:
                return "step2";
            case TrainingStep.Step3_CutAirway:
                return "step3";
            case TrainingStep.Step4_InsertTracheal:
                return "step4";
            default:
                return "idle";
        }
    }

    private string GetStepName(TrainingStep step)
    {
        switch (step)
        {
            case TrainingStep.Step1_PositionDetermination:
                return "步骤一：确定切割位置";
            case TrainingStep.Step2_CutSkinAndTissue:
                return "步骤二：切开皮肤和组织";
            case TrainingStep.Step3_CutAirway:
                return "步骤三：切开气管";
            case TrainingStep.Step4_InsertTracheal:
                return "步骤四：插入气管套管";
            default:
                return "空闲";
        }
    }
}
