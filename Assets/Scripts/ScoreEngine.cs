using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ScoreEngine
{
    public static TrainingEvaluationReport Evaluate(TrainingSessionRecord session, RubricConfig config)
    {
        RubricConfig rubric = config ?? RubricConfig.CreateDefault();
        TrainingEvaluationReport report = new TrainingEvaluationReport();
        report.reportId = Guid.NewGuid().ToString("N");
        report.reportGeneratedAt = DateTime.UtcNow.ToString("o");
        report.session = session ?? new TrainingSessionRecord();

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

            EvaluateStep(step);
            report.stepResults.Add(step);
            total += Mathf.Clamp01(step.normalizedScore);
            counted++;

            AddRange(report.sessionObservedMetrics, step.observedMetrics);
            AddRange(report.sessionNormalizedMetrics, step.normalizedMetrics);
            AddRange(report.sessionThresholdResults, step.thresholdResults);

            hasCritical |= HasCritical(step);
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

    private static void EvaluateStep(StepEvaluationResult step)
    {
        EnsureLists(step);
        step.normalizedMetrics.Clear();

        float total = 0f;
        int counted = 0;

        for (int i = 0; i < step.observedMetrics.Count; i++)
        {
            ObservedMetricRecord metric = step.observedMetrics[i];
            if (metric == null || IsNonScoring(metric)) continue;

            NormalizedMetricRecord normalized = NormalizeMetric(metric);
            step.normalizedMetrics.Add(normalized);
            total += Mathf.Clamp01(normalized.weightedScore);
            counted++;
        }

        step.normalizedScore = counted > 0 ? total / counted : (step.completed ? 1f : 0f);
        step.rawScore = step.normalizedScore;
        EvaluateThresholds(step);

        if (!step.completed && step.isRequired)
        {
            step.status = TrainingOverallStatus.Incomplete;
            step.summary = "步骤未完成。";
            step.recommendation = "请完成该步骤后再评分。";
        }
        else if (HasCritical(step))
        {
            step.status = TrainingOverallStatus.Failed;
            step.summary = "步骤存在关键阈值问题。";
            step.recommendation = "请优先修正关键问题。";
        }
        else if (HasWarning(step))
        {
            step.status = TrainingOverallStatus.PassedWithWarnings;
            step.summary = "步骤完成，但存在警告项。";
            step.recommendation = "建议复查警告指标。";
        }
        else
        {
            step.status = step.normalizedScore >= 0.8f ? TrainingOverallStatus.Passed : TrainingOverallStatus.Failed;
            step.summary = step.status == TrainingOverallStatus.Passed ? "步骤表现达标。" : "步骤分数未达标。";
            step.recommendation = step.status == TrainingOverallStatus.Passed ? "保持当前操作质量。" : "建议复练该步骤。";
        }
    }

    private static NormalizedMetricRecord NormalizeMetric(ObservedMetricRecord metric)
    {
        MetricDirection direction = InferDirection(metric);
        float value = NormalizeByDirection(metric, direction);

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
        normalized.formula = BuildFormulaLabel(direction);
        normalized.note = axisScore < 1f ? "Axis mismatch penalty applied." : string.Empty;
        return normalized;
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
            threshold.passed = minOk && maxOk && axisOk && !threshold.criticalTriggered;
            threshold.warningTriggered |= !threshold.passed && threshold.warningThreshold > 0f;
            threshold.criticalTriggered |= !threshold.passed && threshold.criticalThreshold > 0f;
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
        return text.Contains("time") || text.Contains("duration") || text.Contains("retry") || text.Contains("attempt");
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
