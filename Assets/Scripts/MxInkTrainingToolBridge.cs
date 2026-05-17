using UnityEngine;

public class MxInkTrainingToolBridge : MonoBehaviour
{
    private enum TrainingTool
    {
        None,
        Marker,
        Scalpel
    }

    [Header("References")]
    [SerializeField] private StylusHandler _stylusRuntime;
    [SerializeField] private Transform _stylusPoseSource;
    [SerializeField] private SkillTrainingManager _trainingManager;
    [SerializeField] private Transform _markerRoot;
    [SerializeField] private Transform _markerTip;
    [SerializeField] private Transform _scalpelRoot;
    [SerializeField] private Transform _scalpelTip;

    [Header("Tool Constraints")]
    [SerializeField] private MxInkToolTransformConstraint _markerConstraint;
    [SerializeField] private MxInkToolTransformConstraint _scalpelConstraint;

    [Header("Activation")]
    [SerializeField] private bool _requirePoseSourceActive = true;
    [SerializeField] private bool _requireStylusContact = true;
    [SerializeField] private bool _allowDistanceContactFallback = true;
    [SerializeField] private float _contactDistanceMeters = 0.035f;

    [Header("Stylus Commands")]
    [SerializeField] private bool _enableStylusCommands = true;
    [SerializeField, Range(0f, 1f)] private float _confirmTipThreshold = 0.65f;
    [SerializeField, Range(0f, 1f)] private float _retryMiddleThreshold = 0.65f;
    [SerializeField] private bool _frontConfirms = true;
    [SerializeField] private bool _backRetries = true;

    [Header("Feedback")]
    [SerializeField] private bool _enableFeedback = true;
    [SerializeField] private float _hapticAmplitude = 0.35f;
    [SerializeField] private float _hapticDuration = 0.035f;

    [Header("Proxy Visibility")]
    [SerializeField] private bool _manageProxyVisibility = true;
    [SerializeField] private bool _hideStylusVisualWhileProxying = true;
    [SerializeField] private bool _showActiveToolWhileProxying = true;
    [SerializeField] private VisibilityMode _visibilityMode = VisibilityMode.Renderers;
    [SerializeField] private GameObject _stylusVisualRoot;
    [SerializeField] private GameObject _markerVisualRoot;
    [SerializeField] private GameObject _scalpelVisualRoot;

    [Header("Debug")]
    [SerializeField] private bool _logStateChanges;

    private VisibilityState _stylusVisibilityState;
    private VisibilityState _markerVisibilityState;
    private VisibilityState _scalpelVisibilityState;
    private TrainingTool _activeTool;
    private bool _referencesResolved;
    private bool _lastConfirmInput;
    private bool _lastRetryInput;
    private bool _lastContactFeedback;
    private TrainingTool _contactedTool;
    private Collider _contactedCollider;

    private enum VisibilityMode
    {
        Renderers,
        GameObjectActive
    }

    private void Reset()
    {
        _stylusRuntime = FindObjectOfType<StylusHandler>();
        _stylusPoseSource = _stylusRuntime != null ? _stylusRuntime.transform : transform;
        _trainingManager = FindObjectOfType<SkillTrainingManager>();
        ResolveReferences(true);
    }

    private void Awake()
    {
        ResolveReferences(true);
        EnsureConstraint(ref _markerConstraint, _markerRoot, "MX Ink Marker Constraint");
        EnsureConstraint(ref _scalpelConstraint, _scalpelRoot, "MX Ink Scalpel Constraint");
        ConfigureConstraints();

        _stylusVisibilityState = new VisibilityState(_stylusVisualRoot);
        _markerVisibilityState = new VisibilityState(_markerVisualRoot != null ? _markerVisualRoot : (_markerRoot != null ? _markerRoot.gameObject : null));
        _scalpelVisibilityState = new VisibilityState(_scalpelVisualRoot != null ? _scalpelVisualRoot : (_scalpelRoot != null ? _scalpelRoot.gameObject : null));
    }

    private void OnDisable()
    {
        ReleaseToolProxy(true);
        _lastConfirmInput = false;
        _lastRetryInput = false;
        _lastContactFeedback = false;
        ClearStylusToolContact(null);
    }

    public void NotifyStylusTriggerEnter(Collider other)
    {
        UpdateStylusToolContact(other, true);
    }

    public void NotifyStylusTriggerStay(Collider other)
    {
        UpdateStylusToolContact(other, false);
    }

    public void NotifyStylusTriggerExit(Collider other)
    {
        if (other == null || other != _contactedCollider)
        {
            return;
        }

        ClearStylusToolContact(other);
    }

    private void Update()
    {
        ResolveReferences(false);
        HandleStylusCommands();
    }

    private void LateUpdate()
    {
        ResolveReferences(false);
        ConfigureConstraints();

        TrainingTool desiredTool = ResolveDesiredTool();
        if (desiredTool == TrainingTool.None)
        {
            ReleaseToolProxy(false);
            return;
        }

        DriveToolProxy(desiredTool);
    }

    private void ResolveReferences(bool allowSceneSearch)
    {
        if (_referencesResolved && AreRequiredReferencesAlive())
        {
            return;
        }

        _referencesResolved = false;

        if (_stylusRuntime == null && allowSceneSearch)
        {
            _stylusRuntime = FindObjectOfType<StylusHandler>();
        }

        if (_stylusPoseSource == null && _stylusRuntime != null)
        {
            _stylusPoseSource = _stylusRuntime.transform;
        }

        if (_trainingManager == null && allowSceneSearch)
        {
            _trainingManager = FindObjectOfType<SkillTrainingManager>();
        }

        if (_trainingManager == null)
        {
            return;
        }

        if (_markerRoot == null && _trainingManager.Marker != null)
        {
            _markerRoot = _trainingManager.Marker.transform;
        }

        if (_scalpelRoot == null && _trainingManager.Scalpel != null)
        {
            _scalpelRoot = _trainingManager.Scalpel.transform;
        }

        if (_markerTip == null &&
            _trainingManager.positionDetermination != null &&
            _trainingManager.positionDetermination.MarkerTip != null)
        {
            _markerTip = _trainingManager.positionDetermination.MarkerTip.transform;
        }

        if (_scalpelTip == null &&
            _trainingManager.cutSkin != null &&
            _trainingManager.cutSkin.ScalpelTip != null)
        {
            _scalpelTip = _trainingManager.cutSkin.ScalpelTip.transform;
        }

        if (_scalpelTip == null &&
            _trainingManager.cutAirway != null &&
            _trainingManager.cutAirway.ScalpelTip != null)
        {
            _scalpelTip = _trainingManager.cutAirway.ScalpelTip.transform;
        }

        if (_markerVisualRoot == null)
        {
            _markerVisualRoot = _trainingManager.Marker;
        }

        if (_scalpelVisualRoot == null)
        {
            _scalpelVisualRoot = _trainingManager.Scalpel;
        }

        _referencesResolved = _stylusRuntime != null &&
                              _stylusPoseSource != null &&
                              _trainingManager != null &&
                              _markerRoot != null &&
                              _markerTip != null &&
                              _scalpelRoot != null &&
                              _scalpelTip != null;
    }

    private bool AreRequiredReferencesAlive()
    {
        return _stylusRuntime != null &&
               _stylusPoseSource != null &&
               _trainingManager != null &&
               _markerRoot != null &&
               _markerTip != null &&
               _scalpelRoot != null &&
               _scalpelTip != null;
    }

    private TrainingTool ResolveDesiredTool()
    {
        if (!IsStylusAvailable())
        {
            return TrainingTool.None;
        }

        TrainingTool stepTool = ResolveToolForCurrentStep();
        if (stepTool == TrainingTool.None)
        {
            return TrainingTool.None;
        }

        if (!_requireStylusContact || _activeTool == stepTool || HasStylusContact(stepTool))
        {
            return stepTool;
        }

        return TrainingTool.None;
    }

    private bool IsStylusAvailable()
    {
        if (_stylusRuntime == null || _stylusPoseSource == null)
        {
            return false;
        }

        StylusInputs stylus = _stylusRuntime.CurrentState;
        return stylus.isActive &&
               _stylusRuntime.CanDraw() &&
               (!_requirePoseSourceActive || _stylusPoseSource.gameObject.activeInHierarchy);
    }

    private TrainingTool ResolveToolForCurrentStep()
    {
        if (_trainingManager == null)
        {
            return TrainingTool.None;
        }

        switch (_trainingManager.CurrentStep)
        {
            case SkillTrainingManager.TrainingStep.Step1_PositionDetermination:
                return _markerRoot != null && _markerTip != null ? TrainingTool.Marker : TrainingTool.None;
            case SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue:
            case SkillTrainingManager.TrainingStep.Step3_CutAirway:
                return _scalpelRoot != null && _scalpelTip != null ? TrainingTool.Scalpel : TrainingTool.None;
            default:
                return TrainingTool.None;
        }
    }

    private bool HasStylusContact(TrainingTool tool)
    {
        if (_contactedTool == tool)
        {
            PlayContactFeedbackOnce(tool);
            return true;
        }

        if (!_allowDistanceContactFallback)
        {
            _lastContactFeedback = false;
            return false;
        }

        Transform tip = tool == TrainingTool.Marker ? _markerTip : _scalpelTip;
        if (tip == null || _stylusPoseSource == null)
        {
            return false;
        }

        Collider[] toolColliders = tip.GetComponentsInChildren<Collider>(true);
        Vector3 stylusPosition = _stylusPoseSource.position;
        for (int i = 0; i < toolColliders.Length; i++)
        {
            Collider collider = toolColliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            Vector3 closestPoint = collider.ClosestPoint(stylusPosition);
            if (Vector3.Distance(closestPoint, stylusPosition) <= _contactDistanceMeters)
            {
                PlayContactFeedbackOnce(tool);
                return true;
            }
        }

        if (Vector3.Distance(stylusPosition, tip.position) <= _contactDistanceMeters)
        {
            PlayContactFeedbackOnce(tool);
            return true;
        }

        _lastContactFeedback = false;
        return false;
    }

    private void UpdateStylusToolContact(Collider other, bool playFeedback)
    {
        TrainingTool tool = ResolveToolFromCollider(other);
        if (tool == TrainingTool.None)
        {
            return;
        }

        _contactedTool = tool;
        _contactedCollider = other;

        if (playFeedback)
        {
            PlayContactFeedbackOnce(tool);
        }
    }

    private void ClearStylusToolContact(Collider other)
    {
        if (other != null && other != _contactedCollider)
        {
            return;
        }

        _contactedTool = TrainingTool.None;
        _contactedCollider = null;
        _lastContactFeedback = false;
    }

    private TrainingTool ResolveToolFromCollider(Collider other)
    {
        if (other == null)
        {
            return TrainingTool.None;
        }

        TrainingTool stepTool = ResolveToolForCurrentStep();
        if (stepTool == TrainingTool.None)
        {
            return TrainingTool.None;
        }

        Transform otherTransform = other.transform;
        if (stepTool == TrainingTool.Marker && IsTransformInTool(otherTransform, _markerRoot, _markerTip))
        {
            return TrainingTool.Marker;
        }

        if (stepTool == TrainingTool.Scalpel && IsTransformInTool(otherTransform, _scalpelRoot, _scalpelTip))
        {
            return TrainingTool.Scalpel;
        }

        return TrainingTool.None;
    }

    private static bool IsTransformInTool(Transform candidate, Transform root, Transform tip)
    {
        return IsSameOrChildOf(candidate, root) || IsSameOrChildOf(candidate, tip);
    }

    private static bool IsSameOrChildOf(Transform candidate, Transform parent)
    {
        return candidate != null && parent != null && (candidate == parent || candidate.IsChildOf(parent));
    }

    private void DriveToolProxy(TrainingTool tool)
    {
        if (_activeTool != tool)
        {
            ReleaseToolProxy(false);
            _activeTool = tool;
            BindConstraint(tool);
            ApplyProxyVisibility(tool);
            LogStateChange($"MX Ink training bridge: proxying {tool}.");
        }
    }

    private void BindConstraint(TrainingTool tool)
    {
        MxInkToolTransformConstraint constraint = tool == TrainingTool.Marker ? _markerConstraint : _scalpelConstraint;
        if (constraint == null)
        {
            return;
        }

        constraint.BindToTarget();
    }

    private void ReleaseToolProxy(bool forceRestore)
    {
        if (_activeTool == TrainingTool.None && !forceRestore)
        {
            return;
        }

        if (_markerConstraint != null)
        {
            _markerConstraint.Unbind();
        }

        if (_scalpelConstraint != null)
        {
            _scalpelConstraint.Unbind();
        }

        RestoreProxyVisibility();
        LogStateChange("MX Ink training bridge: released tool proxy.");
        _activeTool = TrainingTool.None;
        _lastContactFeedback = false;
    }

    private void ConfigureConstraints()
    {
        ConfigureConstraint(_markerConstraint, _markerRoot, _stylusPoseSource);
        ConfigureConstraint(_scalpelConstraint, _scalpelRoot, _stylusPoseSource);
    }

    private static void ConfigureConstraint(
        MxInkToolTransformConstraint constraint,
        Transform source,
        Transform target)
    {
        if (constraint == null)
        {
            return;
        }

        constraint.SetSourceObject(source != null ? source.gameObject : constraint.gameObject);
        constraint.SetTargetObject(target != null ? target.gameObject : null);
        constraint.SetRestoreSourceOnUnbind(true);
    }

    private void EnsureConstraint(ref MxInkToolTransformConstraint constraint, Transform source, string nameSuffix)
    {
        if (constraint != null)
        {
            return;
        }

        GameObject host = source != null ? source.gameObject : gameObject;
        constraint = host.GetComponent<MxInkToolTransformConstraint>();
        if (constraint != null)
        {
            return;
        }

        constraint = host.AddComponent<MxInkToolTransformConstraint>();
        constraint.name = nameSuffix;
    }

    private void HandleStylusCommands()
    {
        if (!_enableStylusCommands || _trainingManager == null || !IsStylusAvailable())
        {
            _lastConfirmInput = false;
            _lastRetryInput = false;
            return;
        }

        StylusInputs stylus = _stylusRuntime.CurrentState;
        bool confirmInput = stylus.tip_value >= _confirmTipThreshold || (_frontConfirms && stylus.cluster_front_value);
        bool retryInput = stylus.cluster_middle_value >= _retryMiddleThreshold || (_backRetries && stylus.cluster_back_value);

        if (confirmInput && !_lastConfirmInput)
        {
            TryConfirmCurrentStep();
        }

        if (retryInput && !_lastRetryInput)
        {
            TryRetryCurrentStep();
        }

        _lastConfirmInput = confirmInput;
        _lastRetryInput = retryInput;
    }

    private void TryConfirmCurrentStep()
    {
        try
        {
            switch (_trainingManager.CurrentStep)
            {
                case SkillTrainingManager.TrainingStep.Step1_PositionDetermination:
                    _trainingManager.OnBtnPressedDetermineDrawPosition();
                    PlaySuccessFeedback();
                    break;
                case SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue:
                    _trainingManager.OnBtnPressedCutSkinOver();
                    PlaySuccessFeedback();
                    break;
                case SkillTrainingManager.TrainingStep.Step3_CutAirway:
                    _trainingManager.OnBtnPressedCutAirwayOver();
                    PlaySuccessFeedback();
                    break;
                default:
                    PlayInvalidFeedback("MX Ink training bridge: confirm ignored outside steps 1-3.");
                    break;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"MX Ink training bridge feedback/command failed without blocking training: {exception.Message}", this);
        }
    }

    private void TryRetryCurrentStep()
    {
        try
        {
            switch (_trainingManager.CurrentStep)
            {
                case SkillTrainingManager.TrainingStep.Step1_PositionDetermination:
                    _trainingManager.OnBtnPressedClearDrawTexture();
                    PlaySuccessFeedback();
                    break;
                case SkillTrainingManager.TrainingStep.Step2_CutSkinAndTissue:
                    _trainingManager.OnBtnPressedCutSkinRetry();
                    PlaySuccessFeedback();
                    break;
                case SkillTrainingManager.TrainingStep.Step3_CutAirway:
                    _trainingManager.OnBtnPressedCutAirwayRetry();
                    PlaySuccessFeedback();
                    break;
                default:
                    PlayInvalidFeedback("MX Ink training bridge: retry ignored outside steps 1-3.");
                    break;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"MX Ink training bridge feedback/command failed without blocking training: {exception.Message}", this);
        }
    }

    private void PlayContactFeedbackOnce(TrainingTool tool)
    {
        if (_lastContactFeedback)
        {
            return;
        }

        _lastContactFeedback = true;
        PlayFeedback($"MX Ink training bridge: valid {tool} contact.");
    }

    private void PlaySuccessFeedback()
    {
        PlayFeedback("MX Ink training bridge: command accepted.");
    }

    private void PlayInvalidFeedback(string message)
    {
        PlayFeedback(message);
    }

    private void PlayFeedback(string message)
    {
        if (!_enableFeedback)
        {
            return;
        }

        try
        {
            if (_stylusRuntime is MxInkSwitcher switcher)
            {
                switcher.TriggerHapticPulse(_hapticAmplitude, _hapticDuration);
            }
            else if (_stylusRuntime is MetaMxInkRuntime metaRuntime)
            {
                metaRuntime.TriggerHapticPulse(_hapticAmplitude, _hapticDuration);
            }

            Debug.Log(message, this);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"MX Ink training bridge feedback failed: {exception.Message}", this);
        }
    }

    private void ApplyProxyVisibility(TrainingTool activeTool)
    {
        if (!_manageProxyVisibility)
        {
            return;
        }

        if (_hideStylusVisualWhileProxying)
        {
            _stylusVisibilityState.Refresh(_stylusVisualRoot);
            _stylusVisibilityState.SetVisible(false, _visibilityMode);
        }

        if (_showActiveToolWhileProxying)
        {
            _markerVisibilityState.Refresh(_markerVisualRoot != null ? _markerVisualRoot : (_markerRoot != null ? _markerRoot.gameObject : null));
            _scalpelVisibilityState.Refresh(_scalpelVisualRoot != null ? _scalpelVisualRoot : (_scalpelRoot != null ? _scalpelRoot.gameObject : null));

            if (activeTool == TrainingTool.Marker)
            {
                _markerVisibilityState.SetVisible(true, _visibilityMode);
            }
            else if (activeTool == TrainingTool.Scalpel)
            {
                _scalpelVisibilityState.SetVisible(true, _visibilityMode);
            }
        }
    }

    private void RestoreProxyVisibility()
    {
        if (!_manageProxyVisibility)
        {
            return;
        }

        _stylusVisibilityState.Restore(_visibilityMode);
        _markerVisibilityState.Restore(_visibilityMode);
        _scalpelVisibilityState.Restore(_visibilityMode);
    }

    private void LogStateChange(string message)
    {
        if (_logStateChanges)
        {
            Debug.Log(message, this);
        }
    }

    private class VisibilityState
    {
        private GameObject _root;
        private bool _captured;
        private bool _driving;
        private bool _rootActiveSelf;
        private Renderer[] _renderers;
        private bool[] _rendererEnabled;

        public VisibilityState(GameObject root)
        {
            _root = root;
            Capture();
        }

        public void Refresh(GameObject root)
        {
            if (_root == root && _captured)
            {
                return;
            }

            _root = root;
            _captured = false;
            _driving = false;
            Capture();
        }

        public void SetVisible(bool visible, VisibilityMode mode)
        {
            if (_root == null)
            {
                return;
            }

            Capture();
            if (mode == VisibilityMode.GameObjectActive)
            {
                if (_root.activeSelf != visible)
                {
                    _root.SetActive(visible);
                }
            }
            else
            {
                if (_renderers == null || _renderers.Length == 0)
                {
                    CaptureRenderers();
                }

                for (int i = 0; i < _renderers.Length; i++)
                {
                    Renderer renderer = _renderers[i];
                    if (renderer != null)
                    {
                        renderer.enabled = visible && (i >= _rendererEnabled.Length || _rendererEnabled[i]);
                    }
                }
            }

            _driving = true;
        }

        public void Restore(VisibilityMode mode)
        {
            if (!_driving || _root == null)
            {
                return;
            }

            if (mode == VisibilityMode.GameObjectActive)
            {
                if (_root.activeSelf != _rootActiveSelf)
                {
                    _root.SetActive(_rootActiveSelf);
                }
            }
            else if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    Renderer renderer = _renderers[i];
                    if (renderer != null)
                    {
                        renderer.enabled = i < _rendererEnabled.Length && _rendererEnabled[i];
                    }
                }
            }

            _driving = false;
        }

        private void Capture()
        {
            if (_captured || _root == null)
            {
                return;
            }

            _rootActiveSelf = _root.activeSelf;
            CaptureRenderers();
            _captured = true;
        }

        private void CaptureRenderers()
        {
            if (_root == null)
            {
                _renderers = System.Array.Empty<Renderer>();
                _rendererEnabled = System.Array.Empty<bool>();
                return;
            }

            _renderers = _root.GetComponentsInChildren<Renderer>(true);
            _rendererEnabled = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _rendererEnabled[i] = _renderers[i] != null && _renderers[i].enabled;
            }
        }
    }
}
