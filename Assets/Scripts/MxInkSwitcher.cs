using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Logitech MX Ink runtime that follows the official OpenXR Input System setup.
/// Attach this to the stylus proxy object, not to XR Origin or the controller roots.
/// </summary>
public class MxInkSwitcher : StylusHandler
{
    private const string LogitechToken = "logitech";
    private const string InputActionsPath = "Assets/Logitech/UnityXR_InputActions/MX_Ink.inputactions";
    private const string TipActionName = "Ink_Tip";
    private const string GrabActionName = "Grab";
    private const string OptionActionName = "Option";
    private const string MiddleActionName = "Ink_MiddleButton";

    [Header("XR Origin References")]
    [SerializeField] private Transform _xrOriginTrackingRoot;
    [SerializeField] private ActionBasedController _rightActionController;

    [Header("Official MX Ink Input Actions")]
    [SerializeField] private InputActionReference _tipActionRef;
    [SerializeField] private InputActionReference _grabActionRef;
    [SerializeField] private InputActionReference _optionActionRef;
    [SerializeField] private InputActionReference _middleActionRef;
    [SerializeField] private bool _enableActionsOnLifecycle = true;

    [Header("Stylus Proxy")]
    [SerializeField] private Transform _stylusProxyRoot;
    [SerializeField] private GameObject _stylusVisualRoot;

    [Header("Controller Visuals Only")]
    [SerializeField] private GameObject _rightControllerVisualRoot;
    [SerializeField] private GameObject _leftControllerVisualRoot;
    [SerializeField] private bool _hideOnlyControllerRenderers = true;

    [Header("Button Visuals")]
    [SerializeField] private Renderer _tipRenderer;
    [SerializeField] private Renderer _clusterFrontRenderer;
    [SerializeField] private Renderer _clusterMiddleRenderer;
    [SerializeField] private Renderer _clusterBackRenderer;
    [SerializeField] private Color _activeColor = Color.gray;
    [SerializeField] private Color _defaultColor = Color.black;

    [Header("Haptics")]
    [SerializeField] private float _hapticClickDuration = 0.011f;
    [SerializeField] private float _hapticClickAmplitude = 1f;

    [Header("Debug")]
    [SerializeField] private bool _logDeviceChanges = true;
    [SerializeField] private bool _logInputDiagnostics;

    public UnityEvent<bool> StylusActiveChanged;

    private readonly RendererStateCache _rightVisualCache = new RendererStateCache();
    private bool _wasActive;
    private string _lastDiagnostic = string.Empty;

    public bool IsStylusActive => _stylus.isActive;

    private void Reset()
    {
        _stylusProxyRoot = transform;
        _stylusVisualRoot = gameObject;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAssignDefaultBindingsInEditor();
    }
#endif

    private void Awake()
    {
        if (_stylusProxyRoot == null)
        {
            _stylusProxyRoot = transform;
        }

        TryAssignBindings();
        CaptureControllerRendererStates();
    }

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;

        SetInputActionsEnabled(true);
        RefreshConnectionState(true);
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;

        SetInputActionsEnabled(false);
        ClearInputs();
        ApplyVisualSwitch(false);
        UpdateButtonVisuals();
    }

    private void Update()
    {
        RefreshConnectionState(false);

        if (!_stylus.isActive)
        {
            ClearInputs();
            UpdateButtonVisuals();
            return;
        }

        RefreshInputs();
        UpdateButtonVisuals();
    }

    private void OnDeviceConnected(UnityEngine.XR.InputDevice device)
    {
        if (IsLogitechDevice(device.name))
        {
            RefreshConnectionState(true);
        }
    }

    private void OnDeviceDisconnected(UnityEngine.XR.InputDevice device)
    {
        if (IsLogitechDevice(device.name))
        {
            RefreshConnectionState(true);
        }
    }

    private void RefreshConnectionState(bool forceEvent)
    {
        bool active = TryGetConnectedRightHandLogitechDevice(out UnityEngine.XR.InputDevice stylusDevice);
        bool activeChanged = active != _wasActive;

        _wasActive = active;
        _stylus.isActive = active;
        _stylus.isOnRightHand = true;

        ApplyVisualSwitch(active);

        if (_logDeviceChanges && (forceEvent || activeChanged))
        {
            string deviceName = active ? stylusDevice.name : "none";
            Debug.Log($"MX Ink switch: active={active}, rightHand=true, device='{deviceName}'");
        }

        if (forceEvent || activeChanged)
        {
            StylusActiveChanged?.Invoke(active);
        }
    }

    private void RefreshInputs()
    {
        Pose pose = ResolveInkingPose();
        _stylus.inkingPose = pose;

        bool tipRead = TryReadActionFloat(_tipActionRef, out float tipValue);
        bool middleRead = TryReadActionFloat(_middleActionRef, out float middleValue);
        bool frontRead = TryReadActionPressed(_grabActionRef, out bool frontPressed);
        bool backRead = TryReadActionPressed(_optionActionRef, out bool backPressed);

        _stylus.tip_value = Mathf.Clamp01(tipValue);
        _stylus.cluster_middle_value = Mathf.Clamp01(middleValue);
        _stylus.cluster_front_value = frontPressed;
        _stylus.cluster_back_value = backPressed;
        _stylus.cluster_back_double_tap_value = false;
        _stylus.docked = false;
        _stylus.any = _stylus.tip_value > 0f ||
                      _stylus.cluster_middle_value > 0f ||
                      _stylus.cluster_front_value ||
                      _stylus.cluster_back_value;

        if (_logInputDiagnostics)
        {
            LogInputDiagnostics(tipRead, middleRead, frontRead, backRead);
        }
    }

    private Pose ResolveInkingPose()
    {
        Transform source = _stylusProxyRoot != null ? _stylusProxyRoot : transform;
        return new Pose(source.position, source.rotation);
    }

    private void ApplyVisualSwitch(bool stylusActive)
    {
        if (_stylusVisualRoot != null && _stylusVisualRoot != gameObject && _stylusVisualRoot.activeSelf != stylusActive)
        {
            _stylusVisualRoot.SetActive(stylusActive);
        }

        SetControllerVisualVisible(_rightControllerVisualRoot, _rightVisualCache, !stylusActive);
        SetControllerVisualVisible(_leftControllerVisualRoot, null, true);
    }

    private void CaptureControllerRendererStates()
    {
        _rightVisualCache.Capture(_rightControllerVisualRoot);
    }

    private void SetControllerVisualVisible(GameObject root, RendererStateCache cache, bool visible)
    {
        if (root == null)
        {
            return;
        }

        if (!_hideOnlyControllerRenderers)
        {
            if (root.activeSelf != visible)
            {
                root.SetActive(visible);
            }

            return;
        }

        if (cache != null)
        {
            cache.Apply(root, visible);
            return;
        }

        if (root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }

    private void ClearInputs()
    {
        _stylus.tip_value = 0f;
        _stylus.cluster_middle_value = 0f;
        _stylus.cluster_front_value = false;
        _stylus.cluster_back_value = false;
        _stylus.cluster_back_double_tap_value = false;
        _stylus.any = false;
        _stylus.docked = false;
    }

    private void UpdateButtonVisuals()
    {
        SetRendererColor(_tipRenderer, _stylus.tip_value > 0f ? _activeColor : _defaultColor);
        SetRendererColor(_clusterFrontRenderer, _stylus.cluster_front_value ? _activeColor : _defaultColor);
        SetRendererColor(_clusterMiddleRenderer, _stylus.cluster_middle_value > 0f ? _activeColor : _defaultColor);
        SetRendererColor(_clusterBackRenderer, _stylus.cluster_back_value ? _activeColor : _defaultColor);
    }

    private static void SetRendererColor(Renderer target, Color color)
    {
        if (target != null)
        {
            target.material.color = color;
        }
    }

    private void LogInputDiagnostics(bool tipRead, bool middleRead, bool frontRead, bool backRead)
    {
        string diagnostic = $"tip={tipRead}:{_stylus.tip_value:0.00}, middle={middleRead}:{_stylus.cluster_middle_value:0.00}, front={frontRead}:{_stylus.cluster_front_value}, back={backRead}:{_stylus.cluster_back_value}";
        if (diagnostic == _lastDiagnostic)
        {
            return;
        }

        _lastDiagnostic = diagnostic;
        Debug.Log($"MX Ink inputs: {diagnostic}");
    }

    private void SetInputActionsEnabled(bool enabled)
    {
        if (!_enableActionsOnLifecycle)
        {
            return;
        }

        SetActionEnabled(_tipActionRef, enabled);
        SetActionEnabled(_grabActionRef, enabled);
        SetActionEnabled(_optionActionRef, enabled);
        SetActionEnabled(_middleActionRef, enabled);
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
        value = 0f;
        InputAction action = actionRef != null ? actionRef.action : null;
        if (action == null || action.controls.Count == 0)
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
        pressed = false;
        InputAction action = actionRef != null ? actionRef.action : null;
        if (action == null || action.controls.Count == 0)
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

    private void TryAdoptSiblingMxInkHandlerBindings()
    {
        if (_tipActionRef != null &&
            _grabActionRef != null &&
            _optionActionRef != null &&
            _middleActionRef != null)
        {
            return;
        }

        Component siblingHandler = GetComponent("MxInkHandler");
        if (siblingHandler == null)
        {
            return;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        _tipActionRef ??= ReadSiblingActionReference(siblingHandler, "_tipActionRef", flags);
        _grabActionRef ??= ReadSiblingActionReference(siblingHandler, "_grabActionRef", flags);
        _optionActionRef ??= ReadSiblingActionReference(siblingHandler, "_optionActionRef", flags);
        _middleActionRef ??= ReadSiblingActionReference(siblingHandler, "_middleActionRef", flags);
    }

    private void TryAssignBindings()
    {
        TryAdoptSiblingMxInkHandlerBindings();
#if UNITY_EDITOR
        TryAssignDefaultBindingsInEditor();
#endif
    }

#if UNITY_EDITOR
    private void TryAssignDefaultBindingsInEditor()
    {
        if (_tipActionRef != null &&
            _grabActionRef != null &&
            _optionActionRef != null &&
            _middleActionRef != null)
        {
            return;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath);
        if (subAssets == null || subAssets.Length == 0)
        {
            return;
        }

        _tipActionRef ??= FindReference(subAssets, TipActionName);
        _grabActionRef ??= FindReference(subAssets, GrabActionName);
        _optionActionRef ??= FindReference(subAssets, OptionActionName);
        _middleActionRef ??= FindReference(subAssets, MiddleActionName);

        if (_tipActionRef == null ||
            _grabActionRef == null ||
            _optionActionRef == null ||
            _middleActionRef == null)
        {
            return;
        }

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }

    private static InputActionReference FindReference(UnityEngine.Object[] subAssets, string actionName)
    {
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is InputActionReference actionReference &&
                actionReference.action != null &&
                actionReference.action.name == actionName)
            {
                return actionReference;
            }
        }

        return null;
    }
#endif

    private static InputActionReference ReadSiblingActionReference(Component siblingHandler, string fieldName, BindingFlags flags)
    {
        FieldInfo field = siblingHandler.GetType().GetField(fieldName, flags);
        return field != null ? field.GetValue(siblingHandler) as InputActionReference : null;
    }

    private static bool IsLogitechDevice(string deviceName)
    {
        return !string.IsNullOrEmpty(deviceName) &&
               deviceName.IndexOf(LogitechToken, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryGetConnectedRightHandLogitechDevice(out UnityEngine.XR.InputDevice stylusDevice)
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevices(devices);

        for (int i = 0; i < devices.Count; i++)
        {
            if (!IsLogitechDevice(devices[i].name))
            {
                continue;
            }

            if ((devices[i].characteristics & InputDeviceCharacteristics.Right) == 0)
            {
                continue;
            }

            stylusDevice = devices[i];
            return true;
        }

        stylusDevice = default;
        return false;
    }

    public void TriggerHapticPulse(float amplitude, float duration)
    {
        if (!_stylus.isActive)
        {
            return;
        }

        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(_stylus.isOnRightHand ? XRNode.RightHand : XRNode.LeftHand);
        if (device.isValid)
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }

    public void TriggerHapticClick()
    {
        TriggerHapticPulse(_hapticClickAmplitude, _hapticClickDuration);
    }

    public override bool CanDraw()
    {
        return _stylus.isActive;
    }

    [Serializable]
    private class RendererStateCache
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private bool[] _initialStates;

        public void Capture(GameObject root)
        {
            if (root == null)
            {
                _renderers = Array.Empty<Renderer>();
                _initialStates = Array.Empty<bool>();
                return;
            }

            _renderers = root.GetComponentsInChildren<Renderer>(true);
            _initialStates = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _initialStates[i] = _renderers[i] != null && _renderers[i].enabled;
            }
        }

        public void Apply(GameObject root, bool visible)
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                Capture(root);
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = visible && (i >= _initialStates.Length || _initialStates[i]);
            }
        }
    }
}
