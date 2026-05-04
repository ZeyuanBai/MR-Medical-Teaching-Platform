using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class RightHandDeviceDetector : MonoBehaviour
{
    public enum RightHandDeviceKind
    {
        Unknown,
        Controller,
        MxInk
    }

    [SerializeField] private bool _logChanges = true;

    private readonly List<InputDevice> _rightHandDevices = new List<InputDevice>();
    private RightHandDeviceKind _currentKind = RightHandDeviceKind.Unknown;
    private string _currentDeviceName = string.Empty;
    private string _currentManufacturer = string.Empty;

    public RightHandDeviceKind CurrentKind => _currentKind;
    public bool IsMxInk => _currentKind == RightHandDeviceKind.MxInk;
    public string CurrentDeviceName => _currentDeviceName;
    public string CurrentManufacturer => _currentManufacturer;

    public event Action<RightHandDeviceKind> DeviceKindChanged;

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        Refresh(true);
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChanged;
        InputDevices.deviceDisconnected -= OnDeviceChanged;
    }

    private void Update()
    {
        Refresh(false);
    }

    private void OnDeviceChanged(InputDevice device)
    {
        Refresh(true);
    }

    private void Refresh(bool forceLog)
    {
        RightHandDeviceKind nextKind = RightHandDeviceKind.Unknown;
        string nextName = string.Empty;
        string nextManufacturer = string.Empty;

        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, _rightHandDevices);
        for (int i = 0; i < _rightHandDevices.Count; i++)
        {
            InputDevice device = _rightHandDevices[i];
            if (!device.isValid)
            {
                continue;
            }

            nextName = device.name ?? string.Empty;
            nextManufacturer = device.manufacturer ?? string.Empty;
            if (IsMxInkDevice(device))
            {
                nextKind = RightHandDeviceKind.MxInk;
                break;
            }

            if ((device.characteristics & InputDeviceCharacteristics.Controller) != 0)
            {
                nextKind = RightHandDeviceKind.Controller;
            }
        }

        bool changed = nextKind != _currentKind || nextName != _currentDeviceName || nextManufacturer != _currentManufacturer;
        if (!changed && !forceLog)
        {
            return;
        }

        _currentKind = nextKind;
        _currentDeviceName = nextName;
        _currentManufacturer = nextManufacturer;

        if (_logChanges && (changed || forceLog))
        {
            Debug.LogError($"Right hand device: {_currentKind} | name='{_currentDeviceName}' | manufacturer='{_currentManufacturer}'");
        }

        if (changed)
        {
            DeviceKindChanged?.Invoke(_currentKind);
        }
    }

    public static bool IsMxInkDevice(InputDevice device)
    {
        string name = (device.name ?? string.Empty).ToLowerInvariant();
        string manufacturer = (device.manufacturer ?? string.Empty).ToLowerInvariant();
        return name.Contains("mx ink") || name.Contains("logitech") || manufacturer.Contains("logitech");
    }
}
