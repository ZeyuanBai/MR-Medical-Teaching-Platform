using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using TMPro;
using System;
using System.Linq;
using Unity.Mathematics;
using System.Threading.Tasks;

public class SpatialAnchorManager : MonoBehaviour
{
    [Header("Models")]
    public GameObject SpatialAnchorPreview;
    public GameObject SpatialAnchorPrefab;
    public GameObject SpatialAnchorControlUI;
    public GameObject SkillTrainingModelTable;

    [Header("UIelements")]
    public Button CreateModBtn;
    public Button CreateAnchorBtn;
    public Button LoadAnchorBtn;
    public Button ClearAnchorBtn;
    public Button BindSkillTrainingModelBtn;
    public Button DetermineSkillTrainingModelPositionModelBtn;
    public TMP_Text Logs;
    public TMP_Text CreateModBtnText;

    [Header("Scene Manager")]
    public SkillTrainingManager skillTrainingManager;




    private bool CreateModState = false;

    private List<OVRSpatialAnchor> _anchorInstances = new();

    private HashSet<Guid> _anchorUuids = new();

    private Action<bool, OVRSpatialAnchor.UnboundAnchor> _onLocalized;

    public static SpatialAnchorManager Instance { get; private set; }

    private const string SAVED_UUIDS_KEY = "SavedAnchorUUIDs";

    private static SpatialAnchorManager instance = null;


    void Start()
    {
        // 修改：实现单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _onLocalized = OnLocalized;
        CreateModBtn.onClick.AddListener(OnBtnPressedCreateMod);
        CreateAnchorBtn.onClick.AddListener(OnBtnPressedCreateAnchor);
        LoadAnchorBtn.onClick.AddListener(OnBtnPressedLoadAnchor);
        ClearAnchorBtn.onClick.AddListener(OnBtnPressedClearAnchor);
        BindSkillTrainingModelBtn.onClick.AddListener(OnBtnPressedBindToTrainingTable);
        DetermineSkillTrainingModelPositionModelBtn.onClick.AddListener(OnBtnPressedDetermineSkillTrainingModelPosition);

        SpatialAnchorPreview.SetActive(false);

        LoadSavedUUIDs();
    }

    void Update()
    {
        UpdateAnchorLogs();
    }

    private void LoadSavedUUIDs()
    {
        _anchorUuids = SpatialAnchorStorage.Uuids;
    }

    public void RegisterAnchor(Guid uuid)
    {
        SpatialAnchorStorage.Add(uuid);
    }

    public void OnBtnPressedCreateMod()
    {
        CreateModState = !CreateModState;
        if (CreateModState)
        {
            CreateModBtnText.text = "锚点创建：开启";
            SpatialAnchorPreview.SetActive(true);
        }
        else
        {
            CreateModBtnText.text = "锚点创建：关闭";
            SpatialAnchorPreview.SetActive(false);
        }
    }

    public void OnBtnPressedCreateAnchor()
    {
        if (CreateModState)
        {
            GameObject SpatialAnchor = Instantiate(SpatialAnchorPrefab, SpatialAnchorPreview.transform.position, SpatialAnchorPreview.transform.rotation);
            CreateSpatialAnchor(SpatialAnchor);
        }
    }

    public void OnBtnPressedLoadAnchor()
    {
        LoadAllAnchors();
    }

    public void OnBtnPressedClearAnchor()
    {
        SpatialAnchorStorage.ClearAllUuids();
    }

    public void OnBtnPressedBindToTrainingTable()
    {
        if (!SkillTrainingModelTable.activeSelf)
        {
            SkillTrainingModelTable.SetActive(true);
        }
    }

    async public void OnBtnPressedDetermineSkillTrainingModelPosition()
    {
        if (!SkillTrainingModelTable.activeSelf)
        {
            return;
        }
        else
        {
            GameObject Anchor = Instantiate(SpatialAnchorPrefab, SkillTrainingModelTable.transform.position, SkillTrainingModelTable.transform.rotation);
            await CreateSpatialAnchor(Anchor);
            skillTrainingManager.isModelPositioned = true;
            skillTrainingManager.ModelPosition = SkillTrainingModelTable.transform.position;
            skillTrainingManager.ModelRotation = SkillTrainingModelTable.transform.rotation;
            SpatialAnchor spatialAnchor = Anchor.GetComponent<SpatialAnchor>();
            spatialAnchor.OnBtnPressedPersistAnchor();
        }
    }

    async public Task CreateSpatialAnchor(GameObject spatialAnchor)
    {
        var anchor = spatialAnchor.AddComponent<OVRSpatialAnchor>();
        await anchor.WhenLocalizedAsync();

        if (anchor.Uuid != Guid.Empty)
        {
            _anchorUuids.Add(anchor.Uuid);
            //SaveAnchorsToStorage();
            //Debug.Log($"Anchor created with UUID: {anchor.Uuid}");
        }

        _anchorInstances.Add(anchor);
    }

    private void InstantiateAndBindAnchor(OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        if (!unboundAnchor.TryGetPose(out var pose))
        {
            Debug.LogError("Failed to get anchor pose");
            return;
        }

        GameObject newAnchor = Instantiate(SpatialAnchorPrefab, pose.position, pose.rotation);
        var spatialAnchor = newAnchor.GetComponent<SpatialAnchor>();
        newAnchor.AddComponent<OVRSpatialAnchor>();
        OVRSpatialAnchor ovrAnchor = newAnchor.GetComponent<OVRSpatialAnchor>();
        unboundAnchor.BindTo(ovrAnchor);

    }

    public async void LoadAllAnchors()
    {
        var uuids = SpatialAnchorStorage.Uuids;
        if (uuids.Count == 0) return;

        int batchSize = 50;
        int batchCount = Mathf.CeilToInt((float)uuids.Count / batchSize);

        for (int i = 0; i < batchCount; i++)
        {
            var batch = uuids.Skip(i * batchSize).Take(batchSize);
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();

            var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(batch, unboundAnchors);

            if (result.Success)
            {
                foreach (var anchor in unboundAnchors)
                {
                    anchor.LocalizeAsync().ContinueWith((success, unbound) =>
                    {
                        if (success) InstantiateAndBindAnchor(unbound);
                    }, anchor);
                }
            }
        }
    }


    private void OnLocalized(bool success, OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        if (!success)
        {
            Debug.LogError("Failed to localize anchor");
            return;
        }

        if (unboundAnchor.TryGetPose(out var pose))
        {
            var go = Instantiate(SpatialAnchorPrefab, pose.position, pose.rotation);
            var anchor = go.AddComponent<OVRSpatialAnchor>();
            unboundAnchor.BindTo(anchor);
            _anchorInstances.Add(anchor);
        }
        else
        {
            Debug.LogError("Failed to get anchor pose");
        }
    }

    public void RemoveAnchor(Guid uuid)
    {
        if (_anchorUuids.Contains(uuid))
        {
            _anchorUuids.Remove(uuid);
            //SaveAnchorsToStorage();
        }
    }

    private void UpdateAnchorLogs()
    {
        Logs.text = "持久化锚点： (" + SpatialAnchorStorage.Uuids.Count + "):\n";
        foreach (var uuid in SpatialAnchorStorage.Uuids)
        {
            Logs.text += uuid.ToString() + "\n";
        }
    }

    //private void OnLocalized(bool success, OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    //{
    //    unboundAnchor.TryGetPose(out var pose);
    //    var go = Instantiate(SpatialAnchorPrefab, pose.position, pose.rotation);
    //    var anchor = go.AddComponent<OVRSpatialAnchor>();

    //    unboundAnchor.BindTo(anchor);

    //    // Add the anchor to the running total
    //    _anchorInstances.Add(anchor);
    //}

}
