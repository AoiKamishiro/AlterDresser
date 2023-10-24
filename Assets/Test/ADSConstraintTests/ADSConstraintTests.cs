using NUnit.Framework;
using online.kamishiro.alterdresser.editor.pass;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using ADSConstraint = online.kamishiro.alterdresser.AlterDresserSwitchConstraint;

public static class ADSConstraintTests
{
    private static readonly string ADD_CONSTRAINT_GUID = "9e720d7e4ac828444a2c6deee3252d22";

    [Test]
    public static void FixedToWorldTest()
    {
        Assert.That(ADSConstraintPass.FixedToWorld, Is.Not.Null);
    }
    [Test]
    public static void AddConstraintTest()
    {
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ADD_CONSTRAINT_GUID)));
        ADSConstraint adsc = root.transform.Find("Constraint").GetComponent<ADSConstraint>();
        Transform mirror = root.transform.Find("Mirror");
        ParentConstraint constraint = ADSConstraintPass.AddConstraint(adsc, mirror);

        Assert.That(root, Is.Not.Null);
        Assert.That(adsc, Is.Not.Null);
        Assert.That(mirror, Is.Not.Null);
        Assert.That(constraint, Is.Not.Null);
    }
}