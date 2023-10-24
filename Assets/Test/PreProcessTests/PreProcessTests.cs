using NUnit.Framework;
using online.kamishiro.alterdresser.editor.pass;

public static class PreProcessTests
{
    [Test]
    public static void InitializeObjectTest()
    {
        Assert.That(PreProcessPass.InitializeObject, Is.Not.Null);
    }
}