using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Button = UnityEngine.UI.Button;
using System.Runtime.CompilerServices;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using openDicom.Encoding;

public class CaseOfNeckMassAnatomyManager : MonoBehaviour
{
    [Header("Models and Components")]
    public GameObject AnatomyTeachingModelTable;
    public AnatomyTeachingManager anatomyTeachingManager;
    public GameObject ReconstructionModel;
    public GameObject VolumeRenderingModel;
    public GameObject TransversePlane;
    public GeneralGrabTransformer TransverseGrabTransformer;
    public GameObject TransverseSlicingPlane;
    public MeshRenderer TransverseSlicingPlaneMeshRenderer;
    public GameObject TransverseCrossSectionPlane;
    public MeshRenderer TransverseCrossSectionPlaneMeshRenderer;
    public GameObject SagittalPlane;
    public GeneralGrabTransformer SagittalGrabTransformer;
    public GameObject SagittalSlicingPlane;
    public MeshRenderer SagittalSlicingPlaneMeshRenderer;
    public GameObject SagittalCrossSectionPlane;
    public MeshRenderer SagittalCrossSectionPlaneMeshRenderer;
    public GameObject CoronalPlane;
    public GeneralGrabTransformer CoronalGrabTransformer;
    public GameObject CoronalSlicingPlane;
    public MeshRenderer CoronalSlicingPlaneMeshRenderer;
    public GameObject CoronalCrossSectionPlane;
    public MeshRenderer CoronalCrossSectionPlaneMeshRenderer;
    public GameObject AnatomyImageDisplayUI;
    public GameObject CaseOfNeckMassCTImageDisplayPanel;
    public GameObject SkinModel;
    public GameObject BoneModel;
    public GameObject AirwayModel;
    public GameObject LungModel;
    public GameObject VesselAndHeartModel;
    public GameObject MassModel;

    [Header("UIelements")]
    public TMP_Text Logs;
    public Slider RotateSlider;
    public Button ReconstructionModelBtn;
    public TMP_Text ReconstructionModelText;
    public Button TransparentModeBtn;
    public TMP_Text TransparentModeText;
    public Button SkinBtn;
    public TMP_Text SkinText;
    public Button BoneBtn;
    public TMP_Text BoneText;
    public Button AirwayBtn;
    public TMP_Text AirwayText;
    public Button LungBtn;
    public TMP_Text LungText;
    public Button VesselAndHeartBtn;
    public TMP_Text VesselAndHeartText;
    public Button MassBtn;
    public TMP_Text MassText;

    public Button VolumeRenderingModelBtn;
    public TMP_Text VolumeRenderingModelText;
    public Button OriginDataDisplayBtn;
    public TMP_Text OriginDataDisplayText;
    public Button LockPlaneRotationBtn;
    public TMP_Text LockPlaneRotationText;
    public Button TransversePlaneBtn;
    public TMP_Text TransversePlaneText;
    public Button SagittalPlaneBtn;
    public TMP_Text SagittalPlaneText;
    public Button CoronalPlaneBtn;
    public TMP_Text CoronalPlaneText;
    public Slider TransversePlaneSlider;
    public Slider SagittalPlaneSlider;
    public Slider CoronalPlaneSlider;

    [Header("Materials")]
    public Material SkinMat;
    public Material BoneMat;
    public Material AirwayMat;
    public Material LungMat;
    public Material VesselAndHeartMat;
    public Material MassMat;
    public Material TranspSkinMat;
    public Material TranspBoneMat;
    public Material TranspAirwayMat;
    public Material TranspLungMat;
    public Material TranspVesselAndHeartMat;
    public Material TranspMassMat;

    [Header("Anatomy Teaching Arguments")]
    [HideInInspector]
    public bool ReconstructionModelActive = false;
    [HideInInspector]
    public bool VolumeRenderingModelActive = false;
    [HideInInspector]
    public bool isOriginDataDisplay = false;
    [HideInInspector]
    public bool isLockPlaneRotation = false;

    public Vector3 initialPosition = new Vector3(0, 0, 0);
    public float moveRange = 1.0f;

    [HideInInspector]
    public bool TransverseGrabbed = false;
    [HideInInspector]
    public bool SagittalGrabbed = false;
    [HideInInspector]
    public bool CoronalGrabbed = false;

    [HideInInspector]
    public bool TransparentMode = false;

    [HideInInspector]
    //public float RotationY;

    void Start()
    {
        RotateSlider.onValueChanged.AddListener(OnSliderMovedRotateSlider);

        ReconstructionModelBtn.onClick.AddListener(OnBtnPressedReconstructionModel);
        TransparentModeBtn.onClick.AddListener(OnBtnPressedTransparentMode);
        SkinBtn.onClick.AddListener(OnBtnPressedSkin);
        BoneBtn.onClick.AddListener(OnBtnPressedBone);
        AirwayBtn.onClick.AddListener(OnBtnPressedAirway);
        LungBtn.onClick.AddListener(OnBtnPressedLung);
        VesselAndHeartBtn.onClick.AddListener(OnBtnPressedVesselAndHeart);
        MassBtn.onClick.AddListener(OnBtnPressedMass);

        VolumeRenderingModelBtn.onClick.AddListener(OnBtnPressedVolumeRenderingModel);
        OriginDataDisplayBtn.onClick.AddListener(OnBtnPressedOriginDataDisplay);
        LockPlaneRotationBtn.onClick.AddListener(OnBtnPressedLockPlaneRotation);
        TransversePlaneBtn.onClick.AddListener(OnBtnPressedTransversePlane);
        SagittalPlaneBtn.onClick.AddListener(OnBtnPressedSagittalPlane);
        CoronalPlaneBtn.onClick.AddListener(OnBtnPressedCoronalPlane);

        TransversePlaneSlider.onValueChanged.AddListener(OnSliderMovedTransversePlane);
        SagittalPlaneSlider.onValueChanged.AddListener(OnSliderMovedSagittalPlane);
        CoronalPlaneSlider.onValueChanged.AddListener(OnSliderMovedCoronalPlane);

        TransversePlaneSlider.value = 0;
        SagittalPlaneSlider.value = 0;
        CoronalPlaneSlider.value = 0;
        RotateSlider.maxValue = 180;
        RotateSlider.minValue = -180;
        RotateSlider.value = 0;

        InitializeText(true, true);

        ResetPlaneStatus();
    }


    void Update()
    {
        LockPlane();
    }

    public void InitializeText(bool RCM, bool VRM)
    {
        if (RCM)
        {
            ReconstructionModelText.text = "显示重建模型";
            SkinText.text = "隐藏皮肤";
            BoneText.text = "隐藏骨骼";
            AirwayText.text = "隐藏气道";
            LungText.text = "隐藏肺部";
            VesselAndHeartText.text = "隐藏血管和心脏";
            MassText.text = "隐藏肿块";
        }
        if (VRM)
        {
            VolumeRenderingModelText.text = "显示体绘制模型";
            TransversePlaneText.text = "显示横断面";
            SagittalPlaneText.text = "显示矢状面";
            CoronalPlaneText.text = "显示冠状面";
            OriginDataDisplayText.text = "显示原始数据";
            LockPlaneRotationText.text = "锁定解剖面旋转";
        }
    }

    // 重建模型激活
    public void OnBtnPressedReconstructionModel()
    {
        if (!ReconstructionModelActive)
        {
            AnatomyTeachingModelTable.SetActive(true);
            ReconstructionModel.SetActive(true);
            SkinModel.SetActive(true);
            BoneModel.SetActive(true);
            AirwayModel.SetActive(true);
            LungModel.SetActive(true);
            VesselAndHeartModel.SetActive(true);
            MassModel.SetActive(true);
            ReconstructionModelActive = true;
            ReconstructionModelText.text = "隐藏重建模型";
        }
        else
        {
            ReconstructionModelReset();
            ReconstructionModelText.text = "显示重建模型";
        }
    }

    // 重建模型隐藏并重置
    public void ReconstructionModelReset()
    {
        if (ReconstructionModelActive)
        {
            SkinModel.SetActive(true);
            BoneModel.SetActive(true);
            AirwayModel.SetActive(true);
            LungModel.SetActive(true);
            VesselAndHeartModel.SetActive(true);
            MassModel.SetActive(true);
            ReconstructionModel.SetActive(false);
            if (!VolumeRenderingModelActive)
            {
                AnatomyTeachingModelTable.SetActive(false);
            }
            ReconstructionModelActive = false;
            InitializeText(true, false);
            ReconstructionModelText.text = "显示重建模型";
        }
    }

    // 透明模式激活状态切换
    public void OnBtnPressedTransparentMode()
    {
        if (TransparentMode)
        {
            TransparentMode = false;
            TransparentModeText.text = "透明模式：关";
            SkinModel.GetComponent<Renderer>().material = SkinMat;
            BoneModel.GetComponent<Renderer>().material = BoneMat;
            AirwayModel.GetComponent<Renderer>().material = AirwayMat;
            LungModel.GetComponent<Renderer>().material = LungMat;
            VesselAndHeartModel.GetComponent<Renderer>().material = VesselAndHeartMat;
            MassModel.GetComponent<Renderer>().material = MassMat;
        }
        else
        {
            TransparentMode = true;
            TransparentModeText.text = "透明模式：开";
            SkinModel.GetComponent<Renderer>().material = TranspSkinMat;
            BoneModel.GetComponent<Renderer>().material = TranspBoneMat;
            AirwayModel.GetComponent<Renderer>().material = TranspAirwayMat;
            LungModel.GetComponent<Renderer>().material = TranspLungMat;
            VesselAndHeartModel.GetComponent<Renderer>().material = TranspVesselAndHeartMat;
            MassModel.GetComponent<Renderer>().material = TranspMassMat;
        }
    }

    // 皮肤模型激活状态切换
    public void OnBtnPressedSkin()
    {
        if (ReconstructionModelActive)
        {
            if (SkinModel.activeSelf)
            {
                SkinModel.SetActive(false);
                SkinText.text = "显示皮肤";
            }
            else
            {
                SkinModel.SetActive(true);
                SkinText.text = "隐藏皮肤";
            }
        }
    }

    // 骨骼模型激活状态切换
    public void OnBtnPressedBone()
    {
        if (ReconstructionModelActive)
        {
            if (BoneModel.activeSelf)
            {
                BoneModel.SetActive(false);
                BoneText.text = "显示骨骼";
            }
            else
            {
                BoneModel.SetActive(true);
                BoneText.text = "隐藏骨骼";
            }
        }
    }

    // 气道模型激活状态切换
    public void OnBtnPressedAirway()
    {
        if (ReconstructionModelActive)
        {
            if (AirwayModel.activeSelf)
            {
                AirwayModel.SetActive(false);
                AirwayText.text = "显示气道";
            }
            else
            {
                AirwayModel.SetActive(true);
                AirwayText.text = "隐藏气道";
            }
        }
    }

    // 大脑模型激活状态切换
    public void OnBtnPressedLung()
    {
        if (ReconstructionModelActive)
        {
            if (LungModel.activeSelf)
            {
                LungModel.SetActive(false);
                LungText.text = "显示肺部";
            }
            else
            {
                LungModel.SetActive(true);
                LungText.text = "隐藏肺部";
            }
        }
    }

    public void OnBtnPressedVesselAndHeart()
    {
        if (ReconstructionModelActive)
        {
            if (VesselAndHeartModel.activeSelf)
            {
                VesselAndHeartModel.SetActive(false);
                VesselAndHeartText.text = "显示血管和心脏";
            }
            else
            {
                VesselAndHeartModel.SetActive(true);
                VesselAndHeartText.text = "隐藏血管和心脏";
            }
        }
    }

    public void OnBtnPressedMass()
    {
        if (ReconstructionModelActive)
        {
            if (MassModel.activeSelf)
            {
                MassModel.SetActive(false);
                MassText.text = "显示肿块";
            }
            else
            {
                MassModel.SetActive(true);
                MassText.text = "隐藏肿块";
            }
        }    
    }

    public void OnSliderMovedRotateSlider(float value)
    {
        AnatomyTeachingModelTable.transform.rotation = Quaternion.Euler(AnatomyTeachingModelTable.transform.eulerAngles.x, anatomyTeachingManager.RotationY + value, AnatomyTeachingModelTable.transform.eulerAngles.z);
    }

    // 体绘制模型激活
    public void OnBtnPressedVolumeRenderingModel()
    {
        if (VolumeRenderingModelActive)
        {
            TransversePlane.transform.localPosition = initialPosition;
            //TransversePlane.transform.localRotation = Quaternion.Euler(-90, 0, 180);
            TransversePlane.transform.localRotation = Quaternion.Euler(0, 0, 0);
            TransversePlane.SetActive(false);
            SagittalPlane.transform.localPosition = initialPosition;
            SagittalPlane.transform.localRotation = Quaternion.Euler(0, -90, 0);
            SagittalPlane.SetActive(false);
            CoronalPlane.transform.localPosition = initialPosition;
            CoronalPlane.transform.localRotation = Quaternion.Euler(180, 180, 0);
            CoronalPlane.SetActive(false);
            VolumeRenderingModel.SetActive(false);
            if (!ReconstructionModelActive)
            {
                AnatomyTeachingModelTable.SetActive(false);
            }
            VolumeRenderingModelActive = false;
            InitializeText(false, true);
            VolumeRenderingModelText.text = "显示体绘制模型";
        }
        else
        {
            AnatomyTeachingModelTable.SetActive(true);
            VolumeRenderingModel.SetActive(true);
            VolumeRenderingModelActive = true;
            VolumeRenderingModelText.text = "隐藏体绘制模型";
        }
    }

    // 体绘制模型隐藏并重置
    public void VolumeRenderingModelReset()
    {
        if (VolumeRenderingModelActive)
        {
            TransversePlane.transform.localPosition = initialPosition;
            //TransversePlane.transform.localRotation = Quaternion.Euler(-90, 0, 180);
            TransversePlane.transform.localRotation = Quaternion.Euler(0, 0, 0);
            TransversePlane.SetActive(false);
            SagittalPlane.transform.localPosition = initialPosition;
            SagittalPlane.transform.localRotation = Quaternion.Euler(0, -90, 0);
            SagittalPlane.SetActive(false);
            CoronalPlane.transform.localPosition = initialPosition;
            CoronalPlane.transform.localRotation = Quaternion.Euler(180, 180, 0);
            CoronalPlane.SetActive(false);
            VolumeRenderingModel.SetActive(false);
            if (!ReconstructionModelActive)
            {
                AnatomyTeachingModelTable.SetActive(false);
            }
            VolumeRenderingModelActive = false;
            InitializeText(false, true);
            ResetPlaneStatus();
        }
    }

    public void OnBtnPressedOriginDataDisplay()
    {
        if (isOriginDataDisplay)
        {
            TransverseSlicingPlaneMeshRenderer.enabled = false;
            TransverseCrossSectionPlaneMeshRenderer.enabled = true;
            SagittalSlicingPlaneMeshRenderer.enabled = false;
            SagittalCrossSectionPlaneMeshRenderer.enabled = true;
            CoronalSlicingPlaneMeshRenderer.enabled = false;
            CoronalCrossSectionPlaneMeshRenderer.enabled = true;
            OriginDataDisplayText.text = "显示原始数据";
            AnatomyImageDisplayUI.SetActive(false);
            CaseOfNeckMassCTImageDisplayPanel.SetActive(false);
            isOriginDataDisplay = false;
        }
        else
        {
            TransverseSlicingPlaneMeshRenderer.enabled = true;
            TransverseCrossSectionPlaneMeshRenderer.enabled = false;
            SagittalSlicingPlaneMeshRenderer.enabled = true;
            SagittalCrossSectionPlaneMeshRenderer.enabled = false;
            CoronalSlicingPlaneMeshRenderer.enabled = true;
            CoronalCrossSectionPlaneMeshRenderer.enabled = false;
            OriginDataDisplayText.text = "隐藏原始数据";
            AnatomyImageDisplayUI.SetActive(true);
            CaseOfNeckMassCTImageDisplayPanel.SetActive(true);
            isOriginDataDisplay = true;
        }
    }

    public void OnBtnPressedLockPlaneRotation()
    {
        if (isLockPlaneRotation)
        {
            TransverseGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.All;
            SagittalGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.All;
            CoronalGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.All;
            TransverseGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.All;
            SagittalGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.All;
            CoronalGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.All;
            isLockPlaneRotation = false;
            LockPlaneRotationText.text = "锁定解剖面旋转";
        }
        else
        {
            TransversePlane.transform.localPosition = new Vector3(0, TransversePlaneSlider.value / 1.8f, 0);
            TransversePlane.transform.localRotation = Quaternion.Euler(0, 0, 0);
            TransverseGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.None;
            TransverseGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.Y;
            SagittalPlane.transform.localPosition = new Vector3(SagittalPlaneSlider.value / 1.8f, 0, 0);
            SagittalPlane.transform.localRotation = Quaternion.Euler(0, -90, 0);
            SagittalGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.None;
            SagittalGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.Z;
            CoronalPlane.transform.localPosition = new Vector3(0, 0, -CoronalPlaneSlider.value / 1.8f);
            CoronalPlane.transform.localRotation = Quaternion.Euler(180, 180, 0);
            CoronalGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.None;
            CoronalGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.Z;
            isLockPlaneRotation = true;
            LockPlaneRotationText.text = "解锁解剖面旋转";
        }
    }

    // 横断面激活状态切换
    public void OnBtnPressedTransversePlane()
    {
        if (VolumeRenderingModelActive)
        {
            if (TransversePlane.activeSelf)
            {
                TransversePlane.SetActive(false);
                TransverseSlicingPlane.SetActive(false);
                TransverseCrossSectionPlane.SetActive(false);
                TransverseCrossSectionPlaneMeshRenderer.enabled = false;
                TransverseSlicingPlaneMeshRenderer.enabled = false;
                TransversePlaneText.text = "显示横断面";
            }
            else
            {
                TransversePlane.SetActive(true);
                TransverseSlicingPlane.SetActive(true);
                TransverseCrossSectionPlane.SetActive(true);
                if (isOriginDataDisplay)
                {
                    TransverseSlicingPlaneMeshRenderer.enabled = true;
                    TransverseCrossSectionPlaneMeshRenderer.enabled = false;
                }
                else
                {
                    TransverseSlicingPlaneMeshRenderer.enabled = false;
                    TransverseCrossSectionPlaneMeshRenderer.enabled = true;
                }
                TransversePlaneText.text = "隐藏横断面";
            }
            if (TransverseGrabbed)
            {
                TransverseGrabbed = false;
            }
        }
    }

    // 矢状面激活状态切换
    public void OnBtnPressedSagittalPlane()
    {
        if (VolumeRenderingModelActive)
        {
            if (SagittalPlane.activeSelf)
            {
                SagittalPlane.SetActive(false);
                SagittalSlicingPlane.SetActive(false);
                SagittalCrossSectionPlane.SetActive(false);
                SagittalCrossSectionPlaneMeshRenderer.enabled = false;
                SagittalSlicingPlaneMeshRenderer.enabled = false;
                SagittalPlaneText.text = "显示矢状面";
            }
            else
            {
                SagittalPlane.SetActive(true);
                SagittalSlicingPlane.SetActive(true);
                SagittalCrossSectionPlane.SetActive(true);
                if (isOriginDataDisplay)
                {
                    SagittalSlicingPlaneMeshRenderer.enabled = true;
                    SagittalCrossSectionPlaneMeshRenderer.enabled = false;
                }
                else
                {
                    SagittalCrossSectionPlaneMeshRenderer.enabled = true;
                    SagittalSlicingPlaneMeshRenderer.enabled = false;
                }
                SagittalPlaneText.text = "隐藏矢状面";
            }
            if (SagittalGrabbed)
            {
                SagittalGrabbed = false;
            }
        }
    }

    // 冠状面激活状态切换
    public void OnBtnPressedCoronalPlane()
    {
        if (VolumeRenderingModelActive)
        {
            if (CoronalPlane.activeSelf)
            {
                CoronalPlane.SetActive(false);
                CoronalSlicingPlane.SetActive(false);
                CoronalCrossSectionPlane.SetActive(false);
                CoronalCrossSectionPlaneMeshRenderer.enabled = false;
                CoronalSlicingPlaneMeshRenderer.enabled = false;
                CoronalPlaneText.text = "显示冠状面";
            }
            else
            {
                CoronalPlane.SetActive(true);
                CoronalSlicingPlane.SetActive(true);
                CoronalCrossSectionPlane.SetActive(true);
                if (isOriginDataDisplay)
                {
                    CoronalSlicingPlaneMeshRenderer.enabled = true;
                    CoronalCrossSectionPlaneMeshRenderer.enabled = false;
                }
                else
                {
                    CoronalCrossSectionPlaneMeshRenderer.enabled = true;
                    CoronalSlicingPlaneMeshRenderer.enabled = false;
                }
                CoronalPlaneText.text = "隐藏冠状面";
            }
            if (CoronalGrabbed)
            {
                CoronalGrabbed = false;
            }
        }
    }

    // 横断面移动
    public void OnSliderMovedTransversePlane(float value)
    {
        TransversePlane.transform.localPosition = new Vector3(0, value / 1.8f, 0);
        //TransversePlane.transform.localRotation = Quaternion.Euler(-90, 0, 180);
        TransversePlane.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    // 矢状面移动
    public void OnSliderMovedSagittalPlane(float value)
    {
        SagittalPlane.transform.localPosition = new Vector3(value / 1.8f, 0, 0);
        SagittalPlane.transform.localRotation = Quaternion.Euler(0, -90, 0);
    }

    // 冠状面移动
    public void OnSliderMovedCoronalPlane(float value)
    {
        CoronalPlane.transform.localPosition = new Vector3(0, 0, -value / 1.8f);
        CoronalPlane.transform.localRotation = Quaternion.Euler(180, 180, 0);
    }

    public void LockPlane()
    {
        if (!TransverseGrabbed)
        {
            TransversePlane.transform.localPosition = new Vector3(0, TransversePlaneSlider.value / 1.8f, 0);
            //TransversePlane.transform.localRotation = Quaternion.Euler(-90, 0, 180);
            TransversePlane.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        if (!SagittalGrabbed)
        {
            SagittalPlane.transform.localPosition = new Vector3(SagittalPlaneSlider.value / 1.8f, 0, 0);
            SagittalPlane.transform.localRotation = Quaternion.Euler(0, -90, 0);
        }
        if (!CoronalGrabbed)
        {
            CoronalPlane.transform.localPosition = new Vector3(0, 0, -CoronalPlaneSlider.value / 1.8f);
            CoronalPlane.transform.localRotation = Quaternion.Euler(180, 180, 0);
        }
    }

    public void OnTransversePlaneSelectEntered()
    {
        TransverseGrabbed = true;
    }

    public void OnTransversePlaneSelectExited()
    {
        TransverseGrabbed = false;
    }

    public void OnSagittalPlaneSelectEntered()
    {
        SagittalGrabbed = true;
    }

    public void OnSagittalPlaneSelectExited()
    {
        SagittalGrabbed = false;
    }

    public void OnCoronalPlaneSelectEntered()
    {
        CoronalGrabbed = true;
    }

    public void OnCoronalPlaneSelectExited()
    {
        CoronalGrabbed = false;
    }

    public void ResetAllModels()
    {
        ReconstructionModelReset();
        VolumeRenderingModelReset();
    }

    public void ResetPlaneStatus()
    {
        TransverseGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.All;
        SagittalGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.All;
        CoronalGrabTransformer.permittedRotationAxis = GeneralGrabTransformer.ManipulationAxes.All;
        TransverseGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.All;
        SagittalGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.All;
        CoronalGrabTransformer.customLocalTranslationAxes = GeneralGrabTransformer.ManipulationAxes.All;
        isLockPlaneRotation = false;
        TransverseSlicingPlaneMeshRenderer.enabled = false;
        TransverseCrossSectionPlaneMeshRenderer.enabled = false;
        SagittalSlicingPlaneMeshRenderer.enabled = false;
        SagittalCrossSectionPlaneMeshRenderer.enabled = false;
        CoronalSlicingPlaneMeshRenderer.enabled = false;
        CoronalCrossSectionPlaneMeshRenderer.enabled = false;
        isOriginDataDisplay = false;
        TransversePlane.SetActive(false);
        TransverseSlicingPlane.SetActive(false);
        TransverseCrossSectionPlane.SetActive(false);
        SagittalPlane.SetActive(false);
        SagittalSlicingPlane.SetActive(false);
        SagittalCrossSectionPlane.SetActive(false);
        CoronalPlane.SetActive(false);
        CoronalSlicingPlane.SetActive(false);
        CoronalCrossSectionPlane.SetActive(false);
        AnatomyImageDisplayUI.SetActive(false);
        CaseOfNeckMassCTImageDisplayPanel.SetActive(false);
    }
}
