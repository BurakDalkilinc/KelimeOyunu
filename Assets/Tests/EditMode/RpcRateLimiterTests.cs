using NUnit.Framework;

/// <summary>RpcRateLimiter testleri: mesaj seli engellenir, normal oyun etkilenmez.</summary>
public class RpcRateLimiterTests
{
    [Test]
    public void Burst_IsAllowedUpToCapacity_ThenBlocked()
    {
        var limiter = new RpcRateLimiter(3f, 1f);

        Assert.IsTrue(limiter.Allow(1, 0f));
        Assert.IsTrue(limiter.Allow(1, 0f));
        Assert.IsTrue(limiter.Allow(1, 0f));
        Assert.IsFalse(limiter.Allow(1, 0f));
    }

    [Test]
    public void Tokens_RefillOverTime()
    {
        var limiter = new RpcRateLimiter(2f, 2f);
        limiter.Allow(1, 0f);
        limiter.Allow(1, 0f);
        Assert.IsFalse(limiter.Allow(1, 0f));

        Assert.IsFalse(limiter.Allow(1, 0.2f)); // 0,4 hak: yetmez
        Assert.IsTrue(limiter.Allow(1, 0.6f));  // 1,2 hak: bir mesaj geçer
        Assert.IsFalse(limiter.Allow(1, 0.6f));
    }

    [Test]
    public void LongIdle_NeverStoresMoreThanTheCapacity()
    {
        var limiter = new RpcRateLimiter(2f, 5f);
        limiter.Allow(1, 0f);

        Assert.IsTrue(limiter.Allow(1, 1000f));
        Assert.IsTrue(limiter.Allow(1, 1000f));
        Assert.IsFalse(limiter.Allow(1, 1000f)); // Kova 2'den fazla dolmadı
    }

    [Test]
    public void Senders_AreIndependent()
    {
        var limiter = new RpcRateLimiter(1f, 0f);

        Assert.IsTrue(limiter.Allow(1, 0f));
        Assert.IsFalse(limiter.Allow(1, 0f));
        Assert.IsTrue(limiter.Allow(2, 0f)); // Diğer oyuncu etkilenmez
    }

    [Test]
    public void TimeGoingBackwards_DoesNotGiveFreeTokens()
    {
        var limiter = new RpcRateLimiter(1f, 10f);
        Assert.IsTrue(limiter.Allow(1, 100f));

        Assert.IsFalse(limiter.Allow(1, 50f)); // Saat geri gitti: hak yenilenmez
    }

    [Test]
    public void Forget_ResetsTheSender()
    {
        var limiter = new RpcRateLimiter(1f, 0f);
        limiter.Allow(1, 0f);
        Assert.IsFalse(limiter.Allow(1, 0f));

        limiter.Forget(1);

        Assert.IsTrue(limiter.Allow(1, 0f));
    }

    [Test]
    public void NormalHumanPlay_NeverHitsTheTypingLimit()
    {
        // Yazma: en çok saniyede 2 deneme (reddedilen kelimeyi düzeltip tekrar gönderme) bile sorunsuz geçer.
        var limiter = new RpcRateLimiter(5f, 2f);
        float t = 0f;
        for (int i = 0; i < 60; i++)
        {
            Assert.IsTrue(limiter.Allow(1, t), $"{i}. deneme reddedildi");
            t += 0.5f;
        }
    }

    [Test]
    public void FloodOfAThousandMessages_IsCutDownToTheBurst()
    {
        var limiter = new RpcRateLimiter(5f, 2f);
        int accepted = 0;
        for (int i = 0; i < 1000; i++)
            if (limiter.Allow(7, 0.001f * i)) accepted++; // 1 saniyede 1000 mesaj

        Assert.LessOrEqual(accepted, 8); // 5 patlama + yaklaşık 2 yenilenen
    }

    [Test]
    public void InvalidSettings_AreCorrected()
    {
        var limiter = new RpcRateLimiter(0f, -5f);

        Assert.IsTrue(limiter.Allow(1, 0f));   // En az 1 hak
        Assert.IsFalse(limiter.Allow(1, 10f)); // Negatif hız 0 sayılır
    }
}
