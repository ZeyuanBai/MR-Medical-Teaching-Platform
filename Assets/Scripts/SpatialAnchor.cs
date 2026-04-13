using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Button = UnityEngine.UI.Button;
using System;

public class SpatialAnchor : MonoBehaviour
{
    public GameObject SpatialAnchorUI;
    public Button PersistAnchorBtn;
    public Button DeleteAnchorBtn;
    public Button DestroyAnchorBtn;
    public Button BindToTrainingTableBtn;
    public TMP_Text Logs;
    private OVRSpatialAnchor OVRAnchor;

    private GameObject SpatialAnchorManager;
    private SpatialAnchorManager anchorManager;

    private GameObject SpatialAnchorManagerInstance;
    private SkillTrainingManager skillTrainingManager;

    

    private bool _isBound = false;

    void Start()
    {
        PersistAnchorBtn.onClick.AddListener(OnBtnPressedPersistAnchor);
        DeleteAnchorBtn.onClick.AddListener(OnBtnPressedDeleteAnchor);
        DestroyAnchorBtn.onClick.AddListener(OnBtnPressedDestroyAnchor);
        BindToTrainingTableBtn.onClick.AddListener(OnBtnPressedBindToTrainingTable);

        SpatialAnchorManager = GameObject.Find("SpatialAnchorManager");
        anchorManager = SpatialAnchorManager.GetComponent<SpatialAnchorManager>();
        skillTrainingManager = GameObject.Find("SkillTrainingManager").GetComponent<SkillTrainingManager>();
        OVRAnchor = GetComponent<OVRSpatialAnchor>();
    }

    void Update()
    {
        if (OVRAnchor == null)
        {
            OVRAnchor = GetComponent<OVRSpatialAnchor>();
        }
        if (SpatialAnchorUI.activeSelf)
        {
            SpatialAnchorUI.transform.LookAt(new Vector3(SpatialAnchorUI.transform.position.x * 2 - Camera.main.transform.position.x,
                                                  SpatialAnchorUI.transform.position.y * 2 - Camera.main.transform.position.y,
                                                  SpatialAnchorUI.transform.position.z * 2 - Camera.main.transform.position.z),
                                                  Vector3.up);
            //SpatialAnchorUI.transform.position = new Vector3(transform.position.x + 16.0,
            //                                                 transform.position.y,
            //                                                 transform.position.z);
        }
        if (OVRAnchor != null && OVRAnchor.Uuid != null)
        {
            Logs.text = "uuid:" + OVRAnchor.Uuid.ToString();
        }
    }

    public void OnBtnPressedPersistAnchor()
    {
        SaveAnchor(OVRAnchor);
    }

    public void OnBtnPressedDeleteAnchor()
    {
        DeleteAnchor(OVRAnchor);
    }

    public void OnBtnPressedDestroyAnchor()
    {
        Destroy(this.gameObject);
    }

    public void OnBtnPressedBindToTrainingTable()
    {
        skillTrainingManager.isModelPositioned = true;
        skillTrainingManager.ModelPosition = transform.position;
        skillTrainingManager.ModelRotation = transform.rotation;
    }



    //public void BindToUnboundAnchor(OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    //{
    //    var anchor = gameObject.AddComponent<OVRSpatialAnchor>();
    //    unboundAnchor.BindTo(anchor);
    //    _isBound = true;
    //    Logs.text += "\nAnchor loaded from storage";
    //}

    async public void SaveAnchor(OVRSpatialAnchor OVRAnchor)
    {
        var result = await OVRAnchor.SaveAnchorAsync();
        if (result.Success)
        {
            SpatialAnchorStorage.Add(OVRAnchor.Uuid);
            //anchorManager.RegisterAnchor(OVRAnchor.Uuid);
            Logs.text += "\nAnchor saved to storage";
        }

    }

    async public void DeleteAnchor(OVRSpatialAnchor OVRAnchor)
    {
        Guid guid = OVRAnchor.Uuid;
        if (OVRAnchor == null)
        {
            Logs.text += "\nAnchor is null";
            return;
        }
        var result = await OVRAnchor.EraseAnchorAsync();
        if (result.Success)
        {
            SpatialAnchorStorage.Remove(guid);
            anchorManager.RemoveAnchor(guid);
            Logs.text += "\nAnchor deleted from storage";
        }
    }

    private void OnDestroy()
    {
        if (OVRAnchor != null && _isBound)
        {
            anchorManager.RemoveAnchor(OVRAnchor.Uuid);
        }
    }
}
