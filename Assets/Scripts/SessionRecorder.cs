using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionRecorder : MonoBehaviour
{
    public RubricConfig config;

    public TrainingSessionRecord CurrentSession { get; private set; }
    public TrainingEvaluationReport CurrentReport { get; private set; }
    public StepEvaluationResult CurrentStepResult { get; private set; }

    private readonly List<StepEvaluationResult> _capturedSteps = new List<StepEvaluationResult>();
    private readonly List<TrainingEventRecord> _timelineEvents = new List<TrainingEventRecord>();
    private readonly Dictionary<string, float> _lastPoseCaptureTimes = new Dictionary<string, float>();
    private float _sessionStartClock;

    private void Awake()
    {
        if (config == null) config = RubricConfig.CreateDefault();
    }

    public void BeginSession(string scenarioName)
    {
        if (config == null) config = RubricConfig.CreateDefault();

        _capturedSteps.Clear();
        _timelineEvents.Clear();
        _lastPoseCaptureTimes.Clear();
        CurrentStepResult = null;
        _sessionStartClock = Time.unscaledTime;

        string startedAt = DateTime.UtcNow.ToString("o");
        string resolvedScenario = string.IsNullOrEmpty(scenarioName) ? "Unnamed Scenario" : scenarioName;

        CurrentSession = new TrainingSessionRecord
        {
            sessionId = Guid.NewGuid().ToString("N"),
            scenarioId = resolvedScenario,
            scenarioName = resolvedScenario,
            startedAt = startedAt,
            durationSeconds = 0f,
            overallStatus = TrainingOverallStatus.InProgress,
            overallScore = 0f,
            passingScore = config.passScore,
            sceneName = SceneManager.GetActiveScene().name,
            applicationVersion = Application.version,
            contentVersion = config.scenarioVersion,
            schemaVersion = config.algorithmVersion,
            sessionDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            summary = string.Empty
        };

        CurrentReport = new TrainingEvaluationReport
        {
            reportId = Guid.NewGuid().ToString("N"),
            reportGeneratedAt = startedAt,
            exporterVersion = config.algorithmVersion,
            session = CurrentSession,
            overallStatus = TrainingOverallStatus.InProgress,
            overallScore = 0f,
            normalizedOverallScore = 0f,
            passed = false,
            overallSummary = string.Empty
        };

        AppendTimelineEventInternal(string.Empty, TrainingEventType.SessionStarted, "Session Started", resolvedScenario, false);
        RefreshReportSnapshotInternal();
    }

    public void ResetRecorder()
    {
        _capturedSteps.Clear();
        _timelineEvents.Clear();
        _lastPoseCaptureTimes.Clear();
        CurrentStepResult = null;
        CurrentSession = null;
        CurrentReport = null;
        _sessionStartClock = 0f;
    }

    public void StartStep(string stepId, string stepName, int stepIndex)
    {
        EnsureRecorderReadyInternal();

        string resolvedId = string.IsNullOrEmpty(stepId) ? "step-" + stepIndex : stepId;
        CurrentStepResult = new StepEvaluationResult
        {
            stepId = resolvedId,
            stepName = string.IsNullOrEmpty(stepName) ? resolvedId : stepName,
            stepIndex = stepIndex,
            status = TrainingOverallStatus.InProgress,
            startTimeSeconds = ComputeElapsedSessionSecondsInternal()
        };

        AppendTimelineEventInternal(resolvedId, TrainingEventType.StepStarted, "Step Started", CurrentStepResult.stepName, false);
        RefreshReportSnapshotInternal();
    }

    public void CompleteStep(StepEvaluationResult stepResult)
    {
        EnsureRecorderReadyInternal();
        if (stepResult == null) return;

        StepEvaluationResult resolved = CurrentStepResult != null && CurrentStepResult.stepId == stepResult.stepId
            ? CurrentStepResult
            : stepResult;

        if (ReferenceEquals(resolved, CurrentStepResult))
        {
            CopyIncomingStepPayloadInternal(stepResult, resolved);
        }

        if (resolved.startTimeSeconds <= 0f && stepResult.startTimeSeconds > 0f)
        {
            resolved.startTimeSeconds = stepResult.startTimeSeconds;
        }

        if (resolved.endTimeSeconds <= 0f)
        {
            resolved.endTimeSeconds = stepResult.endTimeSeconds > 0f
                ? stepResult.endTimeSeconds
                : ComputeElapsedSessionSecondsInternal();
        }

        resolved.durationSeconds = Mathf.Max(0f, resolved.endTimeSeconds - resolved.startTimeSeconds);
        resolved.completionRatio = resolved.completionRatio > 0f ? resolved.completionRatio : 1f;
        resolved.completed = true;

        if (resolved.status == TrainingOverallStatus.NotStarted || resolved.status == TrainingOverallStatus.InProgress)
        {
            resolved.status = TrainingOverallStatus.Passed;
        }

        int existingIndex = FindTrackedStepIndexInternal(resolved.stepId);
        if (existingIndex >= 0) _capturedSteps[existingIndex] = resolved;
        else _capturedSteps.Add(resolved);

        AppendTimelineEventInternal(resolved.stepId, TrainingEventType.StepCompleted, "Step Completed", resolved.stepName, false);
        CurrentStepResult = null;
        RefreshReportSnapshotInternal();
    }

    public void RecordRetry(string stepId, string reason)
    {
        EnsureRecorderReadyInternal();

        StepEvaluationResult step = LocateTrackedStepInternal(stepId);
        string message = string.IsNullOrEmpty(reason) ? "Retry requested." : reason;

        if (step != null) step.issues.Add(message);
        AppendTimelineEventInternal(step != null ? step.stepId : stepId, TrainingEventType.WarningRaised, "Step Retry", message, false);
        RefreshReportSnapshotInternal();
    }

    public void RecordException(string stepId, string message)
    {
        EnsureRecorderReadyInternal();

        StepEvaluationResult step = LocateTrackedStepInternal(stepId);
        string content = string.IsNullOrEmpty(message) ? "Unknown exception." : message;

        if (step != null)
        {
            step.issues.Add(content);
            step.status = TrainingOverallStatus.Failed;
        }

        AppendTimelineEventInternal(step != null ? step.stepId : stepId, TrainingEventType.WarningRaised, "Step Exception", content, true);
        RefreshReportSnapshotInternal();
    }

    public void CompleteSession()
    {
        EnsureRecorderReadyInternal();

        if (CurrentStepResult != null && !CurrentStepResult.completed)
        {
            CurrentStepResult.endTimeSeconds = ComputeElapsedSessionSecondsInternal();
            CurrentStepResult.durationSeconds = Mathf.Max(0f, CurrentStepResult.endTimeSeconds - CurrentStepResult.startTimeSeconds);
            CurrentStepResult.status = TrainingOverallStatus.Incomplete;

            if (FindTrackedStepIndexInternal(CurrentStepResult.stepId) < 0)
            {
                _capturedSteps.Add(CurrentStepResult);
            }
        }

        CurrentSession.endedAt = DateTime.UtcNow.ToString("o");
        CurrentSession.durationSeconds = ComputeElapsedSessionSecondsInternal();
        CurrentSession.overallScore = ComputeAverageStepScoreInternal();
        CurrentSession.overallStatus = ResolveOverallStatusInternal();
        CurrentSession.summary = ComposeSessionSummaryInternal();

        AppendTimelineEventInternal(string.Empty, TrainingEventType.SessionCompleted, "Session Completed", CurrentSession.summary, false);

        CurrentReport.session = CurrentSession;
        CurrentReport.overallStatus = CurrentSession.overallStatus;
        CurrentReport.overallScore = CurrentSession.overallScore;
        CurrentReport.normalizedOverallScore = CurrentSession.overallScore;
        CurrentReport.passed = CurrentSession.overallStatus == TrainingOverallStatus.Passed;
        CurrentReport.overallSummary = CurrentSession.summary;

        CurrentStepResult = null;
        RefreshReportSnapshotInternal();
    }

    public void CapturePoseSample(string stepId, TrackedToolType toolType, Transform toolTransform, Transform referenceFrame = null)
    {
        EnsureRecorderReadyInternal();
        if (toolTransform == null) return;

        StepEvaluationResult step = LocateTrackedStepInternal(stepId);
        if (step == null) return;

        float interval = config != null && config.samplingRateHz > 0f ? 1f / config.samplingRateHz : 0f;
        string sampleKey = step.stepId + ":" + toolType;
        float now = Time.unscaledTime;

        if (_lastPoseCaptureTimes.TryGetValue(sampleKey, out float previousTime) && interval > 0f && now - previousTime < interval)
        {
            return;
        }

        _lastPoseCaptureTimes[sampleKey] = now;
        step.poseSamples.Add(BuildPoseSampleInternal(step.stepId, toolType, toolTransform, referenceFrame));

        if (toolType != TrackedToolType.Unknown && !CurrentSession.trackedTools.Contains(toolType))
        {
            CurrentSession.trackedTools.Add(toolType);
        }
    }

    private void EnsureRecorderReadyInternal()
    {
        if (CurrentSession == null || CurrentReport == null) BeginSession("Unnamed Scenario");
    }

    private StepEvaluationResult LocateTrackedStepInternal(string stepId)
    {
        if (CurrentStepResult != null && (string.IsNullOrEmpty(stepId) || CurrentStepResult.stepId == stepId)) return CurrentStepResult;
        int index = FindTrackedStepIndexInternal(stepId);
        return index >= 0 ? _capturedSteps[index] : null;
    }

    private int FindTrackedStepIndexInternal(string stepId)
    {
        for (int i = 0; i < _capturedSteps.Count; i++)
        {
            if (_capturedSteps[i] != null && _capturedSteps[i].stepId == stepId) return i;
        }

        return -1;
    }

    private void CopyIncomingStepPayloadInternal(StepEvaluationResult source, StepEvaluationResult target)
    {
        target.stepName = source.stepName;
        target.stepIndex = source.stepIndex;
        target.status = source.status;
        target.startTimeSeconds = source.startTimeSeconds > 0f ? source.startTimeSeconds : target.startTimeSeconds;
        target.endTimeSeconds = source.endTimeSeconds > 0f ? source.endTimeSeconds : target.endTimeSeconds;
        target.durationSeconds = source.durationSeconds > 0f ? source.durationSeconds : target.durationSeconds;
        target.completionRatio = source.completionRatio;
        target.rawScore = source.rawScore;
        target.normalizedScore = source.normalizedScore;
        target.summary = source.summary;
        target.feedback = source.feedback;
        target.recommendation = source.recommendation;
        target.completed = source.completed;
        target.isRequired = source.isRequired;
        target.strengths = source.strengths ?? new List<string>();
        target.issues = source.issues ?? new List<string>();
        target.suggestedActions = source.suggestedActions ?? new List<string>();
        target.observedMetrics = source.observedMetrics ?? new List<ObservedMetricRecord>();
        target.normalizedMetrics = source.normalizedMetrics ?? new List<NormalizedMetricRecord>();
        target.thresholdResults = source.thresholdResults ?? new List<ThresholdResult>();
        target.relatedEvents = source.relatedEvents ?? target.relatedEvents ?? new List<TrainingEventRecord>();
        target.poseSamples = source.poseSamples ?? target.poseSamples ?? new List<PoseSample>();
    }

    private float ComputeElapsedSessionSecondsInternal()
    {
        return Mathf.Max(0f, Time.unscaledTime - _sessionStartClock);
    }

    private float ComputeAverageStepScoreInternal()
    {
        if (_capturedSteps.Count == 0) return 0f;

        float total = 0f;
        for (int i = 0; i < _capturedSteps.Count; i++)
        {
            StepEvaluationResult step = _capturedSteps[i];
            total += step != null && step.normalizedScore > 0f ? step.normalizedScore : step != null ? step.rawScore : 0f;
        }

        return total / _capturedSteps.Count;
    }

    private TrainingOverallStatus ResolveOverallStatusInternal()
    {
        for (int i = 0; i < _capturedSteps.Count; i++)
        {
            if (_capturedSteps[i] != null && _capturedSteps[i].status == TrainingOverallStatus.Failed) return TrainingOverallStatus.Failed;
        }

        return CurrentSession.overallScore >= CurrentSession.passingScore ? TrainingOverallStatus.Passed : TrainingOverallStatus.Incomplete;
    }

    private string ComposeSessionSummaryInternal()
    {
        int completedCount = 0;
        for (int i = 0; i < _capturedSteps.Count; i++)
        {
            if (_capturedSteps[i] != null && _capturedSteps[i].completed) completedCount++;
        }

        return string.Format("Completed {0}/{1} steps in {2:F1}s.", completedCount, _capturedSteps.Count, CurrentSession.durationSeconds);
    }

    private PoseSample BuildPoseSampleInternal(string stepId, TrackedToolType toolType, Transform toolTransform, Transform referenceFrame)
    {
        PoseSample sample = new PoseSample
        {
            sampleId = Guid.NewGuid().ToString("N"),
            stepId = stepId,
            toolType = toolType,
            toolInstanceId = toolTransform.name,
            timestampSeconds = ComputeElapsedSessionSecondsInternal(),
            position = toolTransform.position,
            rotation = toolTransform.rotation
        };

        if (referenceFrame != null)
        {
            sample.referenceFrame = referenceFrame.name;
            sample.localPosition = referenceFrame.InverseTransformPoint(toolTransform.position);
            sample.localRotation = Quaternion.Inverse(referenceFrame.rotation) * toolTransform.rotation;
        }
        else
        {
            sample.localPosition = toolTransform.localPosition;
            sample.localRotation = toolTransform.localRotation;
        }

        return sample;
    }

    private void AppendTimelineEventInternal(string stepId, TrainingEventType eventType, string title, string message, bool isError)
    {
        TrainingEventRecord record = new TrainingEventRecord
        {
            eventId = Guid.NewGuid().ToString("N"),
            sessionId = CurrentSession != null ? CurrentSession.sessionId : string.Empty,
            stepId = stepId,
            eventType = eventType,
            toolType = TrackedToolType.Unknown,
            title = title,
            message = message,
            severity = isError ? "error" : "info",
            timestampSeconds = ComputeElapsedSessionSecondsInternal(),
            worldPosition = Vector3.zero,
            worldRotation = Quaternion.identity,
            isError = isError
        };

        _timelineEvents.Add(record);
        StepEvaluationResult step = LocateTrackedStepInternal(stepId);
        if (step != null) step.relatedEvents.Add(record);
    }

    private void RefreshReportSnapshotInternal()
    {
        if (CurrentReport == null || CurrentSession == null) return;

        CurrentReport.session = CurrentSession;
        CurrentReport.reportGeneratedAt = DateTime.UtcNow.ToString("o");
        CurrentSession.stepResults.Clear();
        CurrentReport.stepResults.Clear();
        CurrentReport.sessionEvents.Clear();

        for (int i = 0; i < _capturedSteps.Count; i++)
        {
            CurrentSession.stepResults.Add(_capturedSteps[i]);
            CurrentReport.stepResults.Add(_capturedSteps[i]);
        }

        if (CurrentStepResult != null && FindTrackedStepIndexInternal(CurrentStepResult.stepId) < 0)
        {
            CurrentSession.stepResults.Add(CurrentStepResult);
            CurrentReport.stepResults.Add(CurrentStepResult);
        }

        for (int i = 0; i < _timelineEvents.Count; i++) CurrentReport.sessionEvents.Add(_timelineEvents[i]);
    }
}
