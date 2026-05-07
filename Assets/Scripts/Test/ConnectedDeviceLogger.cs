using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ConnectedDeviceLogger : MonoBehaviour
{
    private void Awake()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;

        LogCurrentDevices();
    }

    private void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    private void LogCurrentDevices()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        if (devices.Count == 0)
        {
            Debug.LogError("No XR input devices connected.");
            return;
        }

        foreach (var device in devices)
        {
            Debug.LogError($"Current device: {device.name}");
        }
    }

    private void OnDeviceConnected(InputDevice device)
    {
        Debug.LogError($"Device connected: {device.name}");
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        Debug.LogError($"Device disconnected: {device.name}");
    }
}
