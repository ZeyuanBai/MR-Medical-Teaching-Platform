using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum AllowedAxes
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Both = 3
}

[Serializable]
public enum TrainingEventType
{
    Unknown = 0,
    SessionStarted,
    SessionPaused,
    SessionResumed,
    SessionCompleted,
    SessionCancelled,
    StepStarted,
    StepCompleted,
    StepFailed,
    ToolPickedUp,
    ToolReleased,
    ToolPoseCaptured,
    MetricCaptured,
    ThresholdEvaluated,
    WarningRaised,
    NoteAdded
}

[Serializable]
public enum TrainingOverallStatus
{
    NotStarted = 0,
    InProgress,
    Passed,
    PassedWithWarnings,
    Failed,
    Incomplete,
    Cancelled
}

[Serializable]
public enum MetricDirection
{
    None = 0,
    HigherIsBetter,
    LowerIsBetter,
    CloserToTargetIsBetter,
    WithinRangeIsBetter
}

[Serializable]
public enum TrackedToolType
{
    Unknown = 0,
    Marker,
    Scalpel,
    Forceps,
    TrachealTube,
    Suction,
    Hand,
    Other
}

[Serializable]
public class MetricDefinition
{
    public string metricId;
    public string displayName;
    public string description;
    public string unit;
    public MetricDirection direction = MetricDirection.None;
    public float targetValue;
    public float minAcceptableValue;
    public float maxAcceptableValue;
    public float warningThreshold;
    public float criticalThreshold;
    public float normalizationMin;
    public float normalizationMax = 1f;
    public AllowedAxes allowedAxes = AllowedAxes.None;
    public bool requiresPathAxisEvaluation;
    public bool allowIntermediateAxisMix = true;
    public float axisAlignmentScoreWeight = 1f;
    public string sourceStepId;
    public string sourceFieldPath;
    public string aggregationMethod;
    public string notes;
}

[Serializable]
public class FieldMappingEntry
{
    public string exportKey;
    public string exportLabel;
    public string sourceFieldPath;
    public string sourceCollection;
    public string valueType;
    public string unit;
    public string formatHint;
    public string exampleValue;
    public string description;
    public bool includeInSummary = true;
}

[Serializable]
public class PoseSample
{
    public string sampleId;
    public string stepId;
    public TrackedToolType toolType = TrackedToolType.Unknown;
    public string toolInstanceId;
    public string referenceFrame;
    public float timestampSeconds;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
    public Vector3 linearVelocity;
    public Vector3 angularVelocity;
    public AllowedAxes constrainedAxes = AllowedAxes.None;
    public AllowedAxes observedAxes = AllowedAxes.None;
    public float axisAlignmentScore;
    public bool axisMatched = true;
    public bool isInsideTargetRegion;
    public string note;
}

[Serializable]
public class TrainingEventRecord
{
    public string eventId;
    public string sessionId;
    public string stepId;
    public TrainingEventType eventType = TrainingEventType.Unknown;
    public TrackedToolType toolType = TrackedToolType.Unknown;
    public string sourceObjectId;
    public string title;
    public string message;
    public string severity;
    public float timestampSeconds;
    public Vector3 worldPosition;
    public Quaternion worldRotation = Quaternion.identity;
    public bool isError;
    public string payloadJson;
}

[Serializable]
public class ObservedMetricRecord
{
    public string metricId;
    public string stepId;
    public string displayName;
    public string unit;
    public float timestampSeconds;
    public float rawValue;
    public float targetValue;
    public float minObservedValue;
    public float maxObservedValue;
    public float averageObservedValue;
    public int sampleCount;
    public AllowedAxes expectedAxes = AllowedAxes.None;
    public AllowedAxes observedAxes = AllowedAxes.None;
    public float axisMatchRatio;
    public bool axisMatched = true;
    public bool isValid = true;
    public string source;
    public string note;
}

[Serializable]
public class NormalizedMetricRecord
{
    public string metricId;
    public string stepId;
    public float rawValue;
    public float normalizedValue;
    public float weightedScore;
    public float weight = 1f;
    public float normalizationMin;
    public float normalizationMax = 1f;
    public MetricDirection direction = MetricDirection.None;
    public bool isClamped;
    public string formula;
    public string note;
}

[Serializable]
public class ThresholdResult
{
    public string thresholdId;
    public string metricId;
    public string stepId;
    public string label;
    public float actualValue;
    public float expectedMin;
    public float expectedMax;
    public float warningThreshold;
    public float criticalThreshold;
    public AllowedAxes expectedAxes = AllowedAxes.None;
    public AllowedAxes observedAxes = AllowedAxes.None;
    public float axisMatchScore;
    public bool passed;
    public bool warningTriggered;
    public bool criticalTriggered;
    public bool axisMatched = true;
    public string resultMessage;
    public string recommendation;
}

[Serializable]
public class StepEvaluationResult
{
    public string stepId;
    public string stepName;
    public int stepIndex;
    public TrainingOverallStatus status = TrainingOverallStatus.NotStarted;
    public float startTimeSeconds;
    public float endTimeSeconds;
    public float durationSeconds;
    public float completionRatio;
    public float rawScore;
    public float normalizedScore;
    public bool completed;
    public bool isRequired = true;
    public string summary;
    public string feedback;
    public string recommendation;
    public List<string> strengths = new List<string>();
    public List<string> issues = new List<string>();
    public List<string> suggestedActions = new List<string>();
    public List<ObservedMetricRecord> observedMetrics = new List<ObservedMetricRecord>();
    public List<NormalizedMetricRecord> normalizedMetrics = new List<NormalizedMetricRecord>();
    public List<ThresholdResult> thresholdResults = new List<ThresholdResult>();
    public List<TrainingEventRecord> relatedEvents = new List<TrainingEventRecord>();
    public List<PoseSample> poseSamples = new List<PoseSample>();
}

[Serializable]
public class TrainingSessionRecord
{
    public string sessionId;
    public string traineeId;
    public string traineeName;
    public string evaluatorId;
    public string evaluatorName;
    public string scenarioId;
    public string scenarioName;
    public string caseId;
    public string caseName;
    public string trainingMode;
    public string deviceModel;
    public string applicationVersion;
    public string contentVersion;
    public string schemaVersion = "1.0.0";
    public string locale = "zh-CN";
    public string environmentName;
    public string sceneName;
    public string sessionDate;
    public string startedAt;
    public string endedAt;
    public float durationSeconds;
    public TrainingOverallStatus overallStatus = TrainingOverallStatus.NotStarted;
    public float overallScore;
    public float passingScore;
    public bool submitted;
    public string instructorComment;
    public string summary;
    public List<TrackedToolType> trackedTools = new List<TrackedToolType>();
    public List<string> tags = new List<string>();
    public List<StepEvaluationResult> stepResults = new List<StepEvaluationResult>();
}

[Serializable]
public class TrainingEvaluationReport
{
    public string reportId;
    public string reportVersion = "1.0.0";
    public string reportGeneratedAt;
    public string exportedAt;
    public string exporterVersion = "1.0.0";
    public string exportFormat = "json";
    public string exportDescription;
    public TrainingSessionRecord session = new TrainingSessionRecord();
    public TrainingOverallStatus overallStatus = TrainingOverallStatus.NotStarted;
    public float overallScore;
    public float normalizedOverallScore;
    public bool passed;
    public string overallSummary;
    public string overallRecommendation;
    public List<string> recommendations = new List<string>();
    public List<string> warnings = new List<string>();
    public List<MetricDefinition> metricDefinitions = new List<MetricDefinition>();
    public List<FieldMappingEntry> exportMappings = new List<FieldMappingEntry>();
    public List<ObservedMetricRecord> sessionObservedMetrics = new List<ObservedMetricRecord>();
    public List<NormalizedMetricRecord> sessionNormalizedMetrics = new List<NormalizedMetricRecord>();
    public List<ThresholdResult> sessionThresholdResults = new List<ThresholdResult>();
    public List<TrainingEventRecord> sessionEvents = new List<TrainingEventRecord>();
    public List<StepEvaluationResult> stepResults = new List<StepEvaluationResult>();
}
