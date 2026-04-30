using System;
using UnityEngine;

[Serializable]
public class RubricConfig
{
    [Header("Versions")]
    public string algorithmVersion = "1.0.0";
    public string rubricVersion = "1.0.0";
    public string scenarioVersion = "1.0.0";

    [Header("Sampling")]
    public float samplingRateHz = 30f;
    public float minSamplingRateHz = 20f;
    public float maxSamplingRateHz = 50f;
    public float debounceDurationSeconds = 0.2f;
    public int stableWindowFrames = 6;
    public float passScore = 0.65f;
    public float stepCompletionWeight = 0.5f;
    public float stepDetailWeight = 0.5f;

    [Header("Position Penalty Curves")]
    public string positionPenaltyCurveVersion = "0.2-review";
    public PositionPenaltyCurveConfig step1PositionCurve = PositionPenaltyCurveConfig.CreateStep1Default();
    public PositionPenaltyCurveConfig step2PositionCurve = PositionPenaltyCurveConfig.CreateStep2Default();
    public AirwayPositionPenaltyConfig step3PositionCurve = AirwayPositionPenaltyConfig.CreateDefault();
    public PositionPenaltyCurveConfig step4PositionCurve = PositionPenaltyCurveConfig.CreateStep4Default();

    [Header("Step Rubrics")]
    public Step1Rubric step1 = new Step1Rubric();
    public Step23Rubric step2 = new Step23Rubric();
    public Step23Rubric step3 = new Step23Rubric();
    public Step4Rubric step4 = new Step4Rubric();

    public static RubricConfig CreateDefault()
    {
        return new RubricConfig
        {
            algorithmVersion = "1.0.0",
            rubricVersion = "1.0.0",
            scenarioVersion = "default-tracheotomy-v1",
            samplingRateHz = 30f,
            minSamplingRateHz = 20f,
            maxSamplingRateHz = 50f,
            debounceDurationSeconds = 0.2f,
            stableWindowFrames = 6,
            passScore = 0.65f,
            stepCompletionWeight = 0.5f,
            stepDetailWeight = 0.5f,
            positionPenaltyCurveVersion = "0.2-review",
            step1PositionCurve = PositionPenaltyCurveConfig.CreateStep1Default(),
            step2PositionCurve = PositionPenaltyCurveConfig.CreateStep2Default(),
            step3PositionCurve = AirwayPositionPenaltyConfig.CreateDefault(),
            step4PositionCurve = PositionPenaltyCurveConfig.CreateStep4Default(),
            step1 = Step1Rubric.CreateDefault(),
            step2 = Step23Rubric.CreateStep2Default(),
            step3 = Step23Rubric.CreateStep3Default(),
            step4 = Step4Rubric.CreateDefault()
        };
    }

    public void EnsureStepDefaults()
    {
        if (passScore <= 0f || Mathf.Approximately(passScore, 0.8f)) passScore = 0.65f;
        if (stepCompletionWeight <= 0f && stepDetailWeight <= 0f)
        {
            stepCompletionWeight = 0.5f;
            stepDetailWeight = 0.5f;
        }

        if (string.IsNullOrEmpty(positionPenaltyCurveVersion)) positionPenaltyCurveVersion = "0.2-review";
        if (step1PositionCurve == null) step1PositionCurve = PositionPenaltyCurveConfig.CreateStep1Default();
        if (step2PositionCurve == null) step2PositionCurve = PositionPenaltyCurveConfig.CreateStep2Default();
        if (step3PositionCurve == null) step3PositionCurve = AirwayPositionPenaltyConfig.CreateDefault();
        else step3PositionCurve.EnsureDefaults();
        if (step4PositionCurve == null) step4PositionCurve = PositionPenaltyCurveConfig.CreateStep4Default();

        if (step1 == null) step1 = Step1Rubric.CreateDefault();
        else if (IsLegacyStep1Default(step1)) step1 = Step1Rubric.CreateDefault();
        if (step2 == null) step2 = Step23Rubric.CreateStep2Default();
        else if (IsLegacyStep2Default(step2)) step2 = Step23Rubric.CreateStep2Default();
        if (step3 == null || IsGenericStep23Default(step3)) step3 = Step23Rubric.CreateStep3Default();
        if (step4 == null) step4 = Step4Rubric.CreateDefault();
    }

    private static bool IsLegacyStep1Default(Step1Rubric rubric)
    {
        return rubric.allowedAxes == AllowedAxes.Vertical
            && Mathf.Approximately(rubric.angleErrorThresholdDegrees, 10f)
            && Mathf.Approximately(rubric.midpointOffsetThresholdMeters, 0.01f)
            && Mathf.Approximately(rubric.markerLengthMinMeters, 0.015f)
            && Mathf.Approximately(rubric.markerLengthMaxMeters, 0.03f)
            && Mathf.Approximately(rubric.angleErrorWeight, 0.4f)
            && Mathf.Approximately(rubric.midpointOffsetWeight, 0.35f)
            && Mathf.Approximately(rubric.markerLengthWeight, 0.25f);
    }

    private static bool IsLegacyStep2Default(Step23Rubric rubric)
    {
        return rubric.allowedAxes == AllowedAxes.Vertical
            && Mathf.Approximately(rubric.angleErrorThresholdDegrees, 12f)
            && Mathf.Approximately(rubric.lengthMinMeters, 0.018f)
            && Mathf.Approximately(rubric.lengthMaxMeters, 0.04f)
            && Mathf.Approximately(rubric.centerOffsetThresholdMeters, 0.008f)
            && Mathf.Approximately(rubric.pathEfficiencyThreshold, 0.75f)
            && Mathf.Approximately(rubric.angleErrorWeight, 0.3f)
            && Mathf.Approximately(rubric.lengthWeight, 0.25f)
            && Mathf.Approximately(rubric.centerOffsetWeight, 0.25f)
            && Mathf.Approximately(rubric.pathEfficiencyWeight, 0.2f);
    }

    private static bool IsGenericStep23Default(Step23Rubric rubric)
    {
        bool genericStep23 = rubric.allowedAxes == AllowedAxes.Vertical
            && Mathf.Approximately(rubric.angleErrorThresholdDegrees, 12f)
            && Mathf.Approximately(rubric.lengthMinMeters, 0.015f)
            && Mathf.Approximately(rubric.lengthMaxMeters, 0.04f)
            && Mathf.Approximately(rubric.centerOffsetThresholdMeters, 0.008f)
            && Mathf.Approximately(rubric.pathEfficiencyThreshold, 0.75f)
            && Mathf.Approximately(rubric.angleErrorWeight, 0.3f)
            && Mathf.Approximately(rubric.lengthWeight, 0.25f)
            && Mathf.Approximately(rubric.centerOffsetWeight, 0.25f)
            && Mathf.Approximately(rubric.pathEfficiencyWeight, 0.2f);

        bool legacyHorizontalAirway = rubric.allowedAxes == AllowedAxes.Horizontal
            && Mathf.Approximately(rubric.angleErrorThresholdDegrees, 8f)
            && Mathf.Approximately(rubric.lengthMinMeters, 0.01f)
            && Mathf.Approximately(rubric.lengthMaxMeters, 0.025f)
            && Mathf.Approximately(rubric.centerOffsetThresholdMeters, 0.006f)
            && Mathf.Approximately(rubric.pathEfficiencyThreshold, 0.8f)
            && Mathf.Approximately(rubric.angleErrorWeight, 0.35f)
            && Mathf.Approximately(rubric.lengthWeight, 0.2f)
            && Mathf.Approximately(rubric.centerOffsetWeight, 0.25f)
            && Mathf.Approximately(rubric.pathEfficiencyWeight, 0.2f);

        return genericStep23 || legacyHorizontalAirway;
    }
}

[Serializable]
public class PositionPenaltyCurveConfig
{
    public float freeCm;
    public float softCm;
    public float hardCm;
    public float failCm;
    public float softPenalty;
    public float hardScore;

    public static PositionPenaltyCurveConfig CreateStep1Default()
    {
        return new PositionPenaltyCurveConfig
        {
            freeCm = 0.7f,
            softCm = 1.4f,
            hardCm = 2.4f,
            failCm = 3.2f,
            softPenalty = 0.12f,
            hardScore = 0.35f
        };
    }

    public static PositionPenaltyCurveConfig CreateStep2Default()
    {
        return new PositionPenaltyCurveConfig
        {
            freeCm = 0.6f,
            softCm = 1.2f,
            hardCm = 2.0f,
            failCm = 2.8f,
            softPenalty = 0.15f,
            hardScore = 0.32f
        };
    }

    public static PositionPenaltyCurveConfig CreateStep3LateralDefault()
    {
        return new PositionPenaltyCurveConfig
        {
            freeCm = 0.25f,
            softCm = 0.5f,
            hardCm = 0.9f,
            failCm = 1.2f,
            softPenalty = 0.20f,
            hardScore = 0.25f
        };
    }

    public static PositionPenaltyCurveConfig CreateStep3CranialDefault()
    {
        return new PositionPenaltyCurveConfig
        {
            freeCm = 0.25f,
            softCm = 0.45f,
            hardCm = 0.75f,
            failCm = 1.0f,
            softPenalty = 0.25f,
            hardScore = 0.20f
        };
    }

    public static PositionPenaltyCurveConfig CreateStep3CaudalDefault()
    {
        return new PositionPenaltyCurveConfig
        {
            freeCm = 0.35f,
            softCm = 0.8f,
            hardCm = 1.2f,
            failCm = 1.8f,
            softPenalty = 0.18f,
            hardScore = 0.30f
        };
    }

    public static PositionPenaltyCurveConfig CreateStep4Default()
    {
        return new PositionPenaltyCurveConfig
        {
            freeCm = 0.5f,
            softCm = 1.0f,
            hardCm = 1.8f,
            failCm = 2.5f,
            softPenalty = 0.12f,
            hardScore = 0.45f
        };
    }
}

[Serializable]
public class AirwayPositionPenaltyConfig
{
    public PositionPenaltyCurveConfig lateral = PositionPenaltyCurveConfig.CreateStep3LateralDefault();
    public PositionPenaltyCurveConfig cranial = PositionPenaltyCurveConfig.CreateStep3CranialDefault();
    public PositionPenaltyCurveConfig caudal = PositionPenaltyCurveConfig.CreateStep3CaudalDefault();

    public static AirwayPositionPenaltyConfig CreateDefault()
    {
        return new AirwayPositionPenaltyConfig
        {
            lateral = PositionPenaltyCurveConfig.CreateStep3LateralDefault(),
            cranial = PositionPenaltyCurveConfig.CreateStep3CranialDefault(),
            caudal = PositionPenaltyCurveConfig.CreateStep3CaudalDefault()
        };
    }

    public void EnsureDefaults()
    {
        if (lateral == null) lateral = PositionPenaltyCurveConfig.CreateStep3LateralDefault();
        if (cranial == null) cranial = PositionPenaltyCurveConfig.CreateStep3CranialDefault();
        if (caudal == null) caudal = PositionPenaltyCurveConfig.CreateStep3CaudalDefault();
    }
}

[Serializable]
public class Step1Rubric
{
    public AllowedAxes allowedAxes = AllowedAxes.Both;

    [Header("Thresholds")]
    public float angleErrorThresholdDegrees = 10f;
    public float midpointOffsetThresholdMeters = 0.01f;
    public float markerLengthMinMeters = 0.015f;
    public float markerLengthMaxMeters = 0.03f;

    [Header("Weights")]
    public float angleErrorWeight = 0.4f;
    public float midpointOffsetWeight = 0.35f;
    public float markerLengthWeight = 0.25f;

    public static Step1Rubric CreateDefault()
    {
        return new Step1Rubric
        {
            allowedAxes = AllowedAxes.Both,
            angleErrorThresholdDegrees = 10f,
            midpointOffsetThresholdMeters = 0.01f,
            markerLengthMinMeters = 0.015f,
            markerLengthMaxMeters = 0.03f,
            angleErrorWeight = 0.4f,
            midpointOffsetWeight = 0.35f,
            markerLengthWeight = 0.25f
        };
    }
}

[Serializable]
public class Step23Rubric
{
    public AllowedAxes allowedAxes = AllowedAxes.Vertical;

    [Header("Thresholds")]
    public float angleErrorThresholdDegrees = 12f;
    public float lengthMinMeters = 0.015f;
    public float lengthMaxMeters = 0.04f;
    public float centerOffsetThresholdMeters = 0.008f;
    public float pathEfficiencyThreshold = 0.75f;

    [Header("Weights")]
    public float angleErrorWeight = 0.3f;
    public float lengthWeight = 0.25f;
    public float centerOffsetWeight = 0.25f;
    public float pathEfficiencyWeight = 0.2f;

    public static Step23Rubric CreateStep2Default()
    {
        return new Step23Rubric
        {
            allowedAxes = AllowedAxes.Both,
            angleErrorThresholdDegrees = 12f,
            lengthMinMeters = 0.018f,
            lengthMaxMeters = 0.04f,
            centerOffsetThresholdMeters = 0.008f,
            pathEfficiencyThreshold = 0.75f,
            angleErrorWeight = 0.3f,
            lengthWeight = 0.25f,
            centerOffsetWeight = 0.25f,
            pathEfficiencyWeight = 0.2f
        };
    }

    public static Step23Rubric CreateStep3Default()
    {
        return new Step23Rubric
        {
            allowedAxes = AllowedAxes.Vertical,
            angleErrorThresholdDegrees = 8f,
            lengthMinMeters = 0.01f,
            lengthMaxMeters = 0.025f,
            centerOffsetThresholdMeters = 0.006f,
            pathEfficiencyThreshold = 0.8f,
            angleErrorWeight = 0.35f,
            lengthWeight = 0.2f,
            centerOffsetWeight = 0.25f,
            pathEfficiencyWeight = 0.2f
        };
    }
}

[Serializable]
public class Step4Rubric
{
    public AllowedAxes allowedAxes = AllowedAxes.Both;

    [Header("Thresholds")]
    public bool requireSuccessState = true;
    public float stableHoldDurationSeconds = 1.5f;
    public float angleErrorThresholdDegrees = 15f;

    [Header("Weights")]
    public float successStateWeight = 0.5f;
    public float stableHoldWeight = 0.3f;
    public float angleErrorWeight = 0.2f;

    public static Step4Rubric CreateDefault()
    {
        return new Step4Rubric
        {
            allowedAxes = AllowedAxes.Both,
            requireSuccessState = true,
            stableHoldDurationSeconds = 1.5f,
            angleErrorThresholdDegrees = 15f,
            successStateWeight = 0.5f,
            stableHoldWeight = 0.3f,
            angleErrorWeight = 0.2f
        };
    }
}
