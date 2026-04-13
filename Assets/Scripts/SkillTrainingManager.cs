using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using TMPro;
using PaintIn3D;
using PaintCore;

public class SkillTrainingManager : MonoBehaviour
{
    [Header("UI Control")]
    public CentralUIController centralUIController;
    public TrainingReportManager trainingReportManager;

    [Header("Models")]
    public GameObject SkillTrainingModelTable;
    public GameObject Skin;
    public GameObject Bone;
    public GameObject Airway;
    public GameObject Brain;
    public GameObject Tissue;
    public GameObject SkinCut;
    public GameObject AirwayCut;
    public GameObject TissueCut;
    public GameObject NeckSkin;
    public GameObject Marker;
    public GameObject Scalpel;
    public GameObject Tracheal;
    public GameObject MarkerPosition;
    private Vector3 MarkerInitPos;
    public GameObject ScalpelPosition;
    private Vector3 ScalpelInitPos;
    public GameObject TrachealTubePosition;
    private Vector3 TrachealTubeInitPos;
    public GameObject MarkerTipVisual;
    public GameObject MarkerBodyVisual;
    public GameObject ScalpelVisual;
    public GameObject TrachealVisual;

    [Header("UIelements")]
    public TMP_Text Logs;

    public Button SkillTrainingStartBtn;
    public Button SkillTrainingResetBtn;
    public Button TransparentModeBtn;
    public TMP_Text TransparentModeBtnText;
    public Button ViewReportBtn;

    public Button DrawTextureClearBtn;
    public Button DetermineDrawPositionBtn;
    public Button CutSkinRetryBtn;
    public Button CutSkinOverBtn;
    public Button CutAirwayRetryBtn;
    public Button CutAirwayOverBtn;
    public Button TrachealReplaceBtn;
    public Button InsertionOverBtn;

    //public Button TestBtn;

    [Header("PaintIn3D")]
    public CwButtonClearAll CwClearAll;

    [Header("Step Status")]
    [HideInInspector]
    public TrainingStep CurrentStep = TrainingStep.Idle;
    [HideInInspector]
    public bool TransparentMode = false;
    [HideInInspector]
    public float TrainingTime = 0f;
    private bool isTimerActive = false;

    [Header("Step 1 Components")]
    public PositionDetermination positionDetermination;
    public GameObject DrawRegion;

    [Header("Step 2 Components")]
    public CutSkin cutSkin;
    public GameObject CutRegion;

    [Header("Step 3 Components")]
    public CutAirway cutAirway;
    public GameObject AirwayRegion;

    [Header("Step 4 Components")]
    public InsertTracheal insertTracheal;
    public GameObject TrachealRegion;

    [Header("Materials")]
    public Material SkinMat;
    public Material BoneMat;
    public Material AirwayMat;
    public Material BrainMat;
    public Material TissueMat;
    public Material TranspSkinMat;
    public Material TranspBoneMat;
    public Material TranspAirwayMat;
    public Material TranspBrainMat;
    public Material TransTissueMat;
    public Material HighLightMat;

    // 日志设置
    private int maxLogCount = 5; // 最大日志条数
    private Queue<string> logQueue = new Queue<string>(); // 日志队列

    // 模型空间定位
    [HideInInspector]
    public bool isModelPositioned = false;
    [HideInInspector]
    public Vector3 ModelPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Quaternion ModelRotation = new Quaternion(0, 0, 0, 0);

    public enum TrainingStep
    {
        Idle = 0,
        Step1_PositionDetermination,
        Step2_CutSkinAndTissue,
        Step3_CutAirway,
        Step4_InsertTracheal
    }

    void Start()
    {
        SkillTrainingStartBtn.onClick.AddListener(OnBtnPressedStartSkillTraining);
        SkillTrainingResetBtn.onClick.AddListener(OnBtnPressedResetSkillTraining);
        TransparentModeBtn.onClick.AddListener(OnBtnPressedTransparentMode);
        ViewReportBtn.onClick.AddListener(OnBtnPressedViewReport);

        DrawTextureClearBtn.onClick.AddListener(OnBtnPressedClearDrawTexture);
        DetermineDrawPositionBtn.onClick.AddListener(OnBtnPressedDetermineDrawPosition);
        CutSkinRetryBtn.onClick.AddListener(OnBtnPressedCutSkinRetry);
        CutSkinOverBtn.onClick.AddListener(OnBtnPressedCutSkinOver);
        CutAirwayRetryBtn.onClick.AddListener(OnBtnPressedCutAirwayRetry);
        CutAirwayOverBtn.onClick.AddListener(OnBtnPressedCutAirwayOver);
        TrachealReplaceBtn.onClick.AddListener(OnBtnPressedTrachealReplace);
        InsertionOverBtn.onClick.AddListener(OnBtnPressedInsertionOver);

        //TestBtn.onClick.AddListener(OnBtnPressedTest);

        positionDetermination = GameObject.Find("DrawRegion").GetComponent<PositionDetermination>();

        ResetTraining();
    }

    void Update()
    {
        UpdateModelPosition();
        MarkerInitPos = MarkerPosition.transform.position;
        ScalpelInitPos = ScalpelPosition.transform.position;
        TrachealTubeInitPos = TrachealTubePosition.transform.position;

        if (isTimerActive)
        {
            TrainingTime += Time.deltaTime;
        }
    }

    private void UpdateModelPosition()
    { 
        if (isModelPositioned)
        {
            SkillTrainingModelTable.transform.position = ModelPosition;
            SkillTrainingModelTable.transform.rotation = ModelRotation;
        }
    }


    private IEnumerator TrainingFlow()
    {
        while ((int)CurrentStep <= (int)TrainingStep.Step4_InsertTracheal)
        {
            switch (CurrentStep)
            {
                case TrainingStep.Step1_PositionDetermination:
                    SetLogInfo("步骤1：确定切割位置");
                    SetLogInfo("选择环状软骨下方第2-3气管软骨环为气管切开位置（约胸骨上窝上方2-3cm），用笔画出纵向切割位置");
                    //Logs.text += "\nSkill Training State: Step 1 - Determine cut position";
                    yield return StartCoroutine(ExecuteStep1());
                    break;

                case TrainingStep.Step2_CutSkinAndTissue:
                    SetLogInfo("步骤2：切开皮肤和组织");
                    SetLogInfo("沿颈部正中线做垂直切口，长度约3-4cm（上起环状软骨下缘，下至胸骨上窝上方）");
                    //Logs.text += "\nSkill Training State: Step 2 - Cut skin and tissue";
                    yield return StartCoroutine(ExecuteStep2());
                    break;

                case TrainingStep.Step3_CutAirway:
                    SetLogInfo("步骤3：切开气管");
                    SetLogInfo("用手术刀水平横向切开气管前壁，切口长度约1-1.5cm");
                    //Logs.text += "\nSkill Training State: Step 3 - Cut airway";
                    yield return StartCoroutine(ExecuteStep3());
                    break;

                case TrainingStep.Step4_InsertTracheal:
                    SetLogInfo("步骤4：插入气管套管");
                    SetLogInfo("将气管套管插入气管内，注意保持气管套管与气管平行，避免损伤后壁");
                    //Logs.text += "\nSkill Training State: Step 4 - Insert the tracheal into the trachea";
                    yield return StartCoroutine(ExecuteStep4());
                    ResetTraining();
                    yield break;
            }
            NextStep();
        }
    }

    private IEnumerator ExecuteStep1()
    {
        Marker.SetActive(true);
        Marker.transform.position = MarkerInitPos;
        Scalpel.SetActive(false);
        Tracheal.SetActive(false);

        //Add_Material(MarkerBodyVisual, HighLightMat);
        //Add_Material(MarkerTipVisual, HighLightMat);
        MarkerBodyVisual.GetComponent<HighLightDisplay>().Add_Material();
        MarkerTipVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(positionDetermination.WaitForCollisionAndCalculate());
        

        //yield return new WaitForSeconds(3f);
    }

    private IEnumerator ExecuteStep2()
    {
        Marker.SetActive(false);
        Scalpel.SetActive(true);
        Scalpel.transform.position = ScalpelInitPos;
        Tracheal.SetActive(false);

        ScalpelVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(cutSkin.WaitForCollisionAndCut());
        Skin.SetActive(false);
        Tissue.SetActive(false);
        SkinCut.SetActive(true);
        TissueCut.SetActive(true);
        NeckSkin.SetActive(false);
        CwClearAll.ClearAll();
        //yield return new WaitForSeconds(3f);
    }

    private IEnumerator ExecuteStep3()
    {
        ScalpelVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(cutAirway.WaitForCollisionAndCut());
        Airway.SetActive(false);
        AirwayCut.SetActive(true);
        //yield return new WaitForSeconds(3f);
    }

    private IEnumerator ExecuteStep4()
    {
        Marker.SetActive(false);
        Scalpel.SetActive(false);
        Tracheal.SetActive(true);
        Tracheal.transform.position = TrachealTubeInitPos;

        TrachealVisual.GetComponent<HighLightDisplay>().Add_Material();

        yield return StartCoroutine(insertTracheal.WaitForCollision());

        //yield return new WaitForSeconds(3f);
    }


    public void OnBtnPressedStartSkillTraining()
    {
        if (CurrentStep != TrainingStep.Idle) return;
        CurrentStep = TrainingStep.Step1_PositionDetermination;
        StartCoroutine(TrainingFlow());
        trainingReportManager.isTrainingOver = false;
        // 启动计时
        TrainingTime = 0f;
        isTimerActive = true;
    }

    private void NextStep()
    {
        if (CurrentStep >= TrainingStep.Step4_InsertTracheal) return;
        CurrentStep++;
    }

    public void ActiveMedicalInstruments(bool active)
    {
        Marker.SetActive(active);
        Scalpel.SetActive(active);
        Tracheal.SetActive(active);
        if (active)
        {
            Marker.transform.position = MarkerInitPos;
            Scalpel.transform.position = ScalpelInitPos;
            Tracheal.transform.position = TrachealTubeInitPos;
        }
    }

    public void OnBtnPressedClearDrawTexture()
    {
        CwClearAll.ClearAll();
        positionDetermination.ResetStep1();
        positionDetermination.isPositionDetermined = false;
    }

    public void OnBtnPressedDetermineDrawPosition()
    {
        if (CurrentStep != TrainingStep.Step1_PositionDetermination) return;    
        positionDetermination.isPositionDetermined = true;
        cutSkin.isCutOver = false;
        cutSkin.ResetStep2();
    }

    public void OnBtnPressedCutSkinRetry()
    {
        cutSkin.isCutOver = false;
        cutSkin.ResetStep2();
    }

    public void OnBtnPressedCutSkinOver()
    {
        if (CurrentStep != TrainingStep.Step2_CutSkinAndTissue) return;
        cutSkin.isCutOver = true;
        cutAirway.isCutOver = false;
        cutAirway.ResetStep3();
    }

    public void OnBtnPressedCutAirwayRetry()
    {
        cutAirway.isCutOver = false;
        cutAirway.ResetStep3();
    }

    public void OnBtnPressedCutAirwayOver()
    {
        if (CurrentStep != TrainingStep.Step3_CutAirway) return;
        cutAirway.isCutOver = true;
        insertTracheal.isInsertionOver = false;
        insertTracheal.ResetStep4();
    }

    public void OnBtnPressedTrachealReplace()
    {
        insertTracheal.isTrachealInserted = false;
        //insertTracheal.ReplaceTrachealPosition();
        Tracheal.transform.position = TrachealTubeInitPos;
    }

    public void OnBtnPressedInsertionOver()
    {
        if (CurrentStep != TrainingStep.Step4_InsertTracheal) return;
        insertTracheal.isInsertionOver = true;
        trainingReportManager.isTrainingOver = true;
        // 结束计时
        isTimerActive = false;
        trainingReportManager.TrainingTime = TrainingTime;
    }

    public void OnBtnPressedResetSkillTraining()
    {
        ResetTraining();
    }

    public void ResetTraining()
    {
        CurrentStep = TrainingStep.Idle;
        ActiveMedicalInstruments(true);
        NeckSkin.SetActive(true);
        positionDetermination.ResetStep1();
        positionDetermination.isPositionDetermined = false;
        CwClearAll.ClearAll();  
        cutSkin.ResetStep2();
        cutSkin.isCutOver = false;
        cutAirway.ResetStep3();
        cutAirway.isCutOver = false;
        insertTracheal.ResetStep4();
        insertTracheal.isTrachealInserted = false;
        Skin.SetActive(true);
        //Bone.SetActive(true);
        Airway.SetActive(true);
        //Brain.SetActive(true);
        Tissue.SetActive(true);
        SkinCut.SetActive(false);
        AirwayCut.SetActive(false);
        TissueCut.SetActive(false);
        NeckSkin.SetActive(true);
        TrainingTime = 0f;
        isTimerActive = false;
        SetLogInfo("按下“开始”按键以开始练习");
        //Logs.text += "\n按下“开始”按键以开始练习";
    }

    public void OnBtnPressedTransparentMode()
    {
        if (TransparentMode)
        {
            TransparentMode = false;
            TransparentModeBtnText.text = "透明模式：关";
            Skin.GetComponent<Renderer>().material = SkinMat;
            SkinCut.GetComponent<Renderer>().material = SkinMat;
            NeckSkin.GetComponent<Renderer>().material = SkinMat;
            Bone.GetComponent<Renderer>().material = BoneMat;
            Airway.GetComponent<Renderer>().material = AirwayMat;
            AirwayCut.GetComponent<Renderer>().material = AirwayMat;
            Brain.GetComponent<Renderer>().material = BrainMat;
            Tissue.GetComponent<Renderer>().material = TissueMat;
            TissueCut.GetComponent<Renderer>().material = TissueMat;
        }
        else
        {
            TransparentMode = true;
            TransparentModeBtnText.text = "透明模式：开";
            Skin.GetComponent<Renderer>().material = TranspSkinMat;
            SkinCut.GetComponent<Renderer>().material = TranspSkinMat;
            NeckSkin.GetComponent<Renderer>().material = TranspSkinMat;
            Bone.GetComponent<Renderer>().material = TranspBoneMat;
            Airway.GetComponent<Renderer>().material = TranspAirwayMat;
            AirwayCut.GetComponent<Renderer>().material = TranspAirwayMat;
            Brain.GetComponent<Renderer>().material = TranspBrainMat;
            Tissue.GetComponent<Renderer>().material = TransTissueMat;
            TissueCut.GetComponent<Renderer>().material = TransTissueMat;
        }
    }

    public void OnBtnPressedViewReport()
    {
        if (CurrentStep == TrainingStep.Idle)
        {
            centralUIController.OnBtnPressedViewReport();
        }
        else
        {
            SetLogInfo("请完成练习后再查看报告");
        }
    }

    // 设置日志信息
    public void SetLogInfo(string log)
    {
        if (logQueue.Count >= maxLogCount) // 如果日志队列达到最大条数
        {
            logQueue.Dequeue(); // 移除最旧的日志
        }
        logQueue.Enqueue(log); // 添加新的日志

        //Debug.Log("SpatialAnchorManager" + log); // 输出到控制台

        Logs.text = string.Join("\n", logQueue.ToArray()); // 更新提示文本显示日志
    }

    public void Add_Material(GameObject obj, Material mat)
    {
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Add(mat);
        meshRenderer.materials = materialList.ToArray();
    }

    public void Remove_Material(GameObject obj, Material mat)
    {
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Remove(mat);
        meshRenderer.materials = materialList.ToArray();
    }

    //public void OnBtnPressedTest()
    //{
    //    MarkerBodyVisual.GetComponent<HighLightDisplay>().Remove_Material();
    //}
}
