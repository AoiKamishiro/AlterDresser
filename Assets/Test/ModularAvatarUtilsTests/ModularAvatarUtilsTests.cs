using nadena.dev.modular_avatar.core;
using NUnit.Framework;
using online.kamishiro.alterdresser.editor;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public static class ModularAvatarUtilsTests
{
    [Test]
    public static void MAMergeAnimatorTest()
    {
        GameObject gameObject = new GameObject();
        RuntimeAnimatorController animator = new AnimatorController();
        gameObject.transform.AddMAMergeAnimator(animator, true, VRCAvatarDescriptor.AnimLayerType.Base, true, MergeAnimatorPathMode.Absolute);

        ModularAvatarMergeAnimator item = gameObject.GetComponent<ModularAvatarMergeAnimator>();
        Assert.That(item, Is.Not.Null);
        Assert.That(item.animator, Is.EqualTo(animator));
        Assert.That(item.deleteAttachedAnimator, Is.EqualTo(true));
        Assert.That(item.layerType, Is.EqualTo(VRCAvatarDescriptor.AnimLayerType.Base));
        Assert.That(item.matchAvatarWriteDefaults, Is.EqualTo(true));
        Assert.That(item.pathMode, Is.EqualTo(MergeAnimatorPathMode.Absolute));
    }
    [Test]
    public static void MAMenuItemTest()
    {
        GameObject gameObject = new GameObject();
        VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control();
        gameObject.transform.AddMAMenuItem(control);

        ModularAvatarMenuItem item = gameObject.GetComponent<ModularAvatarMenuItem>();
        Assert.That(item, Is.Not.Null);
        Assert.That(item.Control, Is.EqualTo(control));
    }
    [Test]
    public static void MAParametersTest()
    {
        GameObject gameObject = new GameObject();
        List<ParameterConfig> parameters = new List<ParameterConfig>();
        gameObject.transform.AddMAParameters(parameters);

        ModularAvatarParameters item = gameObject.GetComponent<ModularAvatarParameters>();
        Assert.That(item, Is.Not.Null);
        Assert.That(item.parameters, Is.EqualTo(parameters));
    }
    [Test]
    public static void MAMenuInstallerTest()
    {
        GameObject gameObject = new GameObject();
        gameObject.transform.AddMaMenuInstaller();

        ModularAvatarMenuInstaller item = gameObject.GetComponent<ModularAvatarMenuInstaller>();
        Assert.That(item, Is.Not.Null);
    }
    [Test]
    public static void MaMeshSettingsTest()
    {
        GameObject gameObject = new GameObject();
        gameObject.transform.AddMaMeshSettings();

        ModularAvatarMeshSettings item = gameObject.GetComponent<ModularAvatarMeshSettings>();
        Assert.That(item, Is.Not.Null);
    }
    [Test]
    public static void MaBlendshapeSyncTest()
    {
        GameObject gameObject = new GameObject();
        BlendshapeBinding binding = new BlendshapeBinding();
        gameObject.transform.AddMaBlendshapeSync(new List<BlendshapeBinding>() { binding });

        ModularAvatarBlendshapeSync item = gameObject.GetComponent<ModularAvatarBlendshapeSync>();
        Assert.That(item, Is.Not.Null);
    }
}