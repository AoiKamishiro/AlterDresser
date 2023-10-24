using NUnit.Framework;
using online.kamishiro.alterdresser.editor;
using UnityEditor;
using UnityEngine;

public static class LilToonUtilsTests
{
    [Test]
    public static void LiltoonMultiTest()
    {
        Assert.That(LilToonUtils.LiltoonMulti, Is.Not.Null);
    }
    [Test]
    public static void ConvertTest()
    {
        Material defMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        Material converted = LilToonUtils.ConvertToLilToonMulti(defMat);
        Assert.That(converted, Is.Not.Null);
        Assert.That(converted.shader, Is.EqualTo(LilToonUtils.LiltoonMulti));
        Assert.That(converted.renderQueue, Is.EqualTo(2461));
        Assert.That(converted.GetVector("_DissolveParams"), Is.EqualTo(new Vector4(3, 1, -1, 0.01f)));
        Assert.That(converted.GetFloat("_DissolveNoiseStrength"), Is.EqualTo(0.0f));
        Assert.That(converted.IsKeywordEnabled("GEOM_TYPE_BRANCH_DETAIL"), Is.True);
        Assert.That(converted.IsKeywordEnabled("UNITY_UI_CLIP_RECT"), Is.True);
    }
}