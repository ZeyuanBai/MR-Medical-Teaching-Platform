using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableInertia : MonoBehaviour
{
    public Rigidbody rigidbody;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }


    void Update()
    {
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector3.zero;
            //rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }
    }
}
