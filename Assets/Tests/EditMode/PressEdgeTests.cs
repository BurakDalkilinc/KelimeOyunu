using NUnit.Framework;

/// <summary>PressEdge testleri: tuş yalnızca basıldığı karede "yeni basıldı" sayılır.</summary>
public class PressEdgeTests
{
    [Test]
    public void FirstDownFrame_IsJustPressed()
    {
        var edge = new PressEdge();
        Assert.IsFalse(edge.Update(false));
        Assert.IsTrue(edge.Update(true));
    }

    [Test]
    public void HeldKey_DoesNotRetrigger()
    {
        var edge = new PressEdge();
        Assert.IsTrue(edge.Update(true));
        for (int i = 0; i < 100; i++)
            Assert.IsFalse(edge.Update(true));
    }

    [Test]
    public void ReleaseThenPressAgain_TriggersAgain()
    {
        var edge = new PressEdge();
        Assert.IsTrue(edge.Update(true));
        Assert.IsFalse(edge.Update(false));
        Assert.IsTrue(edge.Update(true));
    }

    [Test]
    public void NeverPressed_NeverTriggers()
    {
        var edge = new PressEdge();
        for (int i = 0; i < 50; i++)
            Assert.IsFalse(edge.Update(false));
    }

    [Test]
    public void KeyAlreadyHeldAtFirstFrame_CountsAsJustPressed()
    {
        // Sahne başlarken Enter zaten basılıysa ilk karede bir kez tetiklenir.
        // TypingBoxDisplay her karede güncellediği için, kutu açıldığında bu durum oluşmaz.
        var edge = new PressEdge();
        Assert.IsTrue(edge.Update(true));
        Assert.IsFalse(edge.Update(true));
    }
}
