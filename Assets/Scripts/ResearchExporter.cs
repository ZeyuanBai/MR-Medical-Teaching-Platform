using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class ResearchExporter
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    public static string[] ExportAll(TrainingEvaluationReport report)
    {
        TrainingEvaluationReport safeReport = PrepareReport(report);
        string basePath = BuildBasePath(safeReport);
        string jsonPath = WriteJson(safeReport, basePath);
        string csvPath = WriteCsv(safeReport, basePath);
        string mappingPath = WriteFieldMappings(safeReport, basePath);
        return new[] { jsonPath, csvPath, mappingPath };
    }

    public static string ExportJson(TrainingEvaluationReport report)
    {
        TrainingEvaluationReport safeReport = PrepareReport(report);
        return WriteJson(safeReport, BuildBasePath(safeReport));
    }

    public static string ExportCsv(TrainingEvaluationReport report)
    {
        TrainingEvaluationReport safeReport = PrepareReport(report);
        string basePath = BuildBasePath(safeReport);
        string csvPath = WriteCsv(safeReport, basePath);
        WriteFieldMappings(safeReport, basePath);
        return csvPath;
    }

    private static TrainingEvaluationReport PrepareReport(TrainingEvaluationReport report)
    {
        TrainingEvaluationReport safe = report ?? new TrainingEvaluationReport();
        safe.session = safe.session ?? new TrainingSessionRecord();
        safe.stepResults = safe.stepResults ?? new List<StepEvaluationResult>();
        safe.exportMappings = safe.exportMappings ?? new List<FieldMappingEntry>();
        safe.reportId = FirstNonEmpty(safe.reportId, safe.session.sessionId, "training_report");
        safe.reportGeneratedAt = FirstNonEmpty(safe.reportGeneratedAt, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        safe.exportedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        safe.exporterVersion = FirstNonEmpty(safe.exporterVersion, "1.0.0");
        safe.reportVersion = FirstNonEmpty(safe.reportVersion, safe.session.schemaVersion, "1.0.0");
        return safe;
    }

    private static string BuildBasePath(TrainingEvaluationReport report)
    {
        string dir = Path.Combine(Application.persistentDataPath, "TrainingReports");
        Directory.CreateDirectory(dir);
        string name = SanitizeFileName(report.reportId);
        string stampSource = FirstNonEmpty(report.reportGeneratedAt, report.session != null ? report.session.endedAt : string.Empty, report.session != null ? report.session.startedAt : string.Empty);
        string stamp = TryFormatStableTimestamp(stampSource, out string parsedStamp)
            ? parsedStamp
            : DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        return Path.Combine(dir, name + "_" + stamp);
    }

    private static string WriteJson(TrainingEvaluationReport report, string basePath)
    {
        report.exportFormat = "json";
        string path = basePath + ".json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true), Utf8NoBom);
        return path;
    }

    private static string WriteCsv(TrainingEvaluationReport report, string basePath)
    {
        report.exportFormat = "csv";
        string path = basePath + ".csv";
        File.WriteAllText(path, BuildCsv(report), Utf8NoBom);
        return path;
    }

    private static string WriteFieldMappings(TrainingEvaluationReport report, string basePath)
    {
        string path = basePath + "_field_mapping.csv";
        File.WriteAllText(path, BuildFieldMappingCsv(report), Utf8NoBom);
        return path;
    }

    private static string BuildCsv(TrainingEvaluationReport report)
    {
        TrainingSessionRecord session = report.session ?? new TrainingSessionRecord();
        string algorithmVersion = FirstNonEmpty(report.exporterVersion, session.applicationVersion, "1.0.0");
        string rubricVersion = FirstNonEmpty(report.reportVersion, session.schemaVersion, "1.0.0");
        string scenarioVersion = FirstNonEmpty(session.contentVersion, session.scenarioName, "default");
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(Row("recordType", "reportId", "sessionId", "stepId", "stepName", "metricId", "thresholdId", "eventId", "fieldKey", "algorithmVersion", "rubricVersion", "scenarioVersion", "status", "score", "normalizedScore", "rawValue", "weightedScore", "passed", "completed", "durationSeconds", "unit", "timestampSeconds", "description", "summary"));
        sb.AppendLine(Row(
            "session",
            report.reportId,
            session.sessionId,
            string.Empty,
            session.scenarioName,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            algorithmVersion,
            rubricVersion,
            scenarioVersion,
            report.overallStatus.ToString(),
            F(report.overallScore),
            F(report.normalizedOverallScore),
            string.Empty,
            string.Empty,
            B(report.passed),
            string.Empty,
            F(session.durationSeconds),
            string.Empty,
            string.Empty,
            "Training session summary",
            report.overallSummary));

        foreach (StepEvaluationResult step in report.stepResults)
        {
            if (step == null)
            {
                continue;
            }

            sb.AppendLine(Row(
                "step",
                report.reportId,
                session.sessionId,
                step.stepId,
                step.stepName,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                algorithmVersion,
                rubricVersion,
                scenarioVersion,
                step.status.ToString(),
                F(step.rawScore),
                F(step.normalizedScore),
                string.Empty,
                string.Empty,
                string.Empty,
                B(step.completed),
                F(step.durationSeconds),
                string.Empty,
                string.Empty,
                "Step score summary",
                step.summary));

            AppendObservedMetricRows(sb, report, session, step, algorithmVersion, rubricVersion, scenarioVersion);
            AppendNormalizedMetricRows(sb, report, session, step, algorithmVersion, rubricVersion, scenarioVersion);
            AppendThresholdRows(sb, report, session, step, algorithmVersion, rubricVersion, scenarioVersion);
        }

        AppendEventRows(sb, report, session, algorithmVersion, rubricVersion, scenarioVersion);
        AppendDefinitionRows(sb, report, session, algorithmVersion, rubricVersion, scenarioVersion);

        return sb.ToString();
    }

    private static void AppendObservedMetricRows(StringBuilder sb, TrainingEvaluationReport report, TrainingSessionRecord session, StepEvaluationResult step, string algorithmVersion, string rubricVersion, string scenarioVersion)
    {
        if (step.observedMetrics == null) return;

        for (int i = 0; i < step.observedMetrics.Count; i++)
        {
            ObservedMetricRecord metric = step.observedMetrics[i];
            if (metric == null) continue;

            sb.AppendLine(Row(
                "raw_metric",
                report.reportId,
                session.sessionId,
                step.stepId,
                step.stepName,
                metric.metricId,
                string.Empty,
                string.Empty,
                string.Empty,
                algorithmVersion,
                rubricVersion,
                scenarioVersion,
                metric.isValid ? "Valid" : "Invalid",
                string.Empty,
                string.Empty,
                F(metric.rawValue),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                metric.unit,
                F(metric.timestampSeconds),
                metric.displayName,
                metric.note));
        }
    }

    private static void AppendNormalizedMetricRows(StringBuilder sb, TrainingEvaluationReport report, TrainingSessionRecord session, StepEvaluationResult step, string algorithmVersion, string rubricVersion, string scenarioVersion)
    {
        if (step.normalizedMetrics == null) return;

        for (int i = 0; i < step.normalizedMetrics.Count; i++)
        {
            NormalizedMetricRecord metric = step.normalizedMetrics[i];
            if (metric == null) continue;

            sb.AppendLine(Row(
                "normalized_metric",
                report.reportId,
                session.sessionId,
                step.stepId,
                step.stepName,
                metric.metricId,
                string.Empty,
                string.Empty,
                string.Empty,
                algorithmVersion,
                rubricVersion,
                scenarioVersion,
                metric.direction.ToString(),
                string.Empty,
                F(metric.normalizedValue),
                F(metric.rawValue),
                F(metric.weightedScore),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                metric.formula,
                metric.note));
        }
    }

    private static void AppendThresholdRows(StringBuilder sb, TrainingEvaluationReport report, TrainingSessionRecord session, StepEvaluationResult step, string algorithmVersion, string rubricVersion, string scenarioVersion)
    {
        if (step.thresholdResults == null) return;

        for (int i = 0; i < step.thresholdResults.Count; i++)
        {
            ThresholdResult threshold = step.thresholdResults[i];
            if (threshold == null) continue;

            sb.AppendLine(Row(
                "threshold",
                report.reportId,
                session.sessionId,
                step.stepId,
                step.stepName,
                threshold.metricId,
                threshold.thresholdId,
                string.Empty,
                string.Empty,
                algorithmVersion,
                rubricVersion,
                scenarioVersion,
                threshold.resultMessage,
                string.Empty,
                string.Empty,
                F(threshold.actualValue),
                string.Empty,
                B(threshold.passed),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                threshold.label,
                threshold.recommendation));
        }
    }

    private static void AppendEventRows(StringBuilder sb, TrainingEvaluationReport report, TrainingSessionRecord session, string algorithmVersion, string rubricVersion, string scenarioVersion)
    {
        if (report.sessionEvents == null) return;

        for (int i = 0; i < report.sessionEvents.Count; i++)
        {
            TrainingEventRecord record = report.sessionEvents[i];
            if (record == null) continue;

            sb.AppendLine(Row(
                "event",
                report.reportId,
                session.sessionId,
                record.stepId,
                string.Empty,
                string.Empty,
                string.Empty,
                record.eventId,
                string.Empty,
                algorithmVersion,
                rubricVersion,
                scenarioVersion,
                record.eventType.ToString(),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                record.isError ? "false" : string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                F(record.timestampSeconds),
                record.title,
                record.message));
        }
    }

    private static void AppendDefinitionRows(StringBuilder sb, TrainingEvaluationReport report, TrainingSessionRecord session, string algorithmVersion, string rubricVersion, string scenarioVersion)
    {
        if (report.metricDefinitions != null)
        {
            for (int i = 0; i < report.metricDefinitions.Count; i++)
            {
                MetricDefinition definition = report.metricDefinitions[i];
                if (definition == null) continue;

                sb.AppendLine(Row(
                    "metric_definition",
                    report.reportId,
                    session.sessionId,
                    definition.sourceStepId,
                    string.Empty,
                    definition.metricId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    algorithmVersion,
                    rubricVersion,
                    scenarioVersion,
                    definition.direction.ToString(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    definition.unit,
                    string.Empty,
                    definition.displayName,
                    FirstNonEmpty(definition.description, definition.notes)));
            }
        }

        if (report.exportMappings != null)
        {
            for (int i = 0; i < report.exportMappings.Count; i++)
            {
                FieldMappingEntry mapping = report.exportMappings[i];
                if (mapping == null) continue;

                sb.AppendLine(Row(
                    "field_definition",
                    report.reportId,
                    session.sessionId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    mapping.exportKey,
                    algorithmVersion,
                    rubricVersion,
                    scenarioVersion,
                    mapping.valueType,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    mapping.unit,
                    string.Empty,
                    mapping.sourceFieldPath,
                    mapping.description));
            }
        }
    }

    private static string BuildFieldMappingCsv(TrainingEvaluationReport report)
    {
        List<FieldMappingEntry> mappings = new List<FieldMappingEntry>
        {
            Map("algorithmVersion", "Algorithm Version", "report.exporterVersion | report.session.applicationVersion", "string", "Algorithm/export version"),
            Map("rubricVersion", "Rubric Version", "report.reportVersion | report.session.schemaVersion", "string", "Rubric/schema version"),
            Map("scenarioVersion", "Scenario Version", "report.session.contentVersion | report.session.scenarioName", "string", "Scenario/content version"),
            Map("sessionId", "Session Id", "report.session.sessionId", "string", "Training session identifier"),
            Map("overallScore", "Overall Score", "report.overallScore", "float", "Overall session score"),
            Map("stepSummary", "Step Summary", "report.stepResults[].summary", "string", "Per-step summary text")
        };

        foreach (FieldMappingEntry entry in report.exportMappings)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.exportKey))
            {
                continue;
            }

            if (!ContainsKey(mappings, entry.exportKey))
            {
                mappings.Add(entry);
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Row("exportKey", "exportLabel", "sourceFieldPath", "valueType", "description"));
        foreach (FieldMappingEntry entry in mappings)
        {
            sb.AppendLine(Row(entry.exportKey, entry.exportLabel, entry.sourceFieldPath, entry.valueType, entry.description));
        }
        return sb.ToString();
    }

    private static FieldMappingEntry Map(string key, string label, string path, string valueType, string description)
    {
        return new FieldMappingEntry
        {
            exportKey = key,
            exportLabel = label,
            sourceFieldPath = path,
            valueType = valueType,
            description = description
        };
    }

    private static bool ContainsKey(List<FieldMappingEntry> mappings, string key)
    {
        foreach (FieldMappingEntry entry in mappings)
        {
            if (string.Equals(entry.exportKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string Row(params string[] values)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(Escape(values[i]));
        }
        return sb.ToString();
    }

    private static string Escape(string value)
    {
        string text = value ?? string.Empty;
        if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return text;
        }
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string B(bool value)
    {
        return value ? "true" : "false";
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return string.Empty;
    }

    private static string SanitizeFileName(string value)
    {
        string source = string.IsNullOrWhiteSpace(value) ? "training_report" : value.Trim();
        char[] invalid = Path.GetInvalidFileNameChars();
        StringBuilder sb = new StringBuilder(source.Length);
        foreach (char c in source)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }
        return sb.ToString();
    }

    private static bool TryFormatStableTimestamp(string value, out string stamp)
    {
        stamp = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsed))
        {
            return false;
        }

        stamp = parsed.ToUniversalTime().ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        return true;
    }
}
