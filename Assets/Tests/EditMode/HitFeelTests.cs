using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Vuruş hissinin düz hesapları: yay (ezilme, kamera itmesi), travma sarsıntısı, donma/parlama/ezilme, hasar sayısı uçuşu,
/// kalp atışı, ağır çekim, yaklaşan darbe kaydı, ayarlardaki ekran sarsıntısı ve kalkan engelleme sayacı.
/// </summary>
public class HitFeelTests
{
    // ---- SpringValue ----

    [Test]
    public void Spring_SettlesAtTarget()
    {
        var spring = new SpringValue(6f, 0.35f, 1f);
        for (int i = 0; i < 300; i++) spring.Step(1f / 60f);
        Assert.AreEqual(0f, spring.Value, 0.001f);
        Assert.IsTrue(spring.IsSettled(0.002f));
    }

    [Test]
    public void Spring_LowDamping_Overshoots_HighDamping_DoesNot()
    {
        var bouncy = new SpringValue(6f, 0.3f, 1f);
        var stiff = new SpringValue(6f, 1.2f, 1f);
        float bouncyMin = 1f, stiffMin = 1f;
        for (int i = 0; i < 120; i++)
        {
            bouncyMin = Mathf.Min(bouncyMin, bouncy.Step(1f / 60f));
            stiffMin = Mathf.Min(stiffMin, stiff.Step(1f / 60f));
        }
        Assert.Less(bouncyMin, -0.1f, "Az sönümlü yay hedefi aşıp ters yöne geçmeli (yaylanma)");
        Assert.GreaterOrEqual(stiffMin, -0.0001f, "Çok sönümlü yay hedefi aşmamalı");
    }

    [Test]
    public void Spring_IsFrameRateIndependent()
    {
        var slow = new SpringValue(5f, 0.4f, 1f);
        var fast = new SpringValue(5f, 0.4f, 1f);
        for (int i = 0; i < 6; i++) slow.Step(1f / 20f);     // 0,3 sn, ağır kareler
        for (int i = 0; i < 43; i++) fast.Step(0.3f / 43f);  // 0,3 sn, hızlı kareler
        Assert.AreEqual(fast.Value, slow.Value, 0.01f);
    }

    [Test]
    public void Spring_Kick_MovesThenReturns()
    {
        var spring = new SpringValue(4f, 0.5f);
        spring.Kick(5f);
        spring.Step(0.03f);
        Assert.Greater(spring.Value, 0f);
        for (int i = 0; i < 400; i++) spring.Step(1f / 60f);
        Assert.AreEqual(0f, spring.Value, 0.001f);
    }

    [Test]
    public void Spring_ZeroOrNegativeStep_DoesNothing()
    {
        var spring = new SpringValue(4f, 0.5f, 0.7f);
        spring.Step(0f);
        spring.Step(-1f);
        Assert.AreEqual(0.7f, spring.Value, 1e-6f);
    }

    // ---- Trauma ----

    [Test]
    public void Trauma_ClampsAndShakeIsSquared()
    {
        var trauma = new Trauma();
        trauma.Add(0.5f, 1f);
        Assert.AreEqual(0.5f, trauma.Value, 1e-5f);
        Assert.AreEqual(0.25f, trauma.Shake, 1e-5f);
        trauma.Add(0.9f, 1f);
        Assert.AreEqual(1f, trauma.Value, 1e-5f);
    }

    [Test]
    public void Trauma_DecaysOverGivenSeconds()
    {
        var trauma = new Trauma();
        trauma.Add(0.6f, 0.5f);
        trauma.Step(0.25f);
        Assert.AreEqual(0.3f, trauma.Value, 1e-4f);
        trauma.Step(0.3f);
        Assert.AreEqual(0f, trauma.Value);
        Assert.AreEqual(Vector2.zero, trauma.Offset(1f, 3f, 25f, 0.6f));
    }

    [Test]
    public void Trauma_SmallShake_DoesNotShortenBigOne()
    {
        var trauma = new Trauma();
        trauma.Add(0.8f, 1f);       // 1 sn sürecek
        trauma.Step(0.1f);          // kalan ~0,9 sn
        trauma.Add(0.05f, 0.1f);    // kısa küçük darbe
        float remaining = trauma.Value / trauma.DecayPerSecond;
        Assert.GreaterOrEqual(remaining, 0.89f);
    }

    [Test]
    public void Trauma_AmountFor_ReproducesOldAmplitude()
    {
        float amount = Trauma.AmountFor(0.15f, 0.6f, 2f);
        var trauma = new Trauma { Exponent = 2f };
        trauma.Add(amount, 1f);
        Assert.AreEqual(0.15f, trauma.Shake * 0.6f, 1e-4f, "Tepe kayma eski genliğe eşit olmalı");
        Assert.AreEqual(0f, Trauma.AmountFor(0f, 0.6f, 2f));
        Assert.AreEqual(1f, Trauma.AmountFor(5f, 0.6f, 2f), 1e-5f);
    }

    [Test]
    public void Trauma_Offset_StaysWithinMax()
    {
        var trauma = new Trauma();
        trauma.Add(1f, 1f);
        for (int i = 0; i < 50; i++)
        {
            Vector2 offset = trauma.Offset(i * 0.037f, 4f, 28f, 0.6f);
            Assert.LessOrEqual(Mathf.Abs(offset.x), 0.6f + 1e-4f);
            Assert.LessOrEqual(Mathf.Abs(offset.y), 0.6f + 1e-4f);
        }
    }

    // ---- HitFeelMath ----

    [Test]
    public void HitStop_GiantLonger_DeathLongest()
    {
        Assert.AreEqual(0.07f, HitFeelMath.HitStopSeconds(false, false, 0.07f, 0.12f, 0.18f), 1e-6f);
        Assert.AreEqual(0.12f, HitFeelMath.HitStopSeconds(true, false, 0.07f, 0.12f, 0.18f), 1e-6f);
        Assert.AreEqual(0.18f, HitFeelMath.HitStopSeconds(false, true, 0.07f, 0.12f, 0.18f), 1e-6f);
        Assert.AreEqual(0f, HitFeelMath.HitStopSeconds(false, false, -1f, 0f, 0f));
    }

    [Test]
    public void Flash_WhiteThenFadingTintThenNone()
    {
        Assert.AreEqual(HitFlashPhase.White, HitFeelMath.Flash(0f, 0.035f, 0.2f, out _));
        Assert.AreEqual(HitFlashPhase.White, HitFeelMath.Flash(0.02f, 0.035f, 0.2f, out _));

        Assert.AreEqual(HitFlashPhase.Tint, HitFeelMath.Flash(0.04f, 0.035f, 0.2f, out float early));
        Assert.AreEqual(HitFlashPhase.Tint, HitFeelMath.Flash(0.15f, 0.035f, 0.2f, out float late));
        Assert.Greater(early, late);
        Assert.LessOrEqual(early, 1f);

        Assert.AreEqual(HitFlashPhase.None, HitFeelMath.Flash(0.3f, 0.035f, 0.2f, out float none));
        Assert.AreEqual(0f, none);
        Assert.AreEqual(HitFlashPhase.None, HitFeelMath.Flash(-0.1f, 0.035f, 0.2f, out _));
    }

    [Test]
    public void Squash_FollowsHitAxis_AndInverts()
    {
        Vector2 vertical = HitFeelMath.SquashScale(null, 1f, 0.2f, 0.25f);
        Assert.AreEqual(1.25f, vertical.x, 1e-5f);
        Assert.AreEqual(0.8f, vertical.y, 1e-5f);

        Vector2 sideways = HitFeelMath.SquashScale(Vector2.left, 1f, 0.2f, 0.25f);
        Assert.AreEqual(0.8f, sideways.x, 1e-5f);
        Assert.AreEqual(1.25f, sideways.y, 1e-5f);

        Assert.AreEqual(Vector2.one, HitFeelMath.SquashScale(Vector2.up, 0f, 0.2f, 0.25f));

        Vector2 stretched = HitFeelMath.SquashScale(Vector2.right, -1f, 0.2f, 0.1f); // Atılma: gidiş yönünde uzar
        Assert.Greater(stretched.x, 1f);
        Assert.Less(stretched.y, 1f);
    }

    [Test]
    public void ShakeScale_OffIsZero_StrengthClamped()
    {
        Assert.AreEqual(0f, HitFeelMath.ShakeScale(false, 1f));
        Assert.AreEqual(0.5f, HitFeelMath.ShakeScale(true, 0.5f), 1e-6f);
        Assert.AreEqual(1f, HitFeelMath.ShakeScale(true, 3f));
        Assert.AreEqual(0f, HitFeelMath.ShakeScale(true, float.NaN));
        Assert.AreEqual(1f, HitFeelMath.ScreenFlashScale(true, 0.4f));
        Assert.AreEqual(0.4f, HitFeelMath.ScreenFlashScale(false, 0.4f), 1e-6f);
    }

    [Test]
    public void DistanceFalloff_FullNearZeroFar()
    {
        Assert.AreEqual(1f, HitFeelMath.DistanceFalloff(2f, 4f, 16f));
        Assert.AreEqual(0.5f, HitFeelMath.DistanceFalloff(10f, 4f, 16f), 1e-5f);
        Assert.AreEqual(0f, HitFeelMath.DistanceFalloff(20f, 4f, 16f));
    }

    [Test]
    public void DamageNumber_RisesThenFalls_FadesAtEnd_PopsAtStart()
    {
        Assert.AreEqual("-1", HitFeelMath.DamageText(1));
        Assert.AreEqual("-3", HitFeelMath.DamageText(3));
        Assert.AreEqual("-1", HitFeelMath.DamageText(0));

        var velocity = new Vector2(0.5f, 3.4f);
        float peakTime = 3.4f / 9f;
        Vector2 peak = HitFeelMath.DamageNumberOffset(peakTime, velocity, 9f);
        Vector2 later = HitFeelMath.DamageNumberOffset(peakTime * 1.8f, velocity, 9f);
        Assert.Greater(peak.y, 0.5f);
        Assert.Less(later.y, peak.y, "Tepe noktasından sonra düşmeli");
        Assert.Greater(later.x, peak.x);

        Assert.AreEqual(1f, HitFeelMath.DamageNumberAlpha(0.1f, 0.75f, 0.35f));
        Assert.Less(HitFeelMath.DamageNumberAlpha(0.7f, 0.75f, 0.35f), 0.5f);
        Assert.AreEqual(0f, HitFeelMath.DamageNumberAlpha(0.8f, 0.75f, 0.35f));

        Assert.AreEqual(1.7f, HitFeelMath.DamageNumberScale(0f, 0.1f, 1.7f), 1e-5f);
        Assert.AreEqual(1f, HitFeelMath.DamageNumberScale(0.2f, 0.1f, 1.7f));
    }

    [Test]
    public void RandomPitch_WithinVariation()
    {
        Assert.AreEqual(0.92f, HitFeelMath.RandomPitch(0f, 0.08f), 1e-5f);
        Assert.AreEqual(1.08f, HitFeelMath.RandomPitch(1f, 0.08f), 1e-5f);
        Assert.AreEqual(1f, HitFeelMath.RandomPitch(0.5f, 0.08f), 1e-5f);
    }

    [Test]
    public void LowHealth_OnlyWhenAliveAtThreshold()
    {
        Assert.IsTrue(HitFeelMath.IsLowHealth(1, 3, 1));
        Assert.IsFalse(HitFeelMath.IsLowHealth(2, 3, 1));
        Assert.IsFalse(HitFeelMath.IsLowHealth(0, 3, 1), "Hayalet için nabız yok");
        Assert.IsFalse(HitFeelMath.IsLowHealth(1, 1, 1), "Tek canlık maçta hep çalmasın");
    }

    [Test]
    public void Heartbeat_TwoBeatsPerCycle_ThenQuiet()
    {
        float period = 60f / 72f;
        Assert.Greater(HitFeelMath.HeartbeatPulse(period * 0.075f, 72f, out int first), 0.9f);
        Assert.AreEqual(0, first);
        Assert.Greater(HitFeelMath.HeartbeatPulse(period * 0.295f, 72f, out int second), 0.6f);
        Assert.AreEqual(1, second);
        Assert.AreEqual(0f, HitFeelMath.HeartbeatPulse(period * 0.7f, 72f, out _));
        Assert.AreEqual(0f, HitFeelMath.HeartbeatPulse(1f, 0f, out _));
    }

    [Test]
    public void CrossedPhase_DetectsOnsetsAndWrap()
    {
        Assert.IsTrue(HitFeelMath.CrossedPhase(0.1f, 0.3f, 0.22f));
        Assert.IsFalse(HitFeelMath.CrossedPhase(0.25f, 0.3f, 0.22f));
        Assert.IsTrue(HitFeelMath.CrossedPhase(0.95f, 0.05f, 0f), "Başa sarınca ilk vuruş çalmalı");
        Assert.IsFalse(HitFeelMath.CrossedPhase(0.05f, 0.1f, 0f));
    }

    [Test]
    public void SlowMotion_HoldsThenEasesBackToOne()
    {
        Assert.AreEqual(0.3f, HitFeelMath.SlowMotionScale(0f, 0.8f, 0.3f, 0.35f), 1e-5f);
        Assert.AreEqual(0.3f, HitFeelMath.SlowMotionScale(0.4f, 0.8f, 0.3f, 0.35f), 1e-5f);
        float easing = HitFeelMath.SlowMotionScale(0.7f, 0.8f, 0.3f, 0.35f);
        Assert.Greater(easing, 0.3f);
        Assert.Less(easing, 1f);
        Assert.AreEqual(1f, HitFeelMath.SlowMotionScale(0.8f, 0.8f, 0.3f, 0.35f));
        Assert.AreEqual(1f, HitFeelMath.SlowMotionScale(0.1f, 0f, 0.3f, 0.35f), "Süre 0 ise ağır çekim yok");
    }

    [Test]
    public void BossHealthFromLabel_ReadsTrailingFraction()
    {
        Assert.AreEqual(7, HitFeelMath.BossHealthFromLabel("Godzilla 7/10"));
        Assert.AreEqual(0, HitFeelMath.BossHealthFromLabel("{w} 0/10"));
        Assert.AreEqual(12, HitFeelMath.BossHealthFromLabel("{w} 12/20"));
        Assert.AreEqual(-1, HitFeelMath.BossHealthFromLabel("{w}"));
        Assert.AreEqual(-1, HitFeelMath.BossHealthFromLabel(""));
        Assert.AreEqual(-1, HitFeelMath.BossHealthFromLabel(null));
        Assert.AreEqual(-1, HitFeelMath.BossHealthFromLabel("a/b"));
        Assert.AreEqual(-1, HitFeelMath.BossHealthFromLabel("Bomba 3"));
    }

    [Test]
    public void BossHealthFromLabel_MatchesBossRulesLabel()
    {
        var boss = new CreatureModel(null, null, Vector2.zero, 1f, 1, 1f, 10f, new ArenaBounds(new Vector2(20f, 20f)));
        boss.MakeBoss(10);
        boss.TakeBossHit(null, 3);
        Assert.AreEqual(boss.BossHealth, HitFeelMath.BossHealthFromLabel(BossRules.Label(boss, CreatureLabel.NameToken)));
    }

    // ---- StrikeRegistry ----

    [Test]
    public void Strikes_NearestInRangeIsTakenOnce()
    {
        var registry = new StrikeRegistry();
        registry.Add(new IncomingStrike { Position = new Vector2(0f, 0f), ImpactTime = 1.1f, Direction = Vector2.right });
        registry.Add(new IncomingStrike { Position = new Vector2(0.5f, 0f), ImpactTime = 1.1f, Direction = Vector2.left });

        Assert.IsTrue(registry.TryTake(new Vector2(0.45f, 0f), 1f, 1.3f, 2.5f, 0.15f, 0.35f, out IncomingStrike found));
        Assert.AreEqual(Vector2.left, found.Direction, "En yakın darbe seçilmeli");
        Assert.AreEqual(1, registry.Count);

        Assert.IsTrue(registry.TryTake(new Vector2(0.45f, 0f), 1f, 1.3f, 2.5f, 0.15f, 0.35f, out found));
        Assert.AreEqual(Vector2.right, found.Direction);
        Assert.IsFalse(registry.TryTake(new Vector2(0.45f, 0f), 1f, 1.3f, 2.5f, 0.15f, 0.35f, out _));
    }

    [Test]
    public void Strikes_OutOfRangeOrTimeAreIgnored_OldOnesPruned()
    {
        var registry = new StrikeRegistry();
        registry.Add(new IncomingStrike { Position = new Vector2(5f, 0f), ImpactTime = 1.1f });                 // uzak
        registry.Add(new IncomingStrike { Position = Vector2.zero, ImpactTime = 2f });                           // çok ileride
        registry.Add(new IncomingStrike { Position = Vector2.zero, ImpactTime = 0.5f });                         // çoktan geçti

        Assert.IsFalse(registry.TryTake(Vector2.zero, 1f, 1.3f, 2.5f, 0.15f, 0.35f, out _));
        Assert.AreEqual(2, registry.Count, "Süresi geçmiş darbe atılmalı");
    }

    [Test]
    public void Strikes_GiantReachesFarther_RecentPastAccepted()
    {
        var registry = new StrikeRegistry();
        registry.Add(new IncomingStrike { Position = new Vector2(2.5f, 0f), ImpactTime = 0.9f, Giant = true });
        Assert.IsTrue(registry.TryTake(Vector2.zero, 1f, 1.3f, 2.5f, 0.15f, 0.35f, out IncomingStrike found),
                      "Dev darbesi daha geniş eşleşir; az önce görünmüş darbe de kabul (ağ gecikmesi)");
        Assert.IsTrue(found.Giant);
    }

    // ---- Ayarlar ve kalkan ----

    [Test]
    public void Settings_ScreenShake_DefaultsOn_RoundTrips_AndClamps()
    {
        var defaults = new GameSettings();
        Assert.IsTrue(defaults.screenShake);
        Assert.AreEqual(1f, defaults.screenShakeStrength);

        var changed = new GameSettings { screenShake = false, screenShakeStrength = 0.4f };
        GameSettings loaded = SettingsStorage.FromJson(SettingsStorage.ToJson(changed));
        Assert.IsFalse(loaded.screenShake);
        Assert.AreEqual(0.4f, loaded.screenShakeStrength, 1e-5f);

        var wild = new GameSettings { screenShakeStrength = 7f };
        wild.Clamp();
        Assert.AreEqual(1f, wild.screenShakeStrength);
    }

    [Test]
    public void Settings_OldFileWithoutShake_KeepsShakeOn()
    {
        GameSettings loaded = SettingsStorage.FromJson("{\"version\":1,\"masterVolume\":0.5}");
        Assert.IsTrue(loaded.screenShake);
        Assert.AreEqual(1f, loaded.screenShakeStrength);
    }

    [Test]
    public void ShieldBlocks_CountsOnlyAbsorbedHits()
    {
        var player = new PlayerState(0, "Oyuncu", 3);
        Assert.AreEqual(0, player.Status.ShieldBlocks);
        player.TakeDamage(1);
        Assert.AreEqual(0, player.Status.ShieldBlocks);

        player.Status.ApplyShield(1, 5f);
        player.TakeDamage(1);
        Assert.AreEqual(1, player.Status.ShieldBlocks);
        Assert.AreEqual(2, player.Health.Current, "Kalkan hasarı engeller; sayaç kuralı değiştirmez");

        player.TakeDamage(1);
        Assert.AreEqual(1, player.Status.ShieldBlocks);
        Assert.AreEqual(1, player.Health.Current);
    }
}
