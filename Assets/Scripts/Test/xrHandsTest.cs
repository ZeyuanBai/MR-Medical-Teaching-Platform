using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class xrHandsTest : MonoBehaviour
{
    public GameObject testCube1;
    public Material Red;
    public Material Green;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void OnThumbUpStarted()
    {
        testCube1.SetActive(true);
    }

    public void OnThumbUpEnded()
    {
        testCube1.SetActive(false);
    }

}
