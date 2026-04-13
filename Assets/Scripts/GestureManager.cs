using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    public CentralUIController UIcontroller;

    void Start()
    {
        
    }

    
    void Update()
    {
        if (UIcontroller == null)
        {
            UIcontroller = GameObject.Find("CentralUIController").GetComponent<CentralUIController>();
        }
    }

    public void OnThumbUpPerformed()
    {
        UIcontroller.ResetUIPosition();
    }

    public void OnThumbUpEnded()
    {

    }
}
