using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LockGrabRotationY : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private bool lockX, lockY, lockZ;
    private Vector3 initialEulerAngles;

    private void Awake()
    {
        grabInteractable.onSelectEntered.AddListener(OnGrab);
        grabInteractable.onSelectExited.AddListener(OnRelease);
        initialEulerAngles = transform.eulerAngles;
    }

    private void OnGrab(XRBaseInteractor interactor)
    {
        // 记录初始旋转
        initialEulerAngles = transform.eulerAngles;
    }

    private void Update()
    {
        if (grabInteractable.isSelected)
        {
            Vector3 currentEuler = transform.eulerAngles;
            if (lockX) currentEuler.x = initialEulerAngles.x;
            if (lockY) currentEuler.y = initialEulerAngles.y;
            if (lockZ) currentEuler.z = initialEulerAngles.z;
            transform.eulerAngles = currentEuler;
        }
    }

    private void OnRelease(XRBaseInteractor interactor)
    {
        // 可选：释放后恢复自由旋转
    }
}
