using UnityEngine;

public class MxInkStylusToolContactSource : MonoBehaviour
{
    [SerializeField] private MxInkTrainingToolBridge _trainingToolBridge;

    private void Awake()
    {
        ResolveBridge();
    }

    private void Reset()
    {
        ResolveBridge();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_trainingToolBridge != null)
        {
            _trainingToolBridge.NotifyStylusTriggerEnter(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_trainingToolBridge != null)
        {
            _trainingToolBridge.NotifyStylusTriggerStay(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_trainingToolBridge != null)
        {
            _trainingToolBridge.NotifyStylusTriggerExit(other);
        }
    }

    private void ResolveBridge()
    {
        if (_trainingToolBridge != null)
        {
            return;
        }

        _trainingToolBridge = GetComponentInParent<MxInkTrainingToolBridge>();
        if (_trainingToolBridge == null)
        {
            _trainingToolBridge = FindObjectOfType<MxInkTrainingToolBridge>();
        }
    }
}
