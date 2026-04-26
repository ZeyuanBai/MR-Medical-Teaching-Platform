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
    public float passScore = 0.8f;

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
            passScore = 0.8f,
            step1 = Step1Rubric.CreateDefault(),
            step2 = Step23Rubric.CreateStep2Default(),
            step3 = Step23Rubric.CreateStep3Default(),
            step4 = Step4Rubric.CreateDefault()
        };
    }
}

[Serializable]
public class Step1Rubric
{
    public AllowedAxes allowedAxes = AllowedAxes.Vertical;

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
            allowedAxes = AllowedAxes.Vertical,
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
            allowedAxes = AllowedAxes.Vertical,
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
            allowedAxes = AllowedAxes.Horizontal,
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
