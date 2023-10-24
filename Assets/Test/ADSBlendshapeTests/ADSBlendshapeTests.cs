using NUnit.Framework;
using online.kamishiro.alterdresser.editor.pass;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ADSBlendshape = online.kamishiro.alterdresser.AlterDresserSwitchBlendshape;

public static class ADSBlendshapeTests
{
    private static readonly string BLENDSHAPE_NAME = "a54450d52e6f0494595798c506f7ad86";
    private static readonly string[] BLENDSHAPE_NAME_EXPECTED = new string[] { "Key 1", "Key 2", "Key 4" };

    [Test]
    public static void BlendshapeNames()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(BLENDSHAPE_NAME)));

        VRCAvatarDescriptor avatarRoot = root.GetComponent<VRCAvatarDescriptor>();
        ADSBlendshape adsb = root.GetComponentInChildren<ADSBlendshape>();

        IEnumerable<int> idxs = ADSBlendshapePass.GetAllIBlendshapeDs(adsb, avatarRoot);
        SkinnedMeshRenderer smr = adsb.GetComponent<SkinnedMeshRenderer>();

        IEnumerable<string> actual = Enumerable.Range(0, idxs.Count()).Select(x => smr.sharedMesh.GetBlendShapeName(idxs.ElementAt(x)));

        Assert.That(actual.Count(), Is.EqualTo(BLENDSHAPE_NAME_EXPECTED.Length));

        for (int i = 0; i < BLENDSHAPE_NAME_EXPECTED.Count(); i++)
        {
            Assert.That(actual.ElementAt(i), Is.EqualTo(BLENDSHAPE_NAME_EXPECTED[i]));
        }
    }
}