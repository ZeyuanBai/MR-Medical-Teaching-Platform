using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ScoreEngine
{
    public static TrainingEvaluationReport Evaluate(TrainingSessionRecord session, RubricConfig config)
    {
        RubricConfig rubric = config ?? RubricConfig.CreateDefault();
        rubric.EnsureStepDefaults();
        TrainingEvaluationReport report = new TrainingEvaluationReport();
        report.session = session ?? new TrainingSessionRecord();
        report.reportId = string.IsNullOrWhiteSpace(report.session.sessionId)
            ? Guid.NewGuid().ToString("N")
            : "report-" + report.session.sessionId;
        report.reportGeneratedAt = ResolveReportTimestamp(report.session);

        float passScore = NormalizeScore(rubric.passScore);
        report.session.passingScore = passScore;

        List<StepEvaluationResult> steps = GetSessionSteps(session);
        float total = 0f;
        int counted = 0;
        bool hasCritical = false;
        bool hasWarning = false;
        bool hasIncomplete = false;

        for (int i = 0; i < steps.Count; i++)
        {
            StepEvaluationResult step = steps[i];
            if (step == null) continue;

            EvaluateStep(step, rubric);
            report.stepResults.Add(step);
            total += Mathf.Clamp01(step.normalizedScore);
            counted++;

            AddRange(report.sessionObservedMetrics, step.observedMetrics);
            AddRange(report.sessionNormalizedMetrics, step.normalizedMetrics);
            AddRange(report.sessionThresholdResults, step.thresholdResults);

            hasCritical |= HasCritical(step) || HasDangerEvent(step);
            hasWarning |= HasWarning(step);
            hasIncomplete |= step.isRequired && !step.completed;
        }

        report.overallScore = counted > 0 ? total / counted : 0f;
        report.normalizedOverallScore = report.overallScore;
        report.passed = report.overallScore >= passScore && !hasCritical && !hasIncomplete;
        report.overallStatus = GetOverallStatus(report.passed, hasCritical, hasWarning, hasIncomplete);
        report.overallSummary = string.Format("共评估 {0} 个步骤，总分 {1:P0}。", counted, report.overallScore);
        report.overallRecommendation = report.passed ? "整体表现达标，可继续保持。" : "建议根据低分步骤进行针对性练习。";
        report.recommendations.Add(report.overallRecommendation);

        if (hasIncomplete) report.warnings.Add("存在未完成的必做步骤。");
        if (hasCritical) report.warnings.Add("存在关键阈值未通过。");
        else if (hasWarning) report.warnings.Add("存在警告项，建议复查相关指标。");

        report.session.overallScore = report.overallScore;
        report.session.overallStatus = report.overallStatus;
        report.session.summary = report.overallSummary;
        return report;
    }

    public static float SelectDirectionError(float horizontalErrorDegrees, float verticalErrorDegrees, AllowedAxes allowedAxes)
    {
        if (allowedAxes == AllowedAxes.Horizontal) return horizontalErrorDegrees;
        if (allowedAxes == AllowedAxes.Vertical) return verticalErrorDegrees;
        return allowedAxes == AllowedAxes.Both ? Mathf.Min(horizontalErrorDegrees, verticalErrorDegrees) : 0f;
    }

    private static void EvaluateStep(StepEvaluationResult step, RubricConfig rubric)
    {
        EnsureLists(step);
        step.normalizedMetrics.Clear();

        float detailTotal = 0f;
        int counted = 0;

        for (int i = 0; i < step.observedMetrics.Count; i++)
        {
            ObservedMetricRecord metric = step.observedMetrics[i];
            if (metric == null || IsNonScoring(metric)) continue;

            NormalizedMetricRecord normalized = NormalizeMetric(metric, rubric, step.observedMetrics);
            step.normalizedMetrics.Add(normalized);
            detailTotal += Mathf.Clamp01(normalized.weightedScore);
            counted++;
        }

        float detailScore = counted > 0 ? detailTotal / counted : 0f;
        step.normalizedScore = ComposeStepScore(step.completed, detailScore, rubric);
        step.rawScore = step.normalizedScore;
        EvaluateThresholds(step);

        if (!step.completed && step.isRequired)
        {
            step.status = TrainingOverallStatus.Incomplete;
            step.summary = "步骤未完成。";
            step.recommendation = "请完成该步骤后再评分。";
        }
        else if (HasCritical(step) || HasDangerEvent(step))
        {
            step.status = TrainingOverallStatus.Failed;
            step.summary = "步骤存在关键阈值问题。";
            step.recommendation = "请优先修正关键问题。";
        }
        else if (step.normalizedScore < NormalizeScore(rubric.passScore))
        {
            step.status = TrainingOverallStatus.Failed;
            step.summary = "Step score is below the pass line.";
            step.recommendation = "Complete the operation and improve this step's details.";
        }
        else if (HasWarning(step))
        {
            step.status = TrainingOverallStatus.PassedWithWarnings;
            step.summary = "步骤完成，但存在警告项。";
            step.recommendation = "建议复查警告指标。";
        }
        else
        {
            step.status = TrainingOverallStatus.Passed;
            step.summary = step.status == TrainingOverallStatus.Passed ? "步骤表现达标。" : "步骤分数未达标。";
            step.recommendation = step.status == TrainingOverallStatus.Passed ? "保持当前操作质量。" : "建议复练该步骤。";
        }
    }

    private static NormalizedMetricRecord NormalizeMetric(ObservedMetricRecord metric, RubricConfig rubric, List<ObservedMetricRecord> stepMetrics)
    {
        MetricDirection direction = InferDirection(metric);
        //旧版算法，不能删去
        //float value = NormalizeByDirection(metric, direction);
        CurveScoreResult curveScore = TryEvaluatePositionCurve(metric, rubric, stepMetrics);
        float value = curveScore.applied ? curveScore.score : NormalizeByDirection(metric, direction);

        float axisScore = 1f;
        if (metric.expectedAxes != AllowedAxes.None)
        {
            axisScore = metric.axisMatched ? Mathf.Clamp01(metric.axisMatchRatio <= 0f ? 1f : metric.axisMatchRatio) : Mathf.Clamp01(metric.axisMatchRatio) * 0.5f;
        }

        NormalizedMetricRecord normalized = new NormalizedMetricRecord();
        normalized.metricId = metric.metricId;
        normalized.stepId = metric.stepId;
        normalized.rawValue = metric.rawValue;
        normalized.normalizedValue = Mathf.Clamp01(value) * axisScore;
        normalized.weight = 1f;
        normalized.weightedScore = normalized.normalizedValue;
        normalized.direction = direction;
        normalized.formula = curveScore.applied ? curveScore.formula : BuildFormulaLabel(direction);
        normalized.note = BuildNormalizedNote(axisScore, curveScore);
        return normalized;
    }

    private static float ComposeStepScore(bool completed, float detailScore, RubricConfig rubric)
    {
        if (!completed)
        {
            return 0f;
        }

        float completionWeight = NormalizeWeight(rubric != null ? rubric.stepCompletionWeight : 0.5f);
        float detailWeight = NormalizeWeight(rubric != null ? rubric.stepDetailWeight : 0.5f);
        float totalWeight = completionWeight + detailWeight;

        if (totalWeight <= 0.0001f)
        {
            completionWeight = 0.5f;
            detailWeight = 0.5f;
            totalWeight = 1f;
        }

        completionWeight /= totalWeight;
        detailWeight /= totalWeight;
        return Mathf.Clamp01(completionWeight + detailWeight * Mathf.Clamp01(detailScore));
    }

    private static float NormalizeWeight(float weight)
    {
        return Mathf.Clamp01(weight > 1f ? weight / 100f : weight);
    }

    private struct CurveScoreResult
    {
        public bool applied;
        public float score;
        public string formula;
        public string note;
    }

    private static CurveScoreResult TryEvaluatePositionCurve(ObservedMetricRecord metric, RubricConfig rubric, List<ObservedMetricRecord> stepMetrics)
    {
        CurveScoreResult result = new CurveScoreResult();
        if (metric == null || rubric == null)
        {
            return result;
        }

        string metricId = metric.metricId ?? string.Empty;
        if (metricId == "s1_midpoint_offset_cm")
        {
            result.applied = true;
            result.score = EvaluateRadialPositionCurve(Mathf.Abs(metric.rawValue), rubric.step1PositionCurve);
            result.formula = "piecewise position curve v" + rubric.positionPenaltyCurveVersion;
            result.note = "Step1 marker midpoint offset curve.";
            return result;
        }

        if (metricId == "s2_center_offset_cm")
        {
            result.applied = true;
            result.score = EvaluateRadialPositionCurve(Mathf.Abs(metric.rawValue), rubric.step2PositionCurve);
            result.formula = "piecewise position curve v" + rubric.positionPenaltyCurveVersion;
            result.note = "Step2 incision center offset curve.";
            return result;
        }

        if (metricId == "s3_center_offset_cm")
        {
            result.applied = true;
            result.score = EvaluateAirwayPositionCurve(metric, rubric.step3PositionCurve, stepMetrics);
            result.formula = "min(lateral, cranio-caudal) position curve v" + rubric.positionPenaltyCurveVersion;
            result.note = "Step3 airway asymmetric curve; radial fallback if split-axis metrics are unavailable.";
            return result;
        }

        if (metricId == "s4_tube_placement_offset_cm")
        {
            result.applied = true;
            result.score = EvaluateRadialPositionCurve(Mathf.Abs(metric.rawValue), rubric.step4PositionCurve);
            result.formula = "piecewise position curve v" + rubric.positionPenaltyCurveVersion;
            result.note = "Step4 tube placement offset curve.";
        }

        return result;
    }

    private static float EvaluateAirwayPositionCurve(ObservedMetricRecord centerMetric, AirwayPositionPenaltyConfig config, List<ObservedMetricRecord> stepMetrics)
    {
        if (config == null)
        {
            config = AirwayPositionPenaltyConfig.CreateDefault();
        }

        ObservedMetricRecord lateralMetric = FindMetric(stepMetrics, "s3_lateral_offset_cm");
        ObservedMetricRecord cranioCaudalMetric = FindMetric(stepMetrics, "s3_cranio_caudal_offset_cm");

        if (lateralMetric != null && cranioCaudalMetric != null)
        {
            float lateralScore = EvaluateRadialPositionCurve(Mathf.Abs(lateralMetric.rawValue), config.lateral);
            PositionPenaltyCurveConfig axialConfig = cranioCaudalMetric.rawValue < 0f ? config.cranial : config.caudal;
            float axialScore = EvaluateRadialPositionCurve(Mathf.Abs(cranioCaudalMetric.rawValue), axialConfig);
            return Mathf.Min(lateralScore, axialScore);
        }

        float centerOffset = centerMetric != null ? Mathf.Abs(centerMetric.rawValue) : 0f;
        float lateralFallback = EvaluateRadialPositionCurve(centerOffset, config.lateral);
        float cranialFallback = EvaluateRadialPositionCurve(centerOffset, config.cranial);
        float caudalFallback = EvaluateRadialPositionCurve(centerOffset, config.caudal);
        return Mathf.Min(lateralFallback, Mathf.Min(cranialFallback, caudalFallback));
    }

    private static ObservedMetricRecord FindMetric(List<ObservedMetricRecord> metrics, string metricId)
    {
        if (metrics == null || string.IsNullOrEmpty(metricId))
        {
            return null;
        }

        for (int i = 0; i < metrics.Count; i++)
        {
            ObservedMetricRecord metric = metrics[i];
            if (metric != null && metric.metricId == metricId)
            {
                return metric;
            }
        }

        return null;
    }

    private static float EvaluateRadialPositionCurve(float offsetCm, PositionPenaltyCurveConfig config)
    {
        if (config == null)
        {
            return 1f;
        }

        float freeCm = Mathf.Max(0f, config.freeCm);
        float softCm = Mathf.Max(freeCm, config.softCm);
        float hardCm = Mathf.Max(softCm, config.hardCm);
        float failCm = Mathf.Max(hardCm, config.failCm);
        float softPenalty = Mathf.Clamp01(config.softPenalty);
        float hardScore = Mathf.Clamp01(config.hardScore);
        float dCm = Mathf.Max(0f, offsetCm);

        if (dCm <= freeCm)
        {
            return 1f;
        }

        if (dCm <= softCm)
        {
            float t = SafeInverseLerp(freeCm, softCm, dCm);
            return Mathf.Clamp01(1f - softPenalty * Mathf.Pow(t, 2f));
        }

        if (dCm <= hardCm)
        {
            float t = SafeInverseLerp(softCm, hardCm, dCm);
            float softScore = 1f - softPenalty;
            return Mathf.Clamp01(softScore - (softScore - hardScore) * Mathf.Pow(t, 2.5f));
        }

        if (dCm <= failCm)
        {
            float t = SafeInverseLerp(hardCm, failCm, dCm);
            return Mathf.Clamp01(hardScore * (1f - Mathf.Pow(t, 2f)));
        }

        return 0f;
    }

    private static float SafeInverseLerp(float min, float max, float value)
    {
        if (Mathf.Approximately(min, max))
        {
            return value <= min ? 0f : 1f;
        }

        return Mathf.Clamp01((value - min) / (max - min));
    }

    private static string BuildNormalizedNote(float axisScore, CurveScoreResult curveScore)
    {
        string note = curveScore.applied ? curveScore.note : string.Empty;
        if (axisScore < 1f)
        {
            note = string.IsNullOrEmpty(note) ? "Axis mismatch penalty applied." : note + " Axis mismatch penalty applied.";
        }

        return note;
    }

    private static void EvaluateThresholds(StepEvaluationResult step)
    {
        for (int i = 0; i < step.thresholdResults.Count; i++)
        {
            ThresholdResult threshold = step.thresholdResults[i];
            if (threshold == null) continue;

            bool minOk = threshold.expectedMin <= 0f || threshold.actualValue >= threshold.expectedMin;
            bool maxOk = threshold.expectedMax <= 0f || threshold.actualValue <= threshold.expectedMax;
            bool axisOk = threshold.expectedAxes == AllowedAxes.None || threshold.axisMatched || threshold.axisMatchScore >= 0.99f;
            bool explicitCritical = threshold.criticalTriggered;
            threshold.passed = minOk && maxOk && axisOk && !explicitCritical;
            threshold.warningTriggered |= !threshold.passed && threshold.warningThreshold > 0f;
            threshold.criticalTriggered = explicitCritical;
            threshold.resultMessage = threshold.passed ? "通过" : "未达到阈值要求";
            threshold.recommendation = threshold.passed ? string.Empty : "请复查该指标并针对性练习。";
        }
    }

    private static List<StepEvaluationResult> GetSessionSteps(TrainingSessionRecord session)
    {
        if (session == null) return new List<StepEvaluationResult>();

        FieldInfo field = typeof(TrainingSessionRecord).GetField("stepResults", BindingFlags.Instance | BindingFlags.Public);
        return field != null && field.GetValue(session) is List<StepEvaluationResult> steps ? steps : new List<StepEvaluationResult>();
    }

    private static TrainingOverallStatus GetOverallStatus(bool passed, bool critical, bool warning, bool incomplete)
    {
        if (incomplete) return TrainingOverallStatus.Incomplete;
        if (critical || !passed) return TrainingOverallStatus.Failed;
        return warning ? TrainingOverallStatus.PassedWithWarnings : TrainingOverallStatus.Passed;
    }

    private static bool IsNonScoring(ObservedMetricRecord metric)
    {
        string text = ((metric.metricId ?? string.Empty) + " " + (metric.displayName ?? string.Empty) + " " + (metric.note ?? string.Empty)).ToLowerInvariant();
        return text.Contains("time")
            || text.Contains("duration")
            || text.Contains("retry")
            || text.Contains("attempt")
            || text.Contains("position-axis-helper")
            || text.Contains("s3_lateral_offset_cm")
            || text.Contains("s3_cranio_caudal_offset_cm");
    }

    private static bool HasWarning(StepEvaluationResult step)
    {
        if (step.issues.Count > 0) return true;

        for (int i = 0; i < step.thresholdResults.Count; i++)
        {
            if (step.thresholdResults[i] != null && step.thresholdResults[i].warningTriggered) return true;
        }

        return false;
    }

    private static bool HasCritical(StepEvaluationResult step)
    {
        for (int i = 0; i < step.thresholdResults.Count; i++)
        {
            if (step.thresholdResults[i] != null && step.thresholdResults[i].criticalTriggered) return true;
        }

        return false;
    }

    private static string ResolveReportTimestamp(TrainingSessionRecord session)
    {
        if (session != null)
        {
            if (!string.IsNullOrWhiteSpace(session.endedAt)) return session.endedAt;
            if (!string.IsNullOrWhiteSpace(session.startedAt)) return session.startedAt;
        }

        return DateTime.UtcNow.ToString("o");
    }

    private static bool HasDangerEvent(StepEvaluationResult step)
    {
        if (step == null || step.relatedEvents == null)
        {
            return false;
        }

        for (int i = 0; i < step.relatedEvents.Count; i++)
        {
            TrainingEventRecord record = step.relatedEvents[i];
            if (record == null)
            {
                continue;
            }

            string severity = (record.severity ?? string.Empty).ToLowerInvariant();
            if (record.isError || severity.Contains("critical") || severity.Contains("danger"))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureLists(StepEvaluationResult step)
    {
        step.observedMetrics = step.observedMetrics ?? new List<ObservedMetricRecord>();
        step.normalizedMetrics = step.normalizedMetrics ?? new List<NormalizedMetricRecord>();
        step.thresholdResults = step.thresholdResults ?? new List<ThresholdResult>();
        step.issues = step.issues ?? new List<string>();
    }

    private static void AddRange<T>(List<T> target, List<T> source)
    {
        if (target != null && source != null) target.AddRange(source);
    }

    private static float NormalizeScore(float score)
    {
        return Mathf.Clamp01(score > 1f ? score / 100f : score);
    }

    private static MetricDirection InferDirection(ObservedMetricRecord metric)
    {
        string text = ((metric.metricId ?? string.Empty) + " " + (metric.displayName ?? string.Empty) + " " + (metric.note ?? string.Empty)).ToLowerInvariant();

        if (text.Contains("offset") || text.Contains("error"))
        {
            return MetricDirection.LowerIsBetter;
        }

        if (text.Contains("length") && metric.minObservedValue > 0f && metric.maxObservedValue > metric.minObservedValue)
        {
            return MetricDirection.WithinRangeIsBetter;
        }

        if (text.Contains("duration") || text.Contains("hold") || text.Contains("success") || text.Contains("efficiency"))
        {
            return MetricDirection.HigherIsBetter;
        }

        if (metric.targetValue > 0f)
        {
            return metric.minObservedValue > 0f || metric.maxObservedValue > metric.targetValue
                ? MetricDirection.WithinRangeIsBetter
                : MetricDirection.CloserToTargetIsBetter;
        }

        return MetricDirection.HigherIsBetter;
    }

    private static float NormalizeByDirection(ObservedMetricRecord metric, MetricDirection direction)
    {
        switch (direction)
        {
            case MetricDirection.LowerIsBetter:
            {
                //旧版算法，不能删去
                //float max = metric.targetValue > 0f ? metric.targetValue : metric.maxObservedValue;
                //if (max <= 0f)
                //{
                //    max = 1f;
                //}
                //
                //return 1f - Mathf.Clamp01(metric.rawValue / max);

                float max = metric.targetValue > 0f ? metric.targetValue : metric.maxObservedValue;
                if (max <= 0f)
                {
                    max = 1f;
                }

                return 1f - Mathf.Clamp01(metric.rawValue / max);
            }

            case MetricDirection.CloserToTargetIsBetter:
            {
                float target = Mathf.Abs(metric.targetValue);
                if (target <= 0f)
                {
                    return metric.rawValue > 1f ? Mathf.Clamp01(metric.rawValue / 100f) : Mathf.Clamp01(metric.rawValue);
                }

                return 1f - Mathf.Clamp01(Mathf.Abs(metric.rawValue - metric.targetValue) / target);
            }

            case MetricDirection.WithinRangeIsBetter:
            {
                float min = metric.minObservedValue;
                float max = metric.maxObservedValue;

                if (max <= min)
                {
                    max = Mathf.Max(min + 0.0001f, metric.targetValue);
                }

                if (metric.rawValue >= min && metric.rawValue <= max)
                {
                    return 1f;
                }

                float target = metric.targetValue > 0f ? metric.targetValue : (min + max) * 0.5f;
                float penaltyRange = metric.rawValue < min ? Mathf.Max(target - min, 0.0001f) : Mathf.Max(max - target, 0.0001f);
                return 1f - Mathf.Clamp01(Mathf.Abs(metric.rawValue - target) / (penaltyRange + Mathf.Abs(metric.rawValue - target)));
            }

            case MetricDirection.HigherIsBetter:
            default:
            {
                if (metric.targetValue > 0f)
                {
                    return Mathf.Clamp01(metric.rawValue / metric.targetValue);
                }

                return metric.rawValue > 1f ? Mathf.Clamp01(metric.rawValue / 100f) : Mathf.Clamp01(metric.rawValue);
            }
        }
    }

    private static string BuildFormulaLabel(MetricDirection direction)
    {
        switch (direction)
        {
            case MetricDirection.LowerIsBetter:
                return "1 - clamp(raw/max)";
            case MetricDirection.CloserToTargetIsBetter:
                return "1 - clamp(abs(raw-target)/target)";
            case MetricDirection.WithinRangeIsBetter:
                return "inside range => 1 else distance penalty";
            case MetricDirection.HigherIsBetter:
                return "clamp(raw/target)";
            default:
                return "clamp(raw)";
        }
    }
}
