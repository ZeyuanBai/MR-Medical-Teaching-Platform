using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class MetaMxInkRuntime : StylusHandler
{
    [Header("Scene References")]
    [SerializeField] private GameObject _mxInkRoot;
    [SerializeField] private ActionBasedController _rightController;

    [Header("MX Ink Input Actions")]
    [SerializeField] private InputActionReference _tipActionRef;
    [SerializeField] private InputActionReference _grabActionRef;
    [SerializeField] private InputActionReference _middleActionRef;
    [SerializeField] private InputActionReference _optionActionRef;

    [Header("Meta Core SDK OpenXR Actions")]
    [SerializeField] private bool _preferMetaOpenXRActions = true;
    [SerializeField] private string _poseActionName = "aim_right";
    [SerializeField] private string _tipActionName = "tip";
    [SerializeField] private string _frontActionName = "front";
    [SerializeField] private string _middleActionName = "middle";
    [SerializeField] private string _backActionName = "back";
    [SerializeField] private string _dockActionName = "dock";
    [SerializeField] private string _hapticActionName = "haptic_pulse";

    [Header("Button Visuals")]
    [SerializeField] private Renderer _tipRenderer;
    [SerializeField] private Renderer _clusterFrontRenderer;
    [SerializeField] private Renderer _clusterMiddleRenderer;
    [SerializeField] private Renderer _clusterBackRenderer;
    [SerializeField] private Color _activeColor = Color.gray;
    [SerializeField] private Color _doubleTapActiveColor = Color.cyan;
    [SerializeField] private Color _defaultColor = Color.black;

    [Header("Profile Mapping")]
    [SerializeField] private string _stylusProfile = "/interaction_profiles/oculus/touch_controller";
    [SerializeField] private string _officialStylusProfile = "/interaction_profiles/logitech/mx_ink_stylus_logitech";
    [SerializeField] private string _controllerProfile = "/interaction_profiles/meta/touch_controller_plus";

    [Header("Debug")]
    [SerializeField] private bool _logProfileChanges = true;
    [SerializeField] private bool _logInputDiagnostics;
    [SerializeField] private bool _useLegacyTouchFallback;

    public UnityEvent<bool> StylusActiveChanged;

    private bool _wasActive;
    private string _lastRightProfile = string.Empty;
    private string _lastInputDiagnostic = string.Empty;

    private enum InputReadSource
    {
        None,
        MetaAction,
        InputAction,
        LegacyFallback
    }

    public bool IsStylusActive => _stylus.isActive;

    private void OnEnable()
    {
        SetMxInkActionsEnabled(true);
        RefreshProfileAndModels(true);
    }

    private void OnDisable()
    {
        SetMxInkActionsEnabled(false);
    }

    private void Update()
    {
        OVRInput.Update();
        RefreshProfileAndModels(false);

        if (!_stylus.isActive)
        {
            ClearInputs();
            UpdateButtonVisuals();
            return;
        }

        RefreshStylusInputs();
        UpdateButtonVisuals();
    }

    private void RefreshProfileAndModels(bool forceEvent)
    {
        string rightProfile = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandRight) ?? string.Empty;
        bool nextActive = IsProfile(rightProfile, _stylusProfile) || IsProfile(rightProfile, _officialStylusProfile);
        bool profileKnownAsController = IsProfile(rightProfile, _controllerProfile);
        bool activeChanged = nextActive != _wasActive;
        bool profileChanged = rightProfile != _lastRightProfile;

        _wasActive = nextActive;
        _lastRightProfile = rightProfile;
        _stylus.isActive = nextActive;
        _stylus.isOnRightHand = true;

        SetActiveIfAssigned(_mxInkRoot, nextActive);
        SetRightControllerModelVisible(!nextActive);

        if (_logProfileChanges && (forceEvent || activeChanged || profileChanged))
        {
            Debug.LogError($"MX Ink visual switch: stylusActive={nextActive}, controllerProfile={profileKnownAsController}, rightProfile='{rightProfile}'");
        }

        if (forceEvent || activeChanged)
        {
            StylusActiveChanged?.Invoke(nextActive);
        }
    }

    private void ClearInputs()
    {
        _stylus.tip_value = 0f;
        _stylus.cluster_middle_value = 0f;
        _stylus.cluster_front_value = false;
        _stylus.cluster_back_value = false;
        _stylus.cluster_back_double_tap_value = false;
        _stylus.docked = false;
        _stylus.any = false;
    }

    private void RefreshStylusInputs()
    {
        bool poseFromMeta = TryReadMetaPose(_poseActionName, out Pose inkingPose);
        _stylus.inkingPose = poseFromMeta ? inkingPose : ResolveInkingPose();

        InputReadSource tipSource = TryReadStylusFloat(_tipActionName, _tipActionRef, out float tipValue);
        if (tipSource == InputReadSource.None && _useLegacyTouchFallback)
        {
            tipValue = Mathf.Max(
                OVRInput.Get(OVRInput.Axis1D.SecondaryStylusForce, OVRInput.Controller.RTouch),
                OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.RTouch));
            tipSource = InputReadSource.LegacyFallback;
        }

        InputReadSource middleSource = TryReadStylusFloat(_middleActionName, _middleActionRef, out float middleValue);
        if (middleSource == InputReadSource.None && _useLegacyTouchFallback)
        {
            middleValue = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.RTouch);
            middleSource = InputReadSource.LegacyFallback;
        }

        InputReadSource frontSource = TryReadStylusButton(_frontActionName, _grabActionRef, out bool frontPressed);
        if (frontSource == InputReadSource.None && _useLegacyTouchFallback)
        {
            frontPressed = TryReadControllerPressed(_rightController != null ? _rightController.selectAction.action : null, out bool controllerSelect) && controllerSelect;
            frontPressed |= OVRInput.Get(OVRInput.Button.SecondaryHandTrigger, OVRInput.Controller.RTouch);
            frontSource = InputReadSource.LegacyFallback;
        }

        InputReadSource backSource = TryReadStylusButton(_backActionName, _optionActionRef, out bool backPressed);
        if (backSource == InputReadSource.None && _useLegacyTouchFallback)
        {
            bool controllerBack = TryReadControllerPressed(_rightController != null ? _rightController.uiPressAction.action : null, out bool uiPress) && uiPress;
            backPressed = controllerBack ||
                          OVRInput.Get(OVRInput.RawButton.A, OVRInput.Controller.RTouch) ||
                          OVRInput.Get(OVRInput.RawButton.B, OVRInput.Controller.RTouch);
            backSource = InputReadSource.LegacyFallback;
        }

        _stylus.tip_value = Mathf.Clamp01(tipValue);
        _stylus.cluster_middle_value = Mathf.Clamp01(middleValue);
        _stylus.cluster_front_value = frontPressed;
        _stylus.cluster_back_value = backPressed;
        _stylus.cluster_back_double_tap_value = false;
        _stylus.docked = TryReadMetaBool(_dockActionName, out bool docked) && docked;
        _stylus.any = _stylus.tip_value > 0f ||
                      _stylus.cluster_middle_value > 0f ||
                      _stylus.cluster_front_value ||
                      _stylus.cluster_back_value;

        LogInputDiagnostics(poseFromMeta, tipSource, frontSource, middleSource, backSource);
    }

    private Pose ResolveInkingPose()
    {
        Transform source = _mxInkRoot != null ? _mxInkRoot.transform : transform;
        return new Pose(source.position, source.rotation);
    }

    private void SetMxInkActionsEnabled(bool enabled)
    {
        SetActionEnabled(_tipActionRef, enabled);
        SetActionEnabled(_grabActionRef, enabled);
        SetActionEnabled(_middleActionRef, enabled);
        SetActionEnabled(_optionActionRef, enabled);
    }

    private static void SetActionEnabled(InputActionReference actionRef, bool enabled)
    {
        InputAction action = actionRef != null ? actionRef.action : null;
        if (action == null)
        {
            return;
        }

        if (enabled && !action.enabled)
        {
            action.Enable();
        }
        else if (!enabled && action.enabled)
        {
            action.Disable();
        }
    }

    private static bool TryReadActionFloat(InputActionReference actionRef, out float value)
    {
        return TryReadActionFloat(actionRef != null ? actionRef.action : null, out value);
    }

    private InputReadSource TryReadStylusFloat(string metaActionName, InputActionReference actionRef, out float value)
    {
        if (_preferMetaOpenXRActions && TryReadMetaFloat(metaActionName, out value))
        {
            return InputReadSource.MetaAction;
        }

        if (TryReadActionFloat(actionRef, out value))
        {
            return InputReadSource.InputAction;
        }

        value = 0f;
        return InputReadSource.None;
    }

    private InputReadSource TryReadStylusButton(string metaActionName, InputActionReference actionRef, out bool pressed)
    {
        if (_preferMetaOpenXRActions && TryReadMetaBool(metaActionName, out pressed))
        {
            return InputReadSource.MetaAction;
        }

        if (TryReadActionPressed(actionRef, out pressed))
        {
            return InputReadSource.InputAction;
        }

        pressed = false;
        return InputReadSource.None;
    }

    private static bool TryReadMetaFloat(string actionName, out float value)
    {
        value = 0f;
        return !string.IsNullOrEmpty(actionName) &&
               OVRPlugin.GetActionStateFloat(actionName, out value);
    }

    private static bool TryReadMetaBool(string actionName, out bool value)
    {
        value = false;
        return !string.IsNullOrEmpty(actionName) &&
               OVRPlugin.GetActionStateBoolean(actionName, out value);
    }

    private static bool TryReadMetaPose(string actionName, out Pose pose)
    {
        pose = default;
        if (string.IsNullOrEmpty(actionName) ||
            !OVRPlugin.GetActionStatePose(actionName, out OVRPlugin.Posef actionPose))
        {
            return false;
        }

        pose = new Pose(
            actionPose.Position.FromFlippedZVector3f(),
            actionPose.Orientation.FromFlippedZQuatf());
        return true;
    }

    private static bool TryReadActionFloat(InputAction action, out float value)
    {
        value = 0f;
        if (action == null)
        {
            return false;
        }

        if (action.controls.Count == 0)
        {
            return false;
        }

        if (!action.enabled)
        {
            action.Enable();
        }

        try
        {
            value = action.ReadValue<float>();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryReadActionPressed(InputActionReference actionRef, out bool pressed)
    {
        return TryReadControllerPressed(actionRef != null ? actionRef.action : null, out pressed);
    }

    private static bool TryReadControllerPressed(InputAction action, out bool pressed)
    {
        pressed = false;
        if (action == null)
        {
            return false;
        }

        if (action.controls.Count == 0)
        {
            return false;
        }

        if (!action.enabled)
        {
            action.Enable();
        }

        try
        {
            pressed = action.IsPressed();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void LogInputDiagnostics(bool poseFromMeta, InputReadSource tipSource, InputReadSource frontSource, InputReadSource middleSource, InputReadSource backSource)
    {
        if (!_logInputDiagnostics)
        {
            return;
        }

        string diagnostic = $"pose={(poseFromMeta ? "meta-action" : "transform")}, " +
                            $"tip={SourceLabel(tipSource)}:{_stylus.tip_value:0.00}, " +
                            $"front={SourceLabel(frontSource)}:{_stylus.cluster_front_value}, " +
                            $"middle={SourceLabel(middleSource)}:{_stylus.cluster_middle_value:0.00}, " +
                            $"back={SourceLabel(backSource)}:{_stylus.cluster_back_value}, " +
                            $"dock={_stylus.docked}";

        if (diagnostic == _lastInputDiagnostic)
        {
            return;
        }

        _lastInputDiagnostic = diagnostic;
        Debug.Log($"MX Ink input mapping: {diagnostic}");
    }

    private static string SourceLabel(InputReadSource source)
    {
        switch (source)
        {
            case InputReadSource.MetaAction:
                return "meta-action";
            case InputReadSource.InputAction:
                return "input-action";
            case InputReadSource.LegacyFallback:
                return "legacy-fallback";
            default:
                return "unread";
        }
    }

    private void UpdateButtonVisuals()
    {
        SetRendererColor(_tipRenderer, _stylus.tip_value > 0f ? _activeColor : _defaultColor);
        SetRendererColor(_clusterFrontRenderer, _stylus.cluster_front_value ? _activeColor : _defaultColor);
        SetRendererColor(_clusterMiddleRenderer, _stylus.cluster_middle_value > 0f ? _activeColor : _defaultColor);

        Color backColor = _defaultColor;
        if (_stylus.cluster_back_value)
        {
            backColor = _activeColor;
        }
        else if (_stylus.cluster_back_double_tap_value)
        {
            backColor = _doubleTapActiveColor;
        }

        SetRendererColor(_clusterBackRenderer, backColor);
    }

    private void SetRightControllerModelVisible(bool visible)
    {
        if (_rightController == null || _rightController.model == null)
        {
            return;
        }

        Renderer[] renderers = _rightController.model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
    }

    private static void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private static bool IsProfile(string currentProfile, string expectedProfile)
    {
        return !string.IsNullOrEmpty(currentProfile) &&
               !string.IsNullOrEmpty(expectedProfile) &&
               string.Equals(currentProfile, expectedProfile, StringComparison.OrdinalIgnoreCase);
    }

    public void TriggerHapticPulse(float amplitude, float duration)
    {
        if (!OVRPlugin.TriggerVibrationAction(_hapticActionName, OVRPlugin.Hand.HandRight, duration, amplitude))
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }

    public void TriggerHapticClick()
    {
    }

    public override bool CanDraw()
    {
        return _stylus.isActive && !_stylus.docked;
    }
}
