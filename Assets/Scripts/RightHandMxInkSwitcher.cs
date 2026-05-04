using UnityEngine;

[RequireComponent(typeof(RightHandDeviceDetector))]
public class RightHandMxInkSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _rightHandStylusRoot;
    [SerializeField] private GameObject _rightControllerRoot;

    private RightHandDeviceDetector _detector;

    private void Awake()
    {
        _detector = GetComponent<RightHandDeviceDetector>();
    }

    private void OnEnable()
    {
        if (_detector == null)
        {
            _detector = GetComponent<RightHandDeviceDetector>();
        }

        _detector.DeviceKindChanged += OnDeviceKindChanged;
        Apply(_detector.CurrentKind);
    }

    private void OnDisable()
    {
        if (_detector != null)
        {
            _detector.DeviceKindChanged -= OnDeviceKindChanged;
        }
    }

    private void OnDeviceKindChanged(RightHandDeviceDetector.RightHandDeviceKind kind)
    {
        Apply(kind);
    }

    private void Apply(RightHandDeviceDetector.RightHandDeviceKind kind)
    {
        bool useStylus = kind == RightHandDeviceDetector.RightHandDeviceKind.MxInk;

        if (_rightHandStylusRoot != null)
        {
            _rightHandStylusRoot.SetActive(useStylus);
        }

        if (_rightControllerRoot != null)
        {
            _rightControllerRoot.SetActive(!useStylus);
        }
    }
}
