using UnityEngine;

[DefaultExecutionOrder(110)]
public class MxInkToolTransformConstraint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _targetObject;
    [SerializeField] private GameObject _sourceObject;

    [Header("Offsets")]
    [SerializeField] private Vector3 _positionOffset;
    [SerializeField] private Vector3 _rotationEulerOffset;
    [SerializeField, HideInInspector] private Quaternion _rotationOffset = Quaternion.identity;

    [Header("Restore")]
    [SerializeField] private bool _restoreSourceOnUnbind = true;

    [Header("Binding")]
    [SerializeField] private bool _isBound;
    private bool _hasCapturedSourcePose;
    private Vector3 _capturedLocalPosition;
    private Quaternion _capturedLocalRotation;

    public GameObject TargetObject
    {
        get { return _targetObject; }
    }

    public GameObject SourceObject
    {
        get { return _sourceObject != null ? _sourceObject : gameObject; }
    }

    public bool IsBound
    {
        get { return _isBound; }
    }

    private void Awake()
    {
        if (_sourceObject == null)
        {
            _sourceObject = gameObject;
        }

        SyncRotationOffset();
    }

    private void OnValidate()
    {
        SyncRotationOffset();
    }

    private void LateUpdate()
    {
        if (_isBound)
        {
            UpdateTransform();
        }
    }

    public void BindToTarget()
    {
        if (_targetObject == null || SourceObject == null)
        {
            Debug.LogError("Target or source object is not assigned for MX Ink tool transform constraint.", this);
            return;
        }

        CaptureSourcePose();
        _isBound = true;
        UpdateTransform();
    }

    public void Bind2Target()
    {
        BindToTarget();
    }

    public void Unbind()
    {
        if (!_isBound)
        {
            return;
        }

        _isBound = false;

        if (_restoreSourceOnUnbind)
        {
            RestoreSourcePose();
        }
    }

    [ContextMenu("UpdateTransform")]
    public void UpdateTransform()
    {
        GameObject sourceObject = SourceObject;
        if (_targetObject == null || sourceObject == null)
        {
            return;
        }

        Vector3 worldOffset = ResolveWorldOffset(_targetObject.transform);
        Transform sourceParent = sourceObject.transform.parent;
        Vector3 offsetLocal = sourceParent != null ? sourceParent.InverseTransformVector(worldOffset) : worldOffset;
        Vector3 targetPositionInSourceParent = sourceParent != null
            ? sourceParent.InverseTransformPoint(_targetObject.transform.position)
            : _targetObject.transform.position;
        Quaternion targetRotationInSourceParent = sourceParent != null
            ? Quaternion.Inverse(sourceParent.rotation) * _targetObject.transform.rotation
            : _targetObject.transform.rotation;

        sourceObject.transform.localPosition = targetPositionInSourceParent + offsetLocal;
        sourceObject.transform.localRotation = targetRotationInSourceParent * _rotationOffset;
    }

    public void SetTargetObject(GameObject target)
    {
        _targetObject = target;
    }

    public void SetSourceObject(GameObject source)
    {
        _sourceObject = source;
    }

    public void SetPositionOffset(Vector3 offset)
    {
        _positionOffset = offset;
    }

    public void SetRotationOffset(Quaternion offset)
    {
        _rotationOffset = offset;
        _rotationEulerOffset = offset.eulerAngles;
    }

    public void SetRotationEulerOffset(Vector3 offset)
    {
        _rotationEulerOffset = offset;
        SyncRotationOffset();
    }

    public void SetRestoreSourceOnUnbind(bool restore)
    {
        _restoreSourceOnUnbind = restore;
    }

    private Vector3 ResolveWorldOffset(Transform target)
    {
        return target.right * _positionOffset.x +
               target.up * _positionOffset.y +
               target.forward * _positionOffset.z;
    }

    private void SyncRotationOffset()
    {
        _rotationOffset = Quaternion.Euler(_rotationEulerOffset);
    }

    private void CaptureSourcePose()
    {
        if (_hasCapturedSourcePose || SourceObject == null)
        {
            return;
        }

        Transform sourceTransform = SourceObject.transform;
        _capturedLocalPosition = sourceTransform.localPosition;
        _capturedLocalRotation = sourceTransform.localRotation;
        _hasCapturedSourcePose = true;
    }

    private void RestoreSourcePose()
    {
        if (!_hasCapturedSourcePose || SourceObject == null)
        {
            return;
        }

        Transform sourceTransform = SourceObject.transform;
        sourceTransform.localPosition = _capturedLocalPosition;
        sourceTransform.localRotation = _capturedLocalRotation;
        _hasCapturedSourcePose = false;
    }
}
