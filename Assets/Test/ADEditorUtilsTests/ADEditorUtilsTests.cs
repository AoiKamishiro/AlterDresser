using NUnit.Framework;
using online.kamishiro.alterdresser;
using online.kamishiro.alterdresser.editor;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;

public static class ADEditorUtilsTests
{
    private static readonly string IS_EDITOR_ONLY_GUID = "d705ac40daaccf546811b10880435e0a";
    private static readonly string GET_ID_GUID = "81ec29cfcab9b184ea4ce5363aa661a2";
    private static readonly string WILL_USE_GUID = "bbe4b61cc49ece84399ff8553b348fad";
    private static readonly string GET_IDX_GUID = "e8f715dae656e9748b187536d33f9f92";
    private static readonly string IS_ROOT_GUID = "3608f75ed56f7564c8cec3d39bbf63e3";
    private static readonly string GET_AVATAR_GUID = "52324f315ad9d6542bf9454306a65b71";
    private static readonly string GET_RELATIVE_PATH_GUID = "9f865bf03fe65704b9979875495e740c";
    private static readonly string GET_VALID_CHILD_RENDERER_GUID = "b53af6b4675d8ca44bc51c4671e270fe";
    private static readonly string CONVERT_TO_SKINNEDMESHRENDERER_GUID = "76a91df3d574a204e997756aa74859b0";
    private static readonly string ADD_ROOTBONE_TO_SKINNEDMESHRENDERER_GUID = "f47fffa7c5923a3489011a4443f9d1f1";
    private static readonly string GET_WILL_MERGE_MATERIALS_GUID = "5bd51166d3d046049976b0dd5c7d5059";

    [Test]
    public static void IsEditorOnlyTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(IS_EDITOR_ONLY_GUID)));
        Transform transform = root.transform.Find("Parent/Child");

        bool actual = ADEditorUtils.IsEditorOnly(transform);
        Assert.That(actual, Is.True);
    }
    [Test]
    public static void GetIDTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_ID_GUID)));
        ADMGroup menuGroup = root.transform.GetComponentInChildren<ADMGroup>(true);
        ADMItem menuItem = root.transform.GetComponentInChildren<ADMItem>(true);

        Assert.That(menuGroup.Id, Is.EqualTo(ADEditorUtils.GetID(menuItem)));
    }
    [Test]
    public static void WillUseTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(WILL_USE_GUID)));
        foreach (Transform item in root.transform.GetChild(0))
        {
            if (item.GetSiblingIndex() < 8) Assert.That(ADEditorUtils.WillUse(item.GetComponent<ADMItem>()), Is.True);
            else Assert.That(ADEditorUtils.WillUse(item.GetComponent<ADMItem>()), Is.False);
        }
    }
    [Test]
    public static void GetIdxTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_IDX_GUID)));
        Transform group1 = root.transform.Find("Group1");
        Transform group2 = root.transform.Find("Group2");
        Transform itemA = root.transform.Find("ItemA");

        foreach (Transform item in group1)
        {
            int expect = item.GetSiblingIndex();
            int actial = ADEditorUtils.GetIdx(item.GetComponent<ADMItem>());
            Assert.That(actial, Is.EqualTo(expect));
        }
        foreach (Transform item in group2)
        {
            int expect = 0;
            int actial = ADEditorUtils.GetIdx(item.GetComponent<ADMItem>());
            Assert.That(actial, Is.EqualTo(expect));
        }
        Assert.That(ADEditorUtils.GetIdx(itemA.GetComponent<ADMItem>()), Is.EqualTo(0));
    }
    [Test]
    public static void IsRootTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(IS_ROOT_GUID)));
        Transform group1 = root.transform.Find("Group1");
        Transform group2 = root.transform.Find("Group2");
        Transform itemA = root.transform.Find("ItemA");

        foreach (Transform item in group1)
        {
            bool expect = false;
            bool actial = ADEditorUtils.IsRoot(item.GetComponent<ADMItem>());
            Assert.That(actial, Is.EqualTo(expect));
        }
        foreach (Transform item in group2)
        {
            bool expect = true;
            bool actial = ADEditorUtils.IsRoot(item.GetComponent<ADMItem>());
            Assert.That(actial, Is.EqualTo(expect));
        }
        Assert.That(ADEditorUtils.IsRoot(itemA.GetComponent<ADMItem>()), Is.True);
    }
    [Test]
    public static void GetAvatarTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_AVATAR_GUID)));
        VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
        ADMItem menuItem = root.GetComponentInChildren<ADMItem>(true);
        SerializedProperty sp = new SerializedObject(menuItem).FindProperty(nameof(ADMItem.menuIcon));

        Assert.That(ADEditorUtils.GetAvatar(sp), Is.EqualTo(avatar));
    }
    [Test]
    public static void GetRelativePathTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_RELATIVE_PATH_GUID)));
        VRCAvatarDescriptor avatar = root.GetComponent<VRCAvatarDescriptor>();
        Transform expect = root.GetComponentInChildren<ADMItem>(true).transform;
        Transform actual = avatar.GetRelativeObject("Parent/Chilad");

        Assert.That(actual, Is.EqualTo(expect));
    }
    [Test]
    public static void GetValidChildRendererTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_VALID_CHILD_RENDERER_GUID)));
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("MeshRenderer").GetComponent<Renderer>()), Is.True);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("MeshRenderer_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("MeshRenderer_NoMesh").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("MeshRenderer_NoMesh_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("MeshRenderer_NoMeshFilter").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("MeshRenderer_NoMeshFilter_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer").GetComponent<Renderer>()), Is.True);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_NoMesh").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_NoMesh_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth").GetComponent<Renderer>()), Is.True);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth_NoMesh").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetValidChildRenderers(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth_NoMesh_NoMat").GetComponent<Renderer>()), Is.False);
    }
    [Test]
    public static void GetWillMergeMeshTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_VALID_CHILD_RENDERER_GUID)));
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("MeshRenderer").GetComponent<Renderer>()), Is.True);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("MeshRenderer_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("MeshRenderer_NoMesh").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("MeshRenderer_NoMesh_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("MeshRenderer_NoMeshFilter").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("MeshRenderer_NoMeshFilter_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer").GetComponent<Renderer>()), Is.True);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_NoMesh").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_NoMesh_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth_NoMat").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth_NoMesh").GetComponent<Renderer>()), Is.False);
        Assert.That(ADEditorUtils.GetWillMergeMesh(root.transform).Contains(root.transform.Find("SkinnedMeshRenderer_Cloth_NoMesh_NoMat").GetComponent<Renderer>()), Is.False);
    }
    [Test]
    public static void ConvertToSkinnedMeshrendererTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(CONVERT_TO_SKINNEDMESHRENDERER_GUID)));

        MeshRenderer meshRenderer = root.GetComponentInChildren<MeshRenderer>();
        int matLength = meshRenderer.sharedMaterials.Length;
        ADEditorUtils.ConvertToSkinnedMeshrenderer(meshRenderer);

        Assert.That(root.GetComponentInChildren<MeshRenderer>(), Is.Null);
        Assert.That(root.GetComponentInChildren<MeshFilter>(), Is.Null);
        Assert.That(root.GetComponentInChildren<SkinnedMeshRenderer>(), Is.Not.Null);
        Assert.That(root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.Length, Is.EqualTo(matLength));
    }
    [Test]
    public static void AddRootBoneToSkinnedMeshrendererTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ADD_ROOTBONE_TO_SKINNEDMESHRENDERER_GUID)));

        SkinnedMeshRenderer skinnedMeshRenderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
        Transform current = skinnedMeshRenderer.rootBone;
        ADEditorUtils.AddRootBoneToSkinnedMeshrenderer(skinnedMeshRenderer);

        Assert.That(current, Is.Null);
        Assert.That(skinnedMeshRenderer.rootBone, Is.Not.Null);
    }
    [Test]
    public static void GetWillMergeMaterialsTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_WILL_MERGE_MATERIALS_GUID)));
        AlterDresserSwitchEnhanced adse = root.GetComponentInChildren<AlterDresserSwitchEnhanced>();

        Material[] mats = ADEditorUtils.GetWillMergeMaterials(adse, adse.mergeMeshIgnoreMask);
        Assert.That(mats.Length, Is.EqualTo(1));
        Assert.That(mats[0].name, Is.EqualTo("Default-Particle"));
    }
}