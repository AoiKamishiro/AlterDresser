using NUnit.Framework;
using online.kamishiro.alterdresser.editor.pass;
using UnityEditor;
using UnityEngine;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;

public static class ADMInstallTests
{
    private static readonly string IS_ROOT_MENU_GROUP_GUID = "cd34de2c55c19c640a772731594da0a4";
    private static readonly string WILL_INSTALL_MENU_ANIMATION_GUID = "e260b4ac689de2649b5fda65ccb13862";

    [Test]
    public static void IsRootMenuGroupTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(IS_ROOT_MENU_GROUP_GUID)));
        ADMGroup parnet = root.transform.Find("Parent").GetComponent<ADMGroup>();
        ADMGroup child = root.transform.Find("Parent/Child").GetComponent<ADMGroup>();

        Assert.That(ADMInstallPass.IsRootMenuGroup(parnet), Is.True);
        Assert.That(ADMInstallPass.IsRootMenuGroup(child), Is.False);
    }
    [Test]
    public static void WillInstallMenuAnimationTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(WILL_INSTALL_MENU_ANIMATION_GUID)));
        ADMGroup parnet = root.transform.Find("Parent").GetComponent<ADMGroup>();
        ADMGroup child = root.transform.Find("Parent/Child").GetComponent<ADMGroup>();
        ADMItem item1 = root.transform.Find("Parent/Child/Item1").GetComponent<ADMItem>();
        ADMItem item2 = root.transform.Find("Item2").GetComponent<ADMItem>();

        Assert.That(ADMInstallPass.WillInstallMenuAnimation(parnet), Is.True);
        Assert.That(ADMInstallPass.WillInstallMenuAnimation(child), Is.False);
        Assert.That(ADMInstallPass.WillInstallMenuAnimation(item1), Is.False);
        Assert.That(ADMInstallPass.WillInstallMenuAnimation(item2), Is.True);
    }
}