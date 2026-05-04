using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class MetaMxInkRuntime : StylusHandler
{
    [Header("Scene References")]
    [SerializeField] private GameObject _mxInkRoot;
    [SerializeField] private ActionBasedController _rightController;

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
    [SerializeField] private string _controllerProfile = "/interaction_profiles/meta/touch_controller_plus";

    [Header("Debug")]
    [SerializeField] private bool _logProfileChanges = true;

    public UnityEvent<bool> StylusActiveChanged;

    private bool _wasActive;
    private string _lastRightProfile = string.Empty;

    public bool IsStylusActive => _stylus.isActive;

    private void OnEnable()
    {
        RefreshProfileAndModels(true);
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

        ClearInputs();
        UpdateButtonVisuals();
    }

    private void RefreshProfileAndModels(bool forceEvent)
    {
        string rightProfile = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandRight) ?? string.Empty;
        bool nextActive = IsProfile(rightProfile, _stylusProfile);
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
    }

    public void TriggerHapticClick()
    {
    }

    public override bool CanDraw()
    {
        return _stylus.isActive && !_stylus.docked;
    }
}
