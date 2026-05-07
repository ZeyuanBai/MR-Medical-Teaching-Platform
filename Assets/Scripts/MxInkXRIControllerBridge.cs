using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers - 1)]
public class MxInkXRIControllerBridge : MonoBehaviour
{
    [Flags]
    private enum StylusInputSource
    {
        None = 0,
        Tip = 1 << 0,
        Front = 1 << 1,
        Middle = 1 << 2,
        Back = 1 << 3,
        BackDoubleTap = 1 << 4
    }

    [Header("References")]
    [SerializeField] private StylusHandler _stylusRuntime;
    [SerializeField] private ActionBasedController _targetController;

    [Header("Pose")]
    [SerializeField] private bool _driveControllerPose;
    [SerializeField] private bool _poseIsTrackingSpace = true;
    [SerializeField] private bool _applyPoseToTransformImmediately;

    [Header("Input Mapping")]
    [SerializeField] private StylusInputSource _selectSources = StylusInputSource.Front;
    [SerializeField] private StylusInputSource _activateSources = StylusInputSource.None;
    [SerializeField] private StylusInputSource _uiPressSources = StylusInputSource.Back;
    [SerializeField, Range(0f, 1f)] private float _tipPressThreshold = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _middlePressThreshold = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool _logStateChanges;
    [SerializeField] private bool _logInputStateChanges;

    private bool _hasOriginalInputSettings;
    private bool _originalEnableInputActions;
    private bool _originalEnableInputTracking;
    private Vector3 _originalLocalPosition;
    private Quaternion _originalLocalRotation;
    private bool _wasDriving;
    private bool _lastLoggedDriving;
    private string _lastInputLog;

    private void Reset()
    {
        _targetController = GetComponent<ActionBasedController>();
    }

    private void Awake()
    {
        if (_targetController == null)
        {
            _targetController = GetComponent<ActionBasedController>();
        }

    }

    private void OnDisable()
    {
        RestoreControllerInputs();
    }

    private void Update()
    {
        if (_targetController == null || _stylusRuntime == null)
        {
            RestoreControllerInputs();
            return;
        }

        StylusInputs stylus = _stylusRuntime.CurrentState;
        bool shouldDrive = stylus.isActive && _stylusRuntime.CanDraw();
        if (!shouldDrive)
        {
            ClearDrivenStateIfNeeded();
            RestoreControllerInputs();
            LogDrivingState(false);
            return;
        }

        CacheControllerInputs();
        _targetController.enableInputActions = false;
        if (_driveControllerPose)
        {
            _targetController.enableInputTracking = false;
        }

        XRControllerState controllerState = _targetController.currentControllerState;
        if (controllerState == null)
        {
            controllerState = new XRControllerState();
            _targetController.currentControllerState = controllerState;
        }

        if (_driveControllerPose)
        {
            ApplyPose(controllerState, stylus.inkingPose);
        }

        ApplyInput(controllerState, stylus);
        if (_applyPoseToTransformImmediately && _driveControllerPose)
        {
            ApplyPoseToTransform(controllerState);
        }

        LogInputState(stylus, controllerState);

        _wasDriving = true;
        LogDrivingState(true);
    }

    private void ApplyPose(XRControllerState controllerState, Pose stylusPose)
    {
        controllerState.isTracked = true;
        controllerState.inputTrackingState = InputTrackingState.Position | InputTrackingState.Rotation;

        if (_poseIsTrackingSpace)
        {
            controllerState.position = stylusPose.position;
            controllerState.rotation = stylusPose.rotation;
            return;
        }

        Transform parent = _targetController.transform.parent;
        if (parent == null)
        {
            controllerState.position = stylusPose.position;
            controllerState.rotation = stylusPose.rotation;
            return;
        }

        controllerState.position = parent.InverseTransformPoint(stylusPose.position);
        controllerState.rotation = Quaternion.Inverse(parent.rotation) * stylusPose.rotation;
    }

    private void ApplyInput(XRControllerState controllerState, StylusInputs stylus)
    {
        float selectValue = ResolveValue(_selectSources, stylus);
        bool selectActive = ResolveActive(_selectSources, stylus, selectValue);
        controllerState.selectInteractionState.SetFrameState(selectActive, selectValue);

        float activateValue = ResolveValue(_activateSources, stylus);
        bool activateActive = ResolveActive(_activateSources, stylus, activateValue);
        controllerState.activateInteractionState.SetFrameState(activateActive, activateValue);

        float uiPressValue = ResolveValue(_uiPressSources, stylus);
        bool uiPressActive = ResolveActive(_uiPressSources, stylus, uiPressValue);
        controllerState.uiPressInteractionState.SetFrameState(uiPressActive, uiPressValue);
        controllerState.uiScrollValue = Vector2.zero;
    }

    private float ResolveValue(StylusInputSource sources, StylusInputs stylus)
    {
        float value = 0f;

        if ((sources & StylusInputSource.Tip) != 0)
        {
            value = Mathf.Max(value, Mathf.Clamp01(stylus.tip_value));
        }

        if ((sources & StylusInputSource.Middle) != 0)
        {
            value = Mathf.Max(value, Mathf.Clamp01(stylus.cluster_middle_value));
        }

        if ((sources & StylusInputSource.Front) != 0 && stylus.cluster_front_value)
        {
            value = 1f;
        }

        if ((sources & StylusInputSource.Back) != 0 && stylus.cluster_back_value)
        {
            value = 1f;
        }

        if ((sources & StylusInputSource.BackDoubleTap) != 0 && stylus.cluster_back_double_tap_value)
        {
            value = 1f;
        }

        return value;
    }

    private bool ResolveActive(StylusInputSource sources, StylusInputs stylus, float value)
    {
        if ((sources & StylusInputSource.Tip) != 0 && stylus.tip_value >= _tipPressThreshold)
        {
            return true;
        }

        if ((sources & StylusInputSource.Middle) != 0 && stylus.cluster_middle_value >= _middlePressThreshold)
        {
            return true;
        }

        return ((sources & StylusInputSource.Front) != 0 && stylus.cluster_front_value) ||
               ((sources & StylusInputSource.Back) != 0 && stylus.cluster_back_value) ||
               ((sources & StylusInputSource.BackDoubleTap) != 0 && stylus.cluster_back_double_tap_value) ||
               value >= 1f;
    }

    private void CacheControllerInputs()
    {
        if (_hasOriginalInputSettings || _targetController == null)
        {
            return;
        }

        _originalEnableInputActions = _targetController.enableInputActions;
        _originalEnableInputTracking = _targetController.enableInputTracking;
        _originalLocalPosition = _targetController.transform.localPosition;
        _originalLocalRotation = _targetController.transform.localRotation;
        _hasOriginalInputSettings = true;
    }

    private void RestoreControllerInputs()
    {
        if (!_hasOriginalInputSettings || _targetController == null)
        {
            return;
        }

        _targetController.enableInputActions = _originalEnableInputActions;
        _targetController.enableInputTracking = _originalEnableInputTracking;
        if (_driveControllerPose)
        {
            _targetController.transform.localPosition = _originalLocalPosition;
            _targetController.transform.localRotation = _originalLocalRotation;
        }

        _hasOriginalInputSettings = false;
        _wasDriving = false;
    }

    private void ClearDrivenStateIfNeeded()
    {
        if (!_wasDriving || _targetController == null)
        {
            return;
        }

        XRControllerState controllerState = _targetController.currentControllerState;
        if (controllerState == null)
        {
            return;
        }

        controllerState.selectInteractionState.SetFrameState(false, 0f);
        controllerState.activateInteractionState.SetFrameState(false, 0f);
        controllerState.uiPressInteractionState.SetFrameState(false, 0f);
    }

    private void ApplyPoseToTransform(XRControllerState controllerState)
    {
        Transform targetTransform = _targetController.transform;
        if ((controllerState.inputTrackingState & InputTrackingState.Position) != 0)
        {
            targetTransform.localPosition = controllerState.position;
        }

        if ((controllerState.inputTrackingState & InputTrackingState.Rotation) != 0)
        {
            targetTransform.localRotation = controllerState.rotation;
        }
    }

    private void LogDrivingState(bool driving)
    {
        if (!_logStateChanges || driving == _lastLoggedDriving)
        {
            return;
        }

        _lastLoggedDriving = driving;
        Debug.Log(driving ? "MX Ink XRI bridge: driving controller state." : "MX Ink XRI bridge: restored controller input.");
    }

    private void LogInputState(StylusInputs stylus, XRControllerState controllerState)
    {
        if (!_logInputStateChanges)
        {
            return;
        }

        string inputLog = $"{name}: tip={stylus.tip_value:0.00}, front={stylus.cluster_front_value}, middle={stylus.cluster_middle_value:0.00}, back={stylus.cluster_back_value}, " +
                          $"select={controllerState.selectInteractionState.active}/{controllerState.selectInteractionState.value:0.00}, " +
                          $"activate={controllerState.activateInteractionState.active}/{controllerState.activateInteractionState.value:0.00}, " +
                          $"ui={controllerState.uiPressInteractionState.active}/{controllerState.uiPressInteractionState.value:0.00}";
        if (inputLog == _lastInputLog)
        {
            return;
        }

        _lastInputLog = inputLog;
        Debug.Log($"MX Ink XRI bridge input: {inputLog}");
    }
}
