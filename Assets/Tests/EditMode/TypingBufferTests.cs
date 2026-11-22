using System;
using NUnit.Framework;

/// <summary>TypingBuffer ve SubmitFeedback testleri: yazma, silme, uzunluk sınırı, emoji ve mesajlar.</summary>
public class TypingBufferTests
{
    static TypingBuffer Type(TypingBuffer buffer, string text)
    {
        foreach (char c in text) buffer.Input(c);
        return buffer;
    }

    [Test]
    public void Input_AppendsCharactersIncludingTurkishLetters()
    {
        var buffer = Type(new TypingBuffer(50), "Işık çöğüşİ 123");
        Assert.AreEqual("Işık çöğüşİ 123", buffer.Text);
    }

    [Test]
    public void Input_BackspaceCharacter_DeletesLast()
    {
        var buffer = Type(new TypingBuffer(50), "kurt");
        buffer.Input('\b');
        Assert.AreEqual("kur", buffer.Text);
    }

    [Test]
    public void Backspace_OnEmpty_DoesNothing()
    {
        var buffer = new TypingBuffer(10);
        Assert.DoesNotThrow(() => buffer.Backspace());
        Assert.IsTrue(buffer.IsEmpty);
    }

    [Test]
    public void Input_EnterAndControlCharacters_AreIgnored()
    {
        var buffer = new TypingBuffer(50);
        buffer.Input('\r');
        buffer.Input('\n');
        buffer.Input('\t');
        buffer.Input('\0');
        buffer.Input('\u001b'); // Escape
        Assert.IsTrue(buffer.IsEmpty);
    }

    [Test]
    public void Input_StopsAtMaxLength()
    {
        var buffer = Type(new TypingBuffer(5), "abcdefgh");
        Assert.AreEqual("abcde", buffer.Text);
        Assert.IsFalse(buffer.TryAppend('x'));
    }

    [Test]
    public void Input_AfterBackspace_CanTypeAgainWhenFull()
    {
        var buffer = Type(new TypingBuffer(3), "abc");
        buffer.Input('\b');
        buffer.Input('z');
        Assert.AreEqual("abz", buffer.Text);
    }

    [Test]
    public void Input_SpacesAreAllowed()
    {
        var buffer = Type(new TypingBuffer(50), "el bombası");
        Assert.AreEqual("el bombası", buffer.Text);
    }

    [Test]
    public void Emoji_IsAddedAndRemovedAsOneUnit()
    {
        var buffer = Type(new TypingBuffer(50), "a😀");
        Assert.AreEqual(3, buffer.Length);       // 1 + 2 karakterlik emoji

        buffer.Backspace();
        Assert.AreEqual("a", buffer.Text);       // Emoji bütün olarak silindi
    }

    [Test]
    public void Emoji_ThatDoesNotFit_IsRejectedWholeNotHalf()
    {
        var buffer = Type(new TypingBuffer(4), "abc");
        buffer.Input('\uD83D'); // Emojinin ilk yarısı: 1 yer kalmış, 2 gerekir
        buffer.Input('\uDE00');

        Assert.AreEqual("abc", buffer.Text);
    }

    [Test]
    public void LoneLowSurrogate_IsRejected()
    {
        var buffer = new TypingBuffer(10);
        Assert.IsFalse(buffer.TryAppend('\uDE00'));
        Assert.IsTrue(buffer.IsEmpty);
    }

    [Test]
    public void DanglingHighSurrogate_IsDroppedWhenNextCharDoesNotCompleteIt()
    {
        var buffer = new TypingBuffer(10);
        buffer.Input('\uD83D');   // Yarım emoji
        buffer.Input('k');        // Tamamlamadı: yarım parça atılır

        Assert.AreEqual("k", buffer.Text);
    }

    [Test]
    public void Clear_EmptiesTheBuffer()
    {
        var buffer = Type(new TypingBuffer(10), "kurt");
        buffer.Clear();
        Assert.IsTrue(buffer.IsEmpty);
        Assert.AreEqual(string.Empty, buffer.Text);
    }

    [Test]
    public void Constructor_ZeroLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TypingBuffer(0));
    }

    // ---- SubmitFeedback ----

    [TestCase(SubmitStatus.Empty)]
    [TestCase(SubmitStatus.TooLong)]
    [TestCase(SubmitStatus.Blocked)]
    [TestCase(SubmitStatus.AlreadyUsed)]
    [TestCase(SubmitStatus.NotWriter)]
    public void Feedback_RejectedStatuses_HaveAMessage(SubmitStatus status)
    {
        Assert.IsNotEmpty(SubmitFeedback.GetMessage(status, 50));
    }

    [TestCase(SubmitStatus.Accepted)]
    [TestCase(SubmitStatus.AcceptedUnknown)]
    [TestCase(SubmitStatus.WrongPhase)]
    public void Feedback_AcceptedOrIrrelevantStatuses_HaveNoMessage(SubmitStatus status)
    {
        Assert.AreEqual(string.Empty, SubmitFeedback.GetMessage(status, 50));
    }

    [Test]
    public void Feedback_TooLong_MentionsTheLimit()
    {
        StringAssert.Contains("50", SubmitFeedback.GetMessage(SubmitStatus.TooLong, 50));
    }

    [Test]
    public void Feedback_EveryStatusIsCovered()
    {
        // Yeni bir SubmitStatus eklenirse bu test hatırlatır: GetMessage'a karar eklemeyi unutma.
        foreach (SubmitStatus status in Enum.GetValues(typeof(SubmitStatus)))
            Assert.DoesNotThrow(() => SubmitFeedback.GetMessage(status, 50));
    }
}
