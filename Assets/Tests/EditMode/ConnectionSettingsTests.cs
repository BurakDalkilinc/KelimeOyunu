using NUnit.Framework;

/// <summary>ConnectionSettings testleri: "Katıl" adresinin çözümü ve doğrulanması.</summary>
public class ConnectionSettingsTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Empty_MeansLocalMachine_DefaultPort(string text)
    {
        Assert.IsTrue(ConnectionSettings.TryParse(text, out string address, out ushort port, out string error));
        Assert.AreEqual("127.0.0.1", address);
        Assert.AreEqual(7777, port);
        Assert.IsNull(error);
    }

    [Test]
    public void AddressOnly_UsesDefaultPort()
    {
        Assert.IsTrue(ConnectionSettings.TryParse(" 192.168.1.5 ", out string address, out ushort port, out _));
        Assert.AreEqual("192.168.1.5", address);
        Assert.AreEqual(7777, port);
    }

    [Test]
    public void AddressWithPort_ParsesBoth()
    {
        Assert.IsTrue(ConnectionSettings.TryParse("10.0.0.2:9000", out string address, out ushort port, out _));
        Assert.AreEqual("10.0.0.2", address);
        Assert.AreEqual(9000, port);
    }

    [TestCase("abc")]
    [TestCase("192.168.1")]
    [TestCase("192.168.1.256")]
    [TestCase("192.168.1.x")]
    [TestCase("192.168..1")]
    [TestCase("1.2.3.4.5")]
    public void BadAddress_IsRejected_WithMessage(string text)
    {
        Assert.IsFalse(ConnectionSettings.TryParse(text, out _, out _, out string error));
        StringAssert.Contains("IP", error);
    }

    [TestCase("1.2.3.4:0")]
    [TestCase("1.2.3.4:65536")]
    [TestCase("1.2.3.4:abc")]
    [TestCase("1.2.3.4:")]
    public void BadPort_IsRejected_WithMessage(string text)
    {
        Assert.IsFalse(ConnectionSettings.TryParse(text, out _, out _, out string error));
        StringAssert.Contains("Port", error);
    }
}
