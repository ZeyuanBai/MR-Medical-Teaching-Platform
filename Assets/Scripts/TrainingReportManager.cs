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

    private void Awake()
    {
        if (rubricConfig == null)
        {
            rubricConfig = RubricConfig.CreateDefault();
        }

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
        Score = 0;
        LatestReport = null;
        LatestExportedPaths = null;

        if (ReportText != null)
        {
            ReportText.text = ReportNotReady;
        }
    }

    public void ShowReport()
    {
        TrainingEvaluationReport report = BuildReport();

        LatestReport = report;
        _reportGenerated = true;
        Score = Mathf.RoundToInt(report.normalizedOverallScore * 100f);

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

        TrainingSessionRecord session = sessionRecorder != null && sessionRecorder.CurrentSession != null
            ? sessionRecorder.CurrentSession
            : CreateFallbackSession();

        PopulateSessionMetadata(session);
        PopulateSteps(session);

        TrainingEvaluationReport report = ScoreEngine.Evaluate(session, rubricConfig);
        report.exporterVersion = rubricConfig.algorithmVersion;
        report.reportVersion = rubricConfig.rubricVersion;
        report.exportDescription = "教学反馈与科研导出";
        report.metricDefinitions = BuildMetricDefinitions();
        report.exportMappings = BuildFieldMappings();
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
        float markerLengthMeters = Vector3.Distance(positionDetermination.StartPointPosition, positionDetermination.EndPointPosition);
        float angleError = CalculateAngleError(
            positionDetermination.StandardA,
            positionDetermination.StandardB,
            positionDetermination.StartPointLocalPosition,
            positionDetermination.EndPointLocalPosition,
            rubric.allowedAxes);
        float midpointOffsetMeters = Vector3.Distance(
            positionDetermination.StandardLine.transform.position,
            (positionDetermination.StartPointPosition + positionDetermination.EndPointPosition) * 0.5f);

        StepEvaluationResult step = CreateBaseStepResult("step1", "步骤一：确定切割位置", 1, positionDetermination.isPositionDetermined, 0f);
        step.observedMetrics.Add(CreateMetric("s1_angle_error_deg", step.stepId, "定位角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateMetric("s1_midpoint_offset_cm", step.stepId, "定位中点偏移", "cm", midpointOffsetMeters * 100f, 0f, rubric.midpointOffsetThresholdMeters * 100f));
        step.observedMetrics.Add(CreateRangeMetric("s1_mark_length_cm", step.stepId, "标记长度", "cm", markerLengthMeters * 100f, rubric.markerLengthMinMeters * 100f, rubric.markerLengthMaxMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s1_duration_seconds", step.stepId, "步骤耗时", "s", TrainingTime, 0f, 0f));
        step.thresholdResults.Add(CreateThreshold("s1_direction_threshold", "定位方向", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, positionDetermination.isParallel ? 1f : 0f, positionDetermination.isParallel));
        step.thresholdResults.Add(CreateThreshold("s1_midpoint_threshold", "定位偏移", step.stepId, midpointOffsetMeters * 100f, 0f, rubric.midpointOffsetThresholdMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s1_length_threshold", "标记长度", step.stepId, markerLengthMeters * 100f, rubric.markerLengthMinMeters * 100f, rubric.markerLengthMaxMeters * 100f));
        ApplyStepNarrative(step, positionDetermination.isParallel, positionDetermination.isPositionValid, "定位方向和位置都达标。", "定位偏差较大，建议重新确认环状软骨下方第 2-3 气管环区域。");
        return step;
    }

    private StepEvaluationResult BuildStep2Result()
    {
        Step23Rubric rubric = rubricConfig.step2 ?? Step23Rubric.CreateStep2Default();
        float lengthMeters = Vector3.Distance(cutSkin.StartPointPosition, cutSkin.EndPointPosition);
        float angleError = CalculateAngleError(
            cutSkin.StandardA,
            cutSkin.StandardB,
            cutSkin.StartPointLocalPosition,
            cutSkin.EndPointLocalPosition,
            rubric.allowedAxes);
        float centerOffsetMeters = Vector3.Distance(
            cutSkin.StandardLine.transform.position,
            (cutSkin.StartPointPosition + cutSkin.EndPointPosition) * 0.5f);
        float idealLengthMeters = (rubric.lengthMinMeters + rubric.lengthMaxMeters) * 0.5f;
        float pathEfficiency = CalculatePathEfficiency(lengthMeters, idealLengthMeters);

        StepEvaluationResult step = CreateBaseStepResult("step2", "步骤二：切开皮肤和组织", 2, cutSkin.isCutOver, 0f);
        step.observedMetrics.Add(CreateMetric("s2_angle_error_deg", step.stepId, "切口角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateRangeMetric("s2_incision_length_cm", step.stepId, "切口长度", "cm", lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s2_center_offset_cm", step.stepId, "切口中心偏移", "cm", centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s2_path_efficiency", step.stepId, "路径效率", "ratio", pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        step.thresholdResults.Add(CreateThreshold("s2_direction_threshold", "切口方向", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, cutSkin.isParallel ? 1f : 0f, cutSkin.isParallel));
        step.thresholdResults.Add(CreateThreshold("s2_length_threshold", "切口长度", step.stepId, lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s2_center_threshold", "切口中心偏移", step.stepId, centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s2_path_threshold", "路径效率", step.stepId, pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        ApplyStepNarrative(step, cutSkin.isParallel, cutSkin.isPositionValid, "皮肤和组织切开质量达标。", "皮肤与组织切开仍需改进，优先控制切口方向和长度。");
        return step;
    }

    private StepEvaluationResult BuildStep3Result()
    {
        Step23Rubric rubric = rubricConfig.step3 ?? Step23Rubric.CreateStep3Default();
        float lengthMeters = Vector3.Distance(cutAirway.StartPointPosition, cutAirway.EndPointPosition) * 1.2f;
        float angleError = CalculateAngleError(
            cutAirway.StandardA,
            cutAirway.StandardB,
            cutAirway.StartPointLocalPosition,
            cutAirway.EndPointLocalPosition,
            rubric.allowedAxes);
        float centerOffsetMeters = Vector3.Distance(
            cutAirway.StandardLine.transform.position,
            (cutAirway.StartPointPosition + cutAirway.EndPointPosition) * 0.5f);
        float idealLengthMeters = (rubric.lengthMinMeters + rubric.lengthMaxMeters) * 0.5f;
        float pathEfficiency = CalculatePathEfficiency(lengthMeters, idealLengthMeters);

        StepEvaluationResult step = CreateBaseStepResult("step3", "步骤三：切开气管", 3, cutAirway.isCutOver, 0f);
        step.observedMetrics.Add(CreateMetric("s3_angle_error_deg", step.stepId, "气管切口角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.observedMetrics.Add(CreateRangeMetric("s3_incision_length_cm", step.stepId, "气管切口长度", "cm", lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s3_center_offset_cm", step.stepId, "气管切口中心偏移", "cm", centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.observedMetrics.Add(CreateMetric("s3_path_efficiency", step.stepId, "路径效率", "ratio", pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        step.thresholdResults.Add(CreateThreshold("s3_direction_threshold", "气管切口方向", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, cutAirway.isParallel ? 1f : 0f, cutAirway.isParallel));
        step.thresholdResults.Add(CreateThreshold("s3_length_threshold", "气管切口长度", step.stepId, lengthMeters * 100f, rubric.lengthMinMeters * 100f, rubric.lengthMaxMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s3_center_threshold", "气管切口中心偏移", step.stepId, centerOffsetMeters * 100f, 0f, rubric.centerOffsetThresholdMeters * 100f));
        step.thresholdResults.Add(CreateThreshold("s3_path_threshold", "路径效率", step.stepId, pathEfficiency, rubric.pathEfficiencyThreshold, 1f));
        ApplyStepNarrative(step, cutAirway.isParallel, cutAirway.isPositionValid, "气管切开角度和长度基本符合要求。", "气管切口未稳定落在目标区域，建议重点练习横向切开控制。");
        return step;
    }

    private StepEvaluationResult BuildStep4Result()
    {
        Step4Rubric rubric = rubricConfig.step4 ?? Step4Rubric.CreateDefault();
        float holdDuration = insertTracheal.stableHoldDuration;
        float angleError = insertTracheal.isPositionValid ? 0f : rubric.angleErrorThresholdDegrees;

        StepEvaluationResult step = CreateBaseStepResult("step4", "步骤四：插入气管套管", 4, insertTracheal.isInsertionOver, holdDuration);
        step.observedMetrics.Add(CreateMetric("s4_success_state", step.stepId, "插管成功状态", "bool", insertTracheal.isPositionValid ? 1f : 0f, rubric.requireSuccessState ? 1f : 0f, 1f));
        step.observedMetrics.Add(CreateMetric("s4_stable_hold_seconds", step.stepId, "稳定停留时长", "s", holdDuration, rubric.stableHoldDurationSeconds, rubric.stableHoldDurationSeconds));
        step.observedMetrics.Add(CreateMetric("s4_angle_error_deg", step.stepId, "插管角度误差", "deg", angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes));
        step.thresholdResults.Add(CreateThreshold("s4_success_threshold", "插管成功", step.stepId, insertTracheal.isPositionValid ? 1f : 0f, rubric.requireSuccessState ? 1f : 0f, 1f));
        step.thresholdResults.Add(CreateThreshold("s4_hold_threshold", "稳定停留时长", step.stepId, holdDuration, rubric.stableHoldDurationSeconds, float.MaxValue));
        step.thresholdResults.Add(CreateThreshold("s4_angle_threshold", "插管角度误差", step.stepId, angleError, 0f, rubric.angleErrorThresholdDegrees, rubric.allowedAxes, insertTracheal.isPositionValid ? 1f : 0f, insertTracheal.isPositionValid));

        if (insertTracheal.isPositionValid && holdDuration >= rubric.stableHoldDurationSeconds)
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
            startTimeSeconds = 0f,
            endTimeSeconds = completed ? Mathf.Max(durationSeconds, 0.01f) : 0f,
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

    private void ApplyStepNarrative(StepEvaluationResult step, bool axisValid, bool positionValid, string passText, string failText)
    {
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
            criticalTriggered = !passed,
            resultMessage = passed ? "通过" : "未通过",
            recommendation = passed ? string.Empty : "建议根据该指标重新练习。"
        };
    }

    private float CalculateAngleError(GameObject standardA, GameObject standardB, Vector3 startLocalPosition, Vector3 endLocalPosition, AllowedAxes allowedAxes)
    {
        if (standardA == null || standardB == null)
        {
            return 0f;
        }

        Vector3 standardDirection = (standardB.transform.position - standardA.transform.position).normalized;
        Vector3 localCutDirection = (endLocalPosition - startLocalPosition).normalized;
        if (localCutDirection == Vector3.zero)
        {
            return 90f;
        }

        Vector3 worldCutDirection = transform.TransformDirection(localCutDirection);
        Vector3 horizontalReference = Vector3.Cross(Vector3.up, standardDirection);
        if (horizontalReference == Vector3.zero)
        {
            horizontalReference = Vector3.right;
        }

        float verticalError = Vector3.Angle(worldCutDirection, standardDirection);
        float horizontalError = Vector3.Angle(worldCutDirection, horizontalReference.normalized);
        return Mathf.Abs(ScoreEngine.SelectDirectionError(horizontalError, verticalError, allowedAxes));
    }

    private float CalculatePathEfficiency(float actualLengthMeters, float idealLengthMeters)
    {
        if (actualLengthMeters <= 0f || idealLengthMeters <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.Min(actualLengthMeters, idealLengthMeters) / Mathf.Max(actualLengthMeters, idealLengthMeters));
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
}
