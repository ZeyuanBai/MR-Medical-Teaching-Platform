using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HighLightDisplay : MonoBehaviour
{
    public Material highlightMaterial;
    public bool isGrabbed = false;
    public bool isHighlighted = false;

    //public SkillTrainingManager skillTrainingManager;

    void Start()
    {

    }

    void Update()
    {
        WhenGrabbedRemoveMaterial();
    }

    public void SetGrabbed()
    {
        isGrabbed = true;
    }

    public void SetUnGrabbed()
    {
        isGrabbed = false;
    }

    public void WhenGrabbedRemoveMaterial()
    {
        if (isGrabbed && isHighlighted)
        {
            Remove_Material();
            isGrabbed = false;
        }
    }

    public void Add_Material()
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Add(highlightMaterial);
        meshRenderer.materials = materialList.ToArray();
        isHighlighted = true;
    }

    public void Add_Material(Material mat)
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Add(mat);
        meshRenderer.materials = materialList.ToArray();
        isHighlighted = true;   
    }

    public bool Remove_Material()
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.sharedMaterials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Remove(highlightMaterial);
        meshRenderer.sharedMaterials = materialList.ToArray();
        isHighlighted = false;
        return true;
    }

    public void Remove_Material(Material mat)
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        List<Material> materialList = new List<Material>(materials);
        materialList.Remove(mat);
        meshRenderer.materials = materialList.ToArray();
        isHighlighted = false;
    }

}
