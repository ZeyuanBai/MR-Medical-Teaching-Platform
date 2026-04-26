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
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
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

        sb.AppendLine(Row("recordType", "reportId", "sessionId", "stepId", "stepName", "algorithmVersion", "rubricVersion", "scenarioVersion", "status", "score", "normalizedScore", "passed", "completed", "durationSeconds", "summary"));
        sb.AppendLine(Row(
            "session",
            report.reportId,
            session.sessionId,
            string.Empty,
            session.scenarioName,
            algorithmVersion,
            rubricVersion,
            scenarioVersion,
            report.overallStatus.ToString(),
            F(report.overallScore),
            F(report.normalizedOverallScore),
            B(report.passed),
            string.Empty,
            F(session.durationSeconds),
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
                algorithmVersion,
                rubricVersion,
                scenarioVersion,
                step.status.ToString(),
                F(step.rawScore),
                F(step.normalizedScore),
                string.Empty,
                B(step.completed),
                F(step.durationSeconds),
                step.summary));
        }

        return sb.ToString();
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
}
