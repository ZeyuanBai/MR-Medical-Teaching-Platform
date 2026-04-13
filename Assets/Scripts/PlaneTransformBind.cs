using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneTransformBind : MonoBehaviour
{
    public GameObject SlicingPlane;
    public GameObject CrossSectionPlane;
    public GameObject MainPlane;
    public int SlicingPlaneIndex = 0;

    void Start()
    {
        
    }

    
    void Update()
    {
        Vector3 mainPosition = MainPlane.transform.position;
        Quaternion mainRotation = MainPlane.transform.rotation;
        SlicingPlane.transform.position = mainPosition;
        //SlicingPlane.transform.rotation = mainRotation;
        CrossSectionPlane.transform.localPosition = new Vector3(0, 0, 0);
        //CrossSectionPlane.transform.position = mainPosition;
        Quaternion crossSectionRotation = Quaternion.Euler(0, 0, 0);
        switch (SlicingPlaneIndex)
        {
            case 0:
                SlicingPlane.transform.rotation = mainRotation; // Quaternion.Euler(0, 0, 0) * mainRotation;
                crossSectionRotation = Quaternion.Euler(-90, 0, 180);
                break;
            case 1:
                Quaternion SagittalOffset = mainRotation * Quaternion.Inverse(Quaternion.Euler(0, -90, 0));
                SlicingPlane.transform.rotation = SagittalOffset * Quaternion.Euler(0, 0, 90); 
                crossSectionRotation = Quaternion.Euler(-90, 0, -90);
                break;
            case 2:
                Quaternion CoronalOffset = mainRotation * Quaternion.Inverse(Quaternion.Euler(180, 180, 0));
                SlicingPlane.transform.rotation = CoronalOffset * Quaternion.Euler(90, 0, 0);
                crossSectionRotation = Quaternion.Euler(270, 180, 0);
                break;
        }
        CrossSectionPlane.transform.localRotation = crossSectionRotation;
        //CrossSectionPlane.transform.rotation = crossSectionRotation * mainRotation;
    }
}
