using NUnit.Framework;
using online.kamishiro.alterdresser.editor.pass;
using UnityEditor;
using UnityEngine;
using ADMGroup = online.kamishiro.alterdresser.AlterDresserMenuGroup;
using ADMItem = online.kamishiro.alterdresser.AlterDresserMenuItem;

public static class ADMItemTests
{
    private static readonly string GET_EXCLUSIVE_MENU_IDX_GUID = "047fcbc769cba37428b1b533fade3ca1";
    private static readonly string WILL_INSTALL_MENU_ANIMATION_GUID = "d801629a6baf05742a4c0052b5d325be";

    [Test]
    public static void GetExclusiveMenuIdxTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GET_EXCLUSIVE_MENU_IDX_GUID)));
        ADMItem item1 = root.transform.Find("Parent/Item1").GetComponent<ADMItem>();
        ADMItem item2 = root.transform.Find("Item2").GetComponent<ADMItem>();

        Assert.That(ADMInstallPass.IsRootMenuGroup(item1), Is.False);
        Assert.That(ADMInstallPass.IsRootMenuGroup(item2), Is.True);
    }
    [Test]
    public static void WillInstallMenuAnimationTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(WILL_INSTALL_MENU_ANIMATION_GUID)));
        ADMGroup parent = root.transform.Find("Parent").GetComponent<ADMGroup>();
        ADMItem item1 = root.transform.Find("Parent/Item1").GetComponent<ADMItem>();
        ADMItem item2 = root.transform.Find("Item2").GetComponent<ADMItem>();

        Assert.That(ADMInstallPass.WillInstallMenuAnimation(parent), Is.True);
        Assert.That(ADMInstallPass.WillInstallMenuAnimation(item1), Is.False);
        Assert.That(ADMInstallPass.WillInstallMenuAnimation(item2), Is.True);
    }
}
