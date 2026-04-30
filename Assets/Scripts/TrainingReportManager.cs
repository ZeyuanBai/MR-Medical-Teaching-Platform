using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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

    [Header("Evaluation")]
    public RubricConfig rubricConfig;
    public SessionRecorder sessionRecorder;
    public bool exportResearchDataOnTrainingEnd = true;

    [Header("UI Elements")]
    public TMP_Text ReportText;

    [HideInInspector]
    public bool isTrainingOver = false;
    [HideInInspector]
    public int Score;
    [HideInInspector]
    public float TrainingTime;

    public TrainingEvaluationReport LatestReport { get; private set; }
    public string[] LatestExportedPaths { get; private set; }

    private bool _reportGenerated;

    private const string ScenarioName = "气管切开训练";
    private const string ReportTitle = "本次训练报告";
    private const string ReportNotReady = "训练尚未完成。";
    private const string SummaryNone = "暂无";
    private const int TrendHistoryLimit = 5;
    private const string TrendHistoryPlayerPrefsKey = "TracheostomyTrainingTrendHistory";

    [Serializable]
    private class TrendSnapshot
    {
        public string sessionId;
        public string generatedAt;
        public float overallScore;
        public float step1AngleError;
        public float step2CenterOffset;
        public float step3CenterOffset;
        public float step4SuccessState;
    }

    [Serializable]
    private class TrendSnapshotList
    {
        public List<TrendSnapshot> snapshots = new List<TrendSnapshot>();
    }

    private void Awake()
    {
        if (rubricConfig == null)
        {
            rubricConfig = RubricConfig.CreateDefault();
        }

        rubricConfig.EnsureStepDefaults();

        if (sessionRecorder == null)
        {
            sessionRecorder = GetComponent<SessionRecorder>();
        }

        if (sessionRecorder == null)
        {
            sessionRecorder = gameObject.AddComponent<SessionRecorder>();
        }

        sessionRecorder.config = rubricConfig;
    }

    private void Start()
    {
        Score = 100;
        ResetReportState();
    }

    private void Update()
    {
        if (isTrainingOver && !_reportGenerated)
        {
            ShowReport();
        }
    }

    public void ResetReportState()
    {
        isTrainingOver = false;
        _reportGenerated = false;
        TrainingTime = 0f;
        Score = 0;
        LatestReport = null;
        LatestExportedPaths = null;

        if (ReportText != null)
        {
            ReportText.text = ReportNotReady;
        }
    }

    public void ResetSessionState()
    {
        if (sessionRecorder != null)
        {
            sessionRecorder.ResetRecorder();
        }
    }

    public void ShowReport()
    {
        TrainingEvaluationReport report = BuildReport();

        LatestReport = report;
        _reportGenerated = true;
        Score = Mathf.RoundToInt(report.normalizedOverallScore * 100f);
        SaveTrendSnapshot(report);

        if (exportResearchDataOnTrainingEnd)
        {
            LatestExportedPaths = ResearchExporter.ExportAll(report);
        }
        else
        {
            LatestExportedPaths = null;
        }

        if (ReportText != null)
        {
            ReportText.text = BuildReportText(report);
        }
    }

    public string ExportLatestReport()
    {
        if (LatestReport == null)
        {
            LatestReport = BuildReport();
        }

        LatestExportedPaths = ResearchExporter.ExportAll(LatestReport);
        return LatestExportedPaths != null && LatestExportedPaths.Length > 0 ? LatestExportedPaths[0] : string.Empty;
    }

    public void BeginSession(string scenarioName)
    {
        if (rubricConfig == null)
        {
            rubricConfig = RubricConfig.CreateDefault();
        }

        rubricConfig.EnsureStepDefaults();

        if (sessionRecorder == null)
        {
            sessionRecorder = GetComponent<SessionRecorder>();
        }

        if (sessionRecorder == null)
        {
            sessionRecorder = gameObject.AddComponent<SessionRecorder>();
        }

        sessionRecorder.config = rubricConfig;
        sessionRecorder.BeginSession(string.IsNullOrWhiteSpace(scenarioName) ? ScenarioName : scenarioName);
        ResetReportState();
    }

    public void RecordRetry(string stepId, string reason)
    {
        if (sessionRecorder != null)
        {
            sessionRecorder.RecordRetry(stepId, reason);
        }
    }

    private TrainingEvaluationReport BuildReport()
    {
        EnsureDependencies();
        rubricConfig.EnsureStepDefaults();

        TrainingSessionRecord session = sessionRecorder != null && sessionRecorder.CurrentSession != null
            ? sessionRecorder.CurrentSession
            : CreateFallbackSession();

        PopulateSessionMetadata(session);
        PopulateSteps(session);
        MergeRecorderEvidence(session);

        TrainingEvaluationReport report = ScoreEngine.Evaluate(session, rubricConfig);
        report.exporterVersion = rubricConfig.algorithmVersion;
        report.reportVersion = rubricConfig.rubricVersion;
        report.exportDescription = "教学反馈与本地数据导出";
        report.metricDefinitions = BuildMetricDefinitions();
        report.exportMappings = BuildFieldMappings();
        CopySessionEvents(report);
        report.recommendations = BuildSessionRecommendations(report);
        report.warnings = BuildSessionWarnings(report);
        return report;
    }

    private void PopulateSessionMetadata(TrainingSessionRecord session)
    {
        session.scenarioId = "tracheostomy-training";
        session.scenarioName = ScenarioName;
        session.trainingMode = "skill-training";
        session.contentVersion = rubricConfig.scenarioVersion;
        session.schemaVersion = rubricConfig.rubricVersion;
        session.applicationVersion = Application.version;
        session.durationSeconds = TrainingTime;
    }

    private void PopulateSteps(TrainingSessionRecord session)
    {
        session.stepResults.Clear();
        session.stepResults.Add(BuildStep1Result());
        session.stepResults.Add(BuildStep2Result());
        session.stepResults.Add(BuildStep3Result());
        session.stepResults.Add(BuildStep4Result());
    }

    private StepEvaluationResult BuildStep1Result()
    {
        Step1Rubric rubric = rubricConfig.step1 ?? Step1Rubric.CreateDefault();
        if (positionDetermination == null)
        {
            return CreateUnavailableStepResult("step1", "步骤一：确定切割位置", 1, "定位组件未找到，无法计算定位指标。");
        }

        Transform referenceFrame = ResolveReferenceFrame(positionDetermination != null ? positionDetermination.transform : null);
        float markerLengthMeters = TrainingMeasurementUtility.DistanceMeters(positionDetermination.StartPointLocalPosition, positionDetermination.EndPointLocalPosition);
        float angleError = TrainingMeasurementUtility.DirectionErrorDegrees(
            referenceFrame,
            positionDetermination.StandardA,
            positionDetermination.StandardB,
            positionDetermination.StartPointLocalPosition,
            positionDetermination.EndPointLocalPosition,
            rubric.allowedAxes);
        float midpointOffsetMeters = TrainingMeasurementUtility.CenterOffsetMeters(
            referenceFrame,
            positionDetermination.StandardLine,
            positionDetermination.StandardA,
            positionDetermination.StandardB,
            positionDetermination.StartPointLocalPosition,
            positionDetermination.EndPointLocalPosition);

        bool hasOperation = HasStep1OperationEvidence();
        bool completed = positionDetermination.isPositionDetermined && hasOperation;
        float durationSeconds = GetStepDurationSeconds(SkillTrainingManager.TrainingStep.Step1_PositionDetermination, completed);
        StepEvaluationResult step = CreateBaseStepResult("step1", "步骤一：确定切割位置", 1, completed, durationSeconds);
        step.observedMetrics.Add(CreateMetric("s1_angle_error_deg", step.stepId, "定位角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateMetric("s1_midpoint_offset_cm", step.stepId, "定位中点偏移", "cm", midpointOffsetMeters * 100f, 0f, rubric.midpointOffsetThresholdMeters * 100f));
        step.observedMetrics.Add(CreateRangeMetric("s1_mark_length_cm", step.stepId, "标记长度", "cm", markerLengthMeters * 100f, rubric.markerLengthMinMeters * 100f, rubric.markerLengthMaxMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s1_duration_seconds", step.stepId, "步骤耗时", "s", durationSeconds, 0f, 0f));
        step.thresholdResults.Add(CreateThreshold("s1_direction_threshold", "定位方向", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, TrainingMeasurementUtility.AxisMatchScore(angleError), positionDetermination.isParallel));
        step.thresholdResults.Add(CreateThreshold("s1_midpoint_threshold", "定位偏移", step.stepId, midpointOffsetMeters * 100f, 0f, rubric.midpointOffsetThresholdMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s1_length_threshold", "标记长度", step.stepId, markerLengthMeters * 100f, rubric.markerLengthMinMeters * 100f, rubric.markerLengthMaxMeters * 100f));
        ApplyStepNarrative(step, hasOperation, positionDetermination.isParallel, positionDetermination.isPositionValid, "定位方向和位置都达标。", "定位偏差较大，建议重新确认环状软骨下方第 2-3 气管环区域。");
        return step;
    }

    private StepEvaluationResult BuildStep2Result()
    {
        Step23Rubric rubric = rubricConfig.step2 ?? Step23Rubric.CreateStep2Default();
        if (cutSkin == null)
        {
            return CreateUnavailableStepResult("step2", "步骤二：切开皮肤和组织", 2, "皮肤切开组件未找到，无法计算切口指标。");
        }

        float lengthMeters = cutSkin.MeasuredLengthCm * 0.01f;
        float angleError = cutSkin.MeasuredAngleErrorDeg;
        float centerOffsetMeters = cutSkin.MeasuredCenterOffsetCm * 0.01f;
        float idealLengthMeters = (rubric.lengthMinMeters + rubric.lengthMaxMeters) * 0.5f;
        float pathEfficiency = CalculatePathEfficiency(lengthMeters, idealLengthMeters, cutSkin.MeasuredPathLengthCm * 0.01f);

        bool hasOperation = HasCutOperationEvidence(cutSkin.MeasuredSampleCount, cutSkin.MeasuredLengthCm, cutSkin.MeasuredPathLengthCm);
        bool completed = cutSkin.isCutOver && hasOperation;
        float durationSeconds = GetStepDurationSeconds(SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue, completed);
        StepEvaluationResult step = CreateBaseStepResult("step2", "步骤二：切开皮肤和组织", 2, completed, durationSeconds);
        step.observedMetrics.Add(CreateMetric("s2_angle_error_deg", step.stepId, "切口角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateRangeMetric("s2_incision_length_cm", step.stepId, "切口长度", "cm", lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s2_center_offset_cm", step.stepId, "切口中心偏移", "cm", centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s2_path_efficiency", step.stepId, "路径效率", "ratio", pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        step.observedMetrics.Add(CreateMetric("s2_duration_seconds", step.stepId, "步骤耗时", "s", durationSeconds, 0f, 0f));
        step.thresholdResults.Add(CreateThreshold("s2_direction_threshold", "切口方向", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, TrainingMeasurementUtility.AxisMatchScore(angleError), cutSkin.isParallel));
        step.thresholdResults.Add(CreateThreshold("s2_length_threshold", "切口长度", step.stepId, lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s2_center_threshold", "切口中心偏移", step.stepId, centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s2_path_threshold", "路径效率", step.stepId, pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        ApplyStepNarrative(step, hasOperation, cutSkin.isParallel, cutSkin.isPositionValid, "皮肤和组织切开质量达标。", "皮肤与组织切开仍需改进，优先控制切口方向和长度。");
        return step;
    }

    private StepEvaluationResult BuildStep3Result()
    {
        Step23Rubric rubric = rubricConfig.step3 ?? Step23Rubric.CreateStep3Default();
        if (cutAirway == null)
        {
            return CreateUnavailableStepResult("step3", "步骤三：切开气管", 3, "气管切开组件未找到，无法计算气管切口指标。");
        }

        float lengthMeters = cutAirway.MeasuredLengthCm * 0.01f;
        float angleError = cutAirway.MeasuredAngleErrorDeg;
        float centerOffsetMeters = cutAirway.MeasuredCenterOffsetCm * 0.01f;
        float idealLengthMeters = (rubric.lengthMinMeters + rubric.lengthMaxMeters) * 0.5f;
        float pathEfficiency = CalculatePathEfficiency(lengthMeters, idealLengthMeters, cutAirway.MeasuredPathLengthCm * 0.01f);

        bool hasOperation = HasCutOperationEvidence(cutAirway.MeasuredSampleCount, cutAirway.MeasuredLengthCm, cutAirway.MeasuredPathLengthCm);
        bool completed = cutAirway.isCutOver && hasOperation;
        float durationSeconds = GetStepDurationSeconds(SkillTrainingManager.TrainingStep.Step3_CutAirway, completed);
        StepEvaluationResult step = CreateBaseStepResult("step3", "步骤三：切开气管", 3, completed, durationSeconds);
        step.observedMetrics.Add(CreateMetric("s3_angle_error_deg", step.stepId, "气管切口角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateRangeMetric("s3_incision_length_cm", step.stepId, "气管切口长度", "cm", lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s3_center_offset_cm", step.stepId, "气管切口中心偏移", "cm", centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.observedMetrics.Add(CreateNonScoringMetric("s3_lateral_offset_cm", step.stepId, "气管切口左右偏移", "cm", cutAirway.MeasuredLateralOffsetCm, "position-axis-helper"));
        step.observedMetrics.Add(CreateNonScoringMetric("s3_cranio_caudal_offset_cm", step.stepId, "气管切口头尾偏移", "cm", cutAirway.MeasuredCranioCaudalOffsetCm, "position-axis-helper"));
        step.observedMetrics.Add(CreateMetric("s3_path_efficiency", step.stepId, "路径效率", "ratio", pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        step.observedMetrics.Add(CreateMetric("s3_duration_seconds", step.stepId, "步骤耗时", "s", durationSeconds, 0f, 0f));
        step.thresholdResults.Add(CreateThreshold("s3_direction_threshold", "气管切口方向", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, TrainingMeasurementUtility.AxisMatchScore(angleError), cutAirway.isParallel));
        step.thresholdResults.Add(CreateThreshold("s3_length_threshold", "气管切口长度", step.stepId, lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s3_center_threshold", "气管切口中心偏移", step.stepId, centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s3_path_threshold", "路径效率", step.stepId, pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        ApplyStepNarrative(step, hasOperation, cutAirway.isParallel, cutAirway.isPositionValid, "气管切开角度和长度基本符合要求。", "气管切口未稳定落在目标区域，建议重点练习横向切开控制。");
        return step;
    }

    private StepEvaluationResult BuildStep4Result()
    {
        Step4Rubric rubric = rubricConfig.step4 ?? Step4Rubric.CreateDefault();
        if (insertTracheal == null)
        {
            return CreateUnavailableStepResult("step4", "步骤四：插入气管套管", 4, "插管组件未找到，无法计算插管指标。");
        }

        float holdDuration = insertTracheal.stableHoldDuration;
        float angleError = insertTracheal.isPositionValid ? 0f : rubric.angleErrorThresholdDegrees;
        PositionPenaltyCurveConfig positionCurve = rubricConfig.step4PositionCurve ?? PositionPenaltyCurveConfig.CreateStep4Default();
        float tubePlacementOffsetCm = insertTracheal.isPositionValid ? 0f : positionCurve.failCm;

        bool hasOperation = insertTracheal.isTrachealInserted || insertTracheal.stableHoldDuration > 0f || insertTracheal.isPositionValid;
        bool completed = insertTracheal.isInsertionOver && hasOperation;
        float durationSeconds = GetStepDurationSeconds(SkillTrainingManager.TrainingStep.Step4_InsertTracheal, completed);
        StepEvaluationResult step = CreateBaseStepResult("step4", "步骤四：插入气管套管", 4, completed, durationSeconds);
        step.observedMetrics.Add(CreateMetric("s4_success_state", step.stepId, "插管成功状态", "bool", insertTracheal.isPositionValid ? 1f : 0f, rubric.requireSuccessState ? 1f : 0f, 1f));
        step.observedMetrics.Add(CreateMetric("s4_tube_placement_offset_cm", step.stepId, "套管放置位置代理偏移", "cm", tubePlacementOffsetCm, 0f, positionCurve.failCm));
        step.observedMetrics.Add(CreateMetric("s4_stable_hold_seconds", step.stepId, "稳定停留时长", "s", holdDuration, rubric.stableHoldDurationSeconds, rubric.stableHoldDurationSeconds));
        step.observedMetrics.Add(CreateMetric("s4_angle_error_deg", step.stepId, "插管角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateMetric("s4_duration_seconds", step.stepId, "步骤耗时", "s", durationSeconds, 0f, 0f));
        step.thresholdResults.Add(CreateThreshold("s4_success_threshold", "插管成功", step.stepId, insertTracheal.isPositionValid ? 1f : 0f, rubric.requireSuccessState ? 1f : 0f, 1f));
        step.thresholdResults.Add(CreateThreshold("s4_hold_threshold", "稳定停留时长", step.stepId, holdDuration, rubric.stableHoldDurationSeconds, float.MaxValue));
        step.thresholdResults.Add(CreateThreshold("s4_angle_threshold", "插管角度误差", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, TrainingMeasurementUtility.AxisMatchScore(angleError), insertTracheal.isPositionValid));

        if (!hasOperation)
        {
            step.issues.Add("未检测到插管进入目标区域。");
            step.suggestedActions.Add("先让套管尖端进入气管目标区域，再结束步骤。");
            step.recommendation = "请完成实际插管操作后再提交该步骤。";
        }
        else if (insertTracheal.isPositionValid && holdDuration >= rubric.stableHoldDurationSeconds)
        {
            step.strengths.Add("套管已经稳定置入并达到保持时长要求。");
            step.recommendation = "继续保持插入角度和停留稳定性。";
        }
        else
        {
            step.issues.Add("套管未稳定保持在目标区域。");
            step.suggestedActions.Add("复练插管深度和进入角度控制。");
            step.recommendation = "优先保证成功置入，再延长稳定停留时间。";
        }

        return step;
    }

    private StepEvaluationResult CreateBaseStepResult(string stepId, string stepName, int stepIndex, bool completed, float durationSeconds)
    {
        return new StepEvaluationResult
        {
            stepId = stepId,
            stepName = stepName,
            stepIndex = stepIndex,
            completed = completed,
            isRequired = true,
            completionRatio = completed ? 1f : 0f,
            startTimeSeconds = GetStepStartTimeSeconds(stepIndex),
            endTimeSeconds = completed ? GetStepEndTimeSeconds(stepIndex, durationSeconds) : 0f,
            durationSeconds = Mathf.Max(durationSeconds, 0f),
            strengths = new List<string>(),
            issues = new List<string>(),
            suggestedActions = new List<string>(),
            observedMetrics = new List<ObservedMetricRecord>(),
            normalizedMetrics = new List<NormalizedMetricRecord>(),
            thresholdResults = new List<ThresholdResult>(),
            relatedEvents = new List<TrainingEventRecord>(),
            poseSamples = new List<PoseSample>()
        };
    }

    private float GetStepDurationSeconds(SkillTrainingManager.TrainingStep step, bool completed)
    {
        if (!completed)
        {
            return 0f;
        }

        if (skillTrainingManager == null)
        {
            skillTrainingManager = FindObjectOfType<SkillTrainingManager>();
        }

        return skillTrainingManager != null ? skillTrainingManager.GetStepDuration(step) : 0f;
    }

    private float GetStepStartTimeSeconds(int stepIndex)
    {
        SkillTrainingManager.TrainingStep step = ToTrainingStep(stepIndex);
        if (step == SkillTrainingManager.TrainingStep.Idle)
        {
            return 0f;
        }

        if (skillTrainingManager == null)
        {
            skillTrainingManager = FindObjectOfType<SkillTrainingManager>();
        }

        return skillTrainingManager != null ? skillTrainingManager.GetStepStartTime(step) : 0f;
    }

    private float GetStepEndTimeSeconds(int stepIndex, float durationSeconds)
    {
        SkillTrainingManager.TrainingStep step = ToTrainingStep(stepIndex);
        if (step == SkillTrainingManager.TrainingStep.Idle)
        {
            return Mathf.Max(durationSeconds, 0f);
        }

        if (skillTrainingManager == null)
        {
            skillTrainingManager = FindObjectOfType<SkillTrainingManager>();
        }

        if (skillTrainingManager == null)
        {
            return Mathf.Max(durationSeconds, 0f);
        }

        float endTime = skillTrainingManager.GetStepEndTime(step);
        if (endTime > 0f)
        {
            return endTime;
        }

        return GetStepStartTimeSeconds(stepIndex) + Mathf.Max(durationSeconds, 0f);
    }

    private SkillTrainingManager.TrainingStep ToTrainingStep(int stepIndex)
    {
        switch (stepIndex)
        {
            case 1:
                return SkillTrainingManager.TrainingStep.Step1_PositionDetermination;
            case 2:
                return SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue;
            case 3:
                return SkillTrainingManager.TrainingStep.Step3_CutAirway;
            case 4:
                return SkillTrainingManager.TrainingStep.Step4_InsertTracheal;
            default:
                return SkillTrainingManager.TrainingStep.Idle;
        }
    }

    private StepEvaluationResult CreateUnavailableStepResult(string stepId, string stepName, int stepIndex, string issue)
    {
        StepEvaluationResult step = CreateBaseStepResult(stepId, stepName, stepIndex, false, 0f);
        step.status = TrainingOverallStatus.Incomplete;
        step.summary = issue;
        step.recommendation = "请检查场景引用后重新生成报告。";
        step.issues.Add(issue);
        return step;
    }

    private void ApplyStepNarrative(StepEvaluationResult step, bool hasOperation, bool axisValid, bool positionValid, string passText, string failText)
    {
        if (!hasOperation)
        {
            step.issues.Add("未检测到有效操作轨迹或目标区域接触。");
            step.suggestedActions.Add("请使用对应器械触碰目标区域并完成该步骤。");
            step.recommendation = "没有实际操作时该步骤记为 0 分。";
            return;
        }

        if (axisValid && positionValid)
        {
            step.strengths.Add(passText);
            step.recommendation = "保持当前操作习惯。";
            return;
        }

        if (!axisValid)
        {
            step.issues.Add("操作方向未对齐标准轴。");
            step.suggestedActions.Add("先对齐参考轴，再开始操作。");
        }

        if (!positionValid)
        {
            step.issues.Add("操作位置或长度未落在有效范围内。");
            step.suggestedActions.Add("缩小偏移并控制操作范围。");
        }

        step.recommendation = failText;
    }

    private bool HasStep1OperationEvidence()
    {
        return positionDetermination != null &&
            (positionDetermination.MeasuredSampleCount >= 2 || positionDetermination.MeasuredLengthCm > 0.01f);
    }

    private bool HasCutOperationEvidence(int sampleCount, float measuredLengthCm, float measuredPathLengthCm)
    {
        return sampleCount >= 2 || measuredLengthCm > 0.01f || measuredPathLengthCm > 0.01f;
    }

    private ObservedMetricRecord CreateMetric(string metricId, string stepId, string displayName, string unit, float rawValue, float targetValue, float maxObservedValue, AllowedAxes axes = AllowedAxes.None)
    {
        return new ObservedMetricRecord
        {
            metricId = metricId,
            stepId = stepId,
            displayName = displayName,
            unit = unit,
            rawValue = rawValue,
            targetValue = targetValue,
            minObservedValue = Mathf.Min(rawValue, targetValue),
            maxObservedValue = Mathf.Max(rawValue, maxObservedValue),
            averageObservedValue = rawValue,
            sampleCount = 1,
            expectedAxes = axes,
            observedAxes = axes,
            axisMatchRatio = 1f,
            axisMatched = true,
            isValid = true
        };
    }

    private ObservedMetricRecord CreateRangeMetric(string metricId, string stepId, string displayName, string unit, float rawValue, float minValue, float maxValue)
    {
        return new ObservedMetricRecord
        {
            metricId = metricId,
            stepId = stepId,
            displayName = displayName,
            unit = unit,
            rawValue = rawValue,
            targetValue = (minValue + maxValue) * 0.5f,
            minObservedValue = minValue,
            maxObservedValue = maxValue,
            averageObservedValue = rawValue,
            sampleCount = 1,
            axisMatchRatio = 1f,
            axisMatched = true,
            isValid = true,
            note = "range-metric"
        };
    }

    private ObservedMetricRecord CreateNonScoringMetric(string metricId, string stepId, string displayName, string unit, float rawValue, string note)
    {
        ObservedMetricRecord metric = CreateMetric(metricId, stepId, displayName, unit, rawValue, 0f, 0f);
        metric.note = note;
        return metric;
    }

    private ThresholdResult CreateThreshold(string thresholdId, string label, string stepId, float actualValue, float expectedMin, float expectedMax, AllowedAxes axes = AllowedAxes.None, float axisMatchScore = 1f, bool axisMatched = true)
    {
        bool minOk = expectedMin <= 0f || actualValue >= expectedMin;
        bool maxOk = expectedMax <= 0f || actualValue <= expectedMax;
        bool passed = minOk && maxOk && axisMatched;

        return new ThresholdResult
        {
            thresholdId = thresholdId,
            metricId = thresholdId,
            stepId = stepId,
            label = label,
            actualValue = actualValue,
            expectedMin = expectedMin,
            expectedMax = expectedMax,
            warningThreshold = expectedMax,
            criticalThreshold = expectedMax,
            expectedAxes = axes,
            observedAxes = axes,
            axisMatchScore = axisMatchScore,
            axisMatched = axisMatched,
            passed = passed,
            warningTriggered = !passed,
            criticalTriggered = false,
            resultMessage = passed ? "通过" : "未通过",
            recommendation = passed ? string.Empty : "建议根据该指标重新练习。"
        };
    }

    private Transform ResolveReferenceFrame(Transform referenceFrame)
    {
        return referenceFrame != null ? referenceFrame : transform;
    }

    private float CalculatePathEfficiency(float actualLengthMeters, float idealLengthMeters, float pathLengthMeters = 0f)
    {
        if (actualLengthMeters <= 0f || idealLengthMeters <= 0f)
        {
            return 0f;
        }

        float lengthEfficiency = Mathf.Clamp01(Mathf.Min(actualLengthMeters, idealLengthMeters) / Mathf.Max(actualLengthMeters, idealLengthMeters));
        float shapeEfficiency = pathLengthMeters > 0f
            ? Mathf.Clamp01(actualLengthMeters / Mathf.Max(actualLengthMeters, pathLengthMeters))
            : 1f;
        return Mathf.Clamp01(lengthEfficiency * shapeEfficiency);
    }

    private List<MetricDefinition> BuildMetricDefinitions()
    {
        return new List<MetricDefinition>
        {
            CreateDefinition("s1_angle_error_deg", "步骤一角度误差", "deg", MetricDirection.LowerIsBetter, 0f, rubricConfig.step1.angleErrorThresholdDegrees),
            CreateDefinition("s1_midpoint_offset_cm", "步骤一中点偏移", "cm", MetricDirection.LowerIsBetter, 0f, rubricConfig.step1.midpointOffsetThresholdMeters * 100f),
            CreateDefinition("s1_mark_length_cm", "步骤一标记长度", "cm", MetricDirection.WithinRangeIsBetter, rubricConfig.step1.markerLengthMinMeters * 100f, rubricConfig.step1.markerLengthMaxMeters * 100f),
            CreateDefinition("s2_incision_length_cm", "步骤二切口长度", "cm", MetricDirection.WithinRangeIsBetter, rubricConfig.step2.lengthMinMeters * 100f, rubricConfig.step2.lengthMaxMeters * 100f),
            CreateDefinition("s2_center_offset_cm", "步骤二中心偏移", "cm", MetricDirection.LowerIsBetter, 0f, rubricConfig.step2.centerOffsetThresholdMeters * 100f),
            CreateDefinition("s3_incision_length_cm", "步骤三切口长度", "cm", MetricDirection.WithinRangeIsBetter, rubricConfig.step3.lengthMinMeters * 100f, rubricConfig.step3.lengthMaxMeters * 100f),
            CreateDefinition("s4_stable_hold_seconds", "步骤四稳定停留", "s", MetricDirection.HigherIsBetter, rubricConfig.step4.stableHoldDurationSeconds, 0f)
        };
    }

    private MetricDefinition CreateDefinition(string metricId, string displayName, string unit, MetricDirection direction, float minValue, float maxValue)
    {
        return new MetricDefinition
        {
            metricId = metricId,
            displayName = displayName,
            unit = unit,
            direction = direction,
            minAcceptableValue = minValue,
            maxAcceptableValue = maxValue
        };
    }

    private List<FieldMappingEntry> BuildFieldMappings()
    {
        return new List<FieldMappingEntry>
        {
            CreateFieldMapping("reportId", "报告编号", "report.reportId", "string", "评估报告唯一编号"),
            CreateFieldMapping("sessionId", "会话编号", "report.session.sessionId", "string", "训练会话唯一编号"),
            CreateFieldMapping("overallStatus", "总评状态", "report.overallStatus", "enum", "训练总评状态"),
            CreateFieldMapping("overallScore", "总评分", "report.overallScore", "float", "0-1 归一化总分"),
            CreateFieldMapping("stepScore", "步骤得分", "report.stepResults[].normalizedScore", "float", "单步骤归一化得分"),
            CreateFieldMapping("stepSummary", "步骤总结", "report.stepResults[].summary", "string", "单步骤总结")
        };
    }

    private FieldMappingEntry CreateFieldMapping(string key, string label, string fieldPath, string valueType, string description)
    {
        return new FieldMappingEntry
        {
            exportKey = key,
            exportLabel = label,
            sourceFieldPath = fieldPath,
            valueType = valueType,
            description = description
        };
    }

    private List<string> BuildSessionRecommendations(TrainingEvaluationReport report)
    {
        List<string> recommendations = new List<string>();

        foreach (StepEvaluationResult step in report.stepResults)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.recommendation))
            {
                continue;
            }

            recommendations.Add(step.stepName + "：" + step.recommendation);
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("整体表现稳定，可继续保持当前训练节奏。");
        }

        return recommendations;
    }

    private List<string> BuildSessionWarnings(TrainingEvaluationReport report)
    {
        List<string> warnings = new List<string>();

        foreach (StepEvaluationResult step in report.stepResults)
        {
            if (step == null || step.status == TrainingOverallStatus.Passed)
            {
                continue;
            }

            warnings.Add(step.stepName + "：" + FirstNonEmpty(step.summary, "存在待改进项。"));
        }

        return warnings;
    }

    private string BuildReportText(TrainingEvaluationReport report)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(ReportTitle);
        sb.AppendLine("训练时间: " + TrainingTime.ToString("F2", CultureInfo.InvariantCulture) + " 秒");
        sb.AppendLine("总评分: " + Mathf.RoundToInt(report.normalizedOverallScore * 100f) + " / 100");
        sb.AppendLine("通过线: " + Mathf.RoundToInt(report.session.passingScore * 100f) + " / 100");
        sb.AppendLine("门槛结论: " + GetOverallStatusText(report.overallStatus));
        sb.AppendLine("总评摘要: " + FirstNonEmpty(report.overallSummary, SummaryNone));
        sb.AppendLine();
        sb.AppendLine("分步评分");

        foreach (StepEvaluationResult step in report.stepResults)
        {
            if (step == null)
            {
                continue;
            }

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0} | 得分 {1}/100 | 状态: {2}",
                step.stepName,
                Mathf.RoundToInt(step.normalizedScore * 100f),
                GetOverallStatusText(step.status)));

            if (!string.IsNullOrWhiteSpace(step.summary))
            {
                sb.AppendLine("  结论: " + step.summary);
            }

            AppendMetricSummary(sb, step.observedMetrics);
        }

        sb.AppendLine();
        sb.AppendLine("改进建议");
        foreach (string recommendation in report.recommendations)
        {
            sb.AppendLine("- " + recommendation);
        }

        AppendTrendSummary(sb);

        if (LatestExportedPaths != null && LatestExportedPaths.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("导出文件");
            foreach (string path in LatestExportedPaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    sb.AppendLine("- " + path);
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private void AppendMetricSummary(StringBuilder sb, List<ObservedMetricRecord> metrics)
    {
        if (metrics == null)
        {
            return;
        }

        foreach (ObservedMetricRecord metric in metrics)
        {
            if (metric == null || string.IsNullOrWhiteSpace(metric.displayName))
            {
                continue;
            }

            sb.AppendLine("  " + metric.displayName + ": " + metric.rawValue.ToString("F2", CultureInfo.InvariantCulture) + " " + metric.unit);
        }
    }

    private void AppendTrendSummary(StringBuilder sb)
    {
        TrendSnapshotList history = LoadTrendHistory();
        if (history.snapshots.Count < 2)
        {
            return;
        }

        TrendSnapshot latest = history.snapshots[history.snapshots.Count - 1];
        TrendSnapshot previous = history.snapshots[history.snapshots.Count - 2];

        sb.AppendLine();
        sb.AppendLine("重复训练趋势");
        sb.AppendLine("总评分变化: " + FormatDelta(latest.overallScore, previous.overallScore, true));
        sb.AppendLine("定位角度误差: " + FormatDelta(latest.step1AngleError, previous.step1AngleError, false));
        sb.AppendLine("皮肤切口中心偏移: " + FormatDelta(latest.step2CenterOffset, previous.step2CenterOffset, false));
        sb.AppendLine("气管切口中心偏移: " + FormatDelta(latest.step3CenterOffset, previous.step3CenterOffset, false));
        sb.AppendLine("插管成功状态: " + latest.step4SuccessState.ToString("F0", CultureInfo.InvariantCulture));
    }

    private string FormatDelta(float current, float previous, bool higherIsBetter)
    {
        float delta = current - previous;
        string direction = Mathf.Approximately(delta, 0f) ? "持平" : delta > 0f ? "上升" : "下降";
        bool improved = Mathf.Approximately(delta, 0f) || (higherIsBetter ? delta > 0f : delta < 0f);
        string suffix = improved ? "，趋势良好" : "，需关注";
        return string.Format(CultureInfo.InvariantCulture, "{0:F2} ({1} {2:F2}{3})", current, direction, Mathf.Abs(delta), suffix);
    }

    private string GetOverallStatusText(TrainingOverallStatus status)
    {
        switch (status)
        {
            case TrainingOverallStatus.Passed:
                return "通过";
            case TrainingOverallStatus.PassedWithWarnings:
                return "通过，含警告";
            case TrainingOverallStatus.Failed:
                return "未通过";
            case TrainingOverallStatus.Incomplete:
                return "未完成";
            case TrainingOverallStatus.InProgress:
                return "进行中";
            case TrainingOverallStatus.Cancelled:
                return "已取消";
            default:
                return "未开始";
        }
    }

    private void EnsureDependencies()
    {
        if (skillTrainingManager == null)
        {
            skillTrainingManager = FindObjectOfType<SkillTrainingManager>();
        }

        if (positionDetermination == null)
        {
            positionDetermination = FindObjectOfType<PositionDetermination>();
        }

        if (cutSkin == null)
        {
            cutSkin = FindObjectOfType<CutSkin>();
        }

        if (cutAirway == null)
        {
            cutAirway = FindObjectOfType<CutAirway>();
        }

        if (insertTracheal == null)
        {
            insertTracheal = FindObjectOfType<InsertTracheal>();
        }
    }

    private TrainingSessionRecord CreateFallbackSession()
    {
        return new TrainingSessionRecord
        {
            sessionId = Guid.NewGuid().ToString("N"),
            scenarioId = "tracheostomy-training",
            scenarioName = ScenarioName,
            startedAt = DateTime.UtcNow.ToString("o"),
            durationSeconds = TrainingTime,
            schemaVersion = rubricConfig.rubricVersion,
            applicationVersion = Application.version,
            contentVersion = rubricConfig.scenarioVersion
        };
    }

    private string FirstNonEmpty(string primary, string fallback)
    {
        return string.IsNullOrWhiteSpace(primary) ? fallback : primary;
    }

    private void MergeRecorderEvidence(TrainingSessionRecord session)
    {
        if (sessionRecorder == null || sessionRecorder.CurrentReport == null || sessionRecorder.CurrentReport.stepResults == null)
        {
            return;
        }

        for (int i = 0; i < session.stepResults.Count; i++)
        {
            StepEvaluationResult target = session.stepResults[i];
            StepEvaluationResult source = FindStepById(sessionRecorder.CurrentReport.stepResults, target != null ? target.stepId : string.Empty);
            if (target == null || source == null)
            {
                continue;
            }

            if (source.relatedEvents != null && source.relatedEvents.Count > 0)
            {
                target.relatedEvents.AddRange(source.relatedEvents);
            }

            if (source.poseSamples != null && source.poseSamples.Count > 0)
            {
                target.poseSamples.AddRange(source.poseSamples);
            }
        }
    }

    private void CopySessionEvents(TrainingEvaluationReport report)
    {
        if (report == null || sessionRecorder == null || sessionRecorder.CurrentReport == null || sessionRecorder.CurrentReport.sessionEvents == null)
        {
            return;
        }

        report.sessionEvents.Clear();
        report.sessionEvents.AddRange(sessionRecorder.CurrentReport.sessionEvents);
    }

    private StepEvaluationResult FindStepById(List<StepEvaluationResult> steps, string stepId)
    {
        if (steps == null || string.IsNullOrEmpty(stepId))
        {
            return null;
        }

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].stepId == stepId)
            {
                return steps[i];
            }
        }

        return null;
    }

    private void SaveTrendSnapshot(TrainingEvaluationReport report)
    {
        if (report == null)
        {
            return;
        }

        TrendSnapshotList history = LoadTrendHistory();
        TrendSnapshot snapshot = new TrendSnapshot
        {
            sessionId = report.session != null ? report.session.sessionId : report.reportId,
            generatedAt = report.reportGeneratedAt,
            overallScore = report.normalizedOverallScore * 100f,
            step1AngleError = FindMetricValue(report, "s1_angle_error_deg"),
            step2CenterOffset = FindMetricValue(report, "s2_center_offset_cm"),
            step3CenterOffset = FindMetricValue(report, "s3_center_offset_cm"),
            step4SuccessState = FindMetricValue(report, "s4_success_state")
        };

        history.snapshots.Add(snapshot);
        while (history.snapshots.Count > TrendHistoryLimit)
        {
            history.snapshots.RemoveAt(0);
        }

        PlayerPrefs.SetString(TrendHistoryPlayerPrefsKey, JsonUtility.ToJson(history));
        PlayerPrefs.Save();
    }

    private TrendSnapshotList LoadTrendHistory()
    {
        string json = PlayerPrefs.GetString(TrendHistoryPlayerPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new TrendSnapshotList();
        }

        try
        {
            TrendSnapshotList history = JsonUtility.FromJson<TrendSnapshotList>(json);
            return history != null && history.snapshots != null ? history : new TrendSnapshotList();
        }
        catch (Exception)
        {
            return new TrendSnapshotList();
        }
    }

    private float FindMetricValue(TrainingEvaluationReport report, string metricId)
    {
        if (report == null || report.stepResults == null || string.IsNullOrEmpty(metricId))
        {
            return 0f;
        }

        for (int i = 0; i < report.stepResults.Count; i++)
        {
            StepEvaluationResult step = report.stepResults[i];
            if (step == null || step.observedMetrics == null)
            {
                continue;
            }

            for (int j = 0; j < step.observedMetrics.Count; j++)
            {
                ObservedMetricRecord metric = step.observedMetrics[j];
                if (metric != null && metric.metricId == metricId)
                {
                    return metric.rawValue;
                }
            }
        }

        return 0f;
    }
}
