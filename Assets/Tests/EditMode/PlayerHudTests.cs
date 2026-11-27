using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Sol üst piksel göstergenin düz hesapları: kalp dizilimi (tam/yarım/boş), piksel ölçeği, bekleme oranı, simge sırası,
/// yanıp sönme, kalp kırılma/dolma zamanları, etkilerden simge listesi.
/// </summary>
public class PlayerHudTests
{
    // ---- Kalpler ----

    [Test]
    public void Hearts_OnePerHealth_FullThenEmpty()
    {
        HeartFill[] hearts = HudRules.Hearts(2, 3, 1);
        CollectionAssert.AreEqual(new[] { HeartFill.Full, HeartFill.Full, HeartFill.Empty }, hearts);
    }

    [Test]
    public void Hearts_TwoPerHeart_ShowHalf()
    {
        Assert.AreEqual(2, HudRules.HeartCount(3, 2), "3 can, kalp başına 2 → 2 kalp");
        CollectionAssert.AreEqual(new[] { HeartFill.Full, HeartFill.Half }, HudRules.Hearts(3, 3, 2));
        CollectionAssert.AreEqual(new[] { HeartFill.Half, HeartFill.Empty }, HudRules.Hearts(1, 4, 2));
    }

    [Test]
    public void Hearts_ClampOutOfRange()
    {
        CollectionAssert.AreEqual(new[] { HeartFill.Empty, HeartFill.Empty }, HudRules.Hearts(-3, 2, 1));
        CollectionAssert.AreEqual(new[] { HeartFill.Full, HeartFill.Full }, HudRules.Hearts(9, 2, 1));
        Assert.AreEqual(1, HudRules.HeartCount(0, 1), "En az bir kalp");
    }

    [Test]
    public void LastFilledHeart_IsRightmostNonEmpty()
    {
        Assert.AreEqual(0, HudRules.LastFilledHeart(HudRules.Hearts(1, 3, 1)));
        Assert.AreEqual(1, HudRules.LastFilledHeart(new[] { HeartFill.Full, HeartFill.Half, HeartFill.Empty }));
        Assert.AreEqual(-1, HudRules.LastFilledHeart(HudRules.Hearts(0, 3, 1)));
    }

    // ---- Piksel ölçeği ----

    [Test]
    public void PixelScale_IsWholeMultipleOfReference()
    {
        Assert.AreEqual(2, HudRules.PixelScale(720, 360, 1, 6));
        Assert.AreEqual(3, HudRules.PixelScale(1080, 360, 1, 6));
        Assert.AreEqual(3, HudRules.PixelScale(1400, 360, 1, 6), "Aşağı yuvarlanır");
        Assert.AreEqual(1, HudRules.PixelScale(200, 360, 1, 6), "En az min");
        Assert.AreEqual(6, HudRules.PixelScale(4320, 360, 1, 6), "En fazla max");
        Assert.AreEqual(2, HudRules.PixelScale(1080, 0, 2, 6), "Referans yoksa min");
    }

    [Test]
    public void SnapToPixel_RoundsToScale()
    {
        Assert.AreEqual(6f, HudRules.SnapToPixel(7f, 3));
        Assert.AreEqual(-3f, HudRules.SnapToPixel(-2f, 3));
        Assert.AreEqual(7f, HudRules.SnapToPixel(7.2f, 1));
    }

    // ---- Bekleme ----

    [Test]
    public void CooldownFraction_ClampsAndHandlesZero()
    {
        Assert.AreEqual(0.5f, HudRules.CooldownFraction(1.5f, 3f), 1e-5f);
        Assert.AreEqual(0f, HudRules.CooldownFraction(0f, 3f));
        Assert.AreEqual(0f, HudRules.CooldownFraction(2f, 0f));
        Assert.AreEqual(1f, HudRules.CooldownFraction(5f, 3f));
    }

    [Test]
    public void DurationGauge_UsesStartAsTotal_AndRefreshes()
    {
        var gauge = new DurationGauge();
        gauge.Update(3f);
        Assert.AreEqual(3f, gauge.Total, 1e-5f);
        gauge.Update(1.5f);
        Assert.AreEqual(0.5f, gauge.FilledFraction, 1e-5f);
        gauge.Update(4f); // tazelendi
        Assert.AreEqual(4f, gauge.Total, 1e-5f);
        Assert.AreEqual(1f, gauge.RemainingFraction, 1e-5f);
        gauge.Update(0f);
        Assert.AreEqual(0f, gauge.Total);
        Assert.AreEqual(0f, gauge.RemainingFraction);
    }

    [Test]
    public void DurationGauge_MinimumTotal_WhenOpenedMidway()
    {
        var gauge = new DurationGauge();
        gauge.Update(1f, 3f);
        Assert.AreEqual(3f, gauge.Total, 1e-5f);
        Assert.AreEqual(1f / 3f, gauge.RemainingFraction, 1e-5f);
    }

    [Test]
    public void SecondsLabel_RoundsUp()
    {
        Assert.AreEqual(1, HudRules.SecondsLabel(0.2f));
        Assert.AreEqual(3, HudRules.SecondsLabel(3f));
        Assert.AreEqual(0, HudRules.SecondsLabel(-1f));
    }

    // ---- Yanıp sönme ----

    [Test]
    public void Blink_OnlyNearTheEnd()
    {
        Assert.IsTrue(HudRules.IsBlinkVisible(5f, 2f, 0.3f, 4f), "Uzun süre kaldıysa hep görünür");
        Assert.IsTrue(HudRules.IsBlinkVisible(-1f, 2f, 0.3f, 4f), "Süre bilinmiyorsa hep görünür");
        Assert.IsTrue(HudRules.IsBlinkVisible(1f, 2f, 0.0f, 4f));
        Assert.IsFalse(HudRules.IsBlinkVisible(1f, 2f, 0.13f, 4f), "Saniyede 4 kez: 0,125-0,25 arası sönük");
        Assert.IsTrue(HudRules.IsBlinkVisible(1f, 2f, 0.26f, 4f));
        Assert.IsTrue(HudRules.IsBlinkVisible(1f, 2f, 0.13f, 0f), "Hz 0 ise yanıp sönmez");
    }

    // ---- Sıra ----

    [Test]
    public void Sort_FollowsPriority_AndIsStable()
    {
        var list = new List<HudStatus>
        {
            new HudStatus(HudStatusKind.Slow, 1f),
            new HudStatus(HudStatusKind.Shield, 2f, 1),
            new HudStatus(HudStatusKind.Burn, 3f),
            new HudStatus(HudStatusKind.Slow, 4f),
            new HudStatus(HudStatusKind.Weapon, 5f, 2),
        };
        HudRules.SortByPriority(list);
        Assert.AreEqual(HudStatusKind.Shield, list[0].Kind);
        Assert.AreEqual(HudStatusKind.Weapon, list[1].Kind);
        Assert.AreEqual(HudStatusKind.Burn, list[2].Kind);
        Assert.AreEqual(1f, list[3].TimeLeft, "Aynı tür eski sırasını korur");
        Assert.AreEqual(4f, list[4].TimeLeft);
    }

    [Test]
    public void EveryKind_HasPriorityAndIcon()
    {
        foreach (HudStatusKind kind in System.Enum.GetValues(typeof(HudStatusKind)))
        {
            Assert.Less(HudRules.PriorityOf(kind), HudRules.Priority.Length, kind + " sırada yok");
            StringAssert.StartsWith("ui_status_", HudRules.IconName(kind));
        }
    }

    [Test]
    public void FallbackColors_CoverEveryKind()
    {
        var config = ScriptableObject.CreateInstance<GameConfig>();
        Assert.GreaterOrEqual(config.hudStatusFallbackColors.Length, System.Enum.GetValues(typeof(HudStatusKind)).Length);
        Object.DestroyImmediate(config);
    }

    // ---- Kırılma ----

    [Test]
    public void BreakFrame_StepsThroughFrames_ThenEnds()
    {
        Assert.AreEqual(0, HudRules.BreakFrame(0f, 0.4f, 4));
        Assert.AreEqual(1, HudRules.BreakFrame(0.15f, 0.4f, 4));
        Assert.AreEqual(3, HudRules.BreakFrame(0.39f, 0.4f, 4));
        Assert.AreEqual(-1, HudRules.BreakFrame(0.4f, 0.4f, 4));
        Assert.AreEqual(-1, HudRules.BreakFrame(-0.1f, 0.4f, 4));
    }

    [Test]
    public void Shards_FlyOutThenFall()
    {
        Vector2 left = HudRules.ShardOffset(0, 4, 0.1f, 40f, 200f);
        Vector2 right = HudRules.ShardOffset(3, 4, 0.1f, 40f, 200f);
        Assert.Less(left.x, 0f);
        Assert.Greater(right.x, 0f);
        Assert.Less(left.y, 0f, "Önce yukarı fırlar (y aşağı pozitif)");
        Vector2 late = HudRules.ShardOffset(0, 4, 1f, 40f, 200f);
        Assert.Greater(late.y, 0f, "Sonra yerçekimiyle düşer");
        Assert.AreEqual(Vector2.zero, HudRules.ShardOffset(1, 4, 0f, 40f, 200f));
    }

    // ---- Kalp canlandırma ----

    [Test]
    public void HeartAnimator_FirstUpdate_DoesNotAnimate()
    {
        var anim = new HeartRowAnimator();
        anim.Update(2, 3, 1, 10f);
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(float.MaxValue, anim.BreakAge(i, 10f));
            Assert.AreEqual(float.MaxValue, anim.FillAge(i, 10f));
        }
    }

    [Test]
    public void HeartAnimator_LosingHealth_BreaksThatHeart()
    {
        var anim = new HeartRowAnimator();
        anim.Update(3, 3, 1, 0f);
        anim.Update(2, 3, 1, 5f);
        Assert.AreEqual(0.5f, anim.BreakAge(2, 5.5f), 1e-5f);
        Assert.AreEqual(HeartFill.Full, anim.BrokenFrom(2));
        Assert.AreEqual(float.MaxValue, anim.BreakAge(1, 5.5f), "Diğer kalpler kırılmaz");
        Assert.AreEqual(HeartFill.Empty, anim.Fills[2]);
    }

    [Test]
    public void HeartAnimator_Healing_FillsThatHeart_AndCancelsBreak()
    {
        var anim = new HeartRowAnimator();
        anim.Update(3, 3, 1, 0f);
        anim.Update(2, 3, 1, 1f);
        anim.Update(3, 3, 1, 2f);
        Assert.AreEqual(0.25f, anim.FillAge(2, 2.25f), 1e-5f);
        Assert.AreEqual(float.MaxValue, anim.BreakAge(2, 2.25f));
    }

    [Test]
    public void HeartAnimator_Reset_SkipsAnimationOnNextUpdate()
    {
        var anim = new HeartRowAnimator();
        anim.Update(3, 3, 1, 0f);
        anim.Reset();
        anim.Update(1, 3, 1, 1f);
        Assert.AreEqual(float.MaxValue, anim.BreakAge(2, 1f));
    }

    // ---- Etkilerden simgeler ----

    [Test]
    public void Collect_FromStatusEffects_TimesAndCounts()
    {
        var status = new StatusEffects();
        status.ApplySlow(0.5f, 2f);
        status.ApplyShield(2, 6f);
        status.ApplyStealth(4f, "cali");
        var list = new List<HudStatus>();
        HudStatusCollector.Collect(status, list);

        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(HudStatusKind.Shield, list[0].Kind);
        Assert.AreEqual(2, list[0].Count, "Kalkanın kalan bloğu rozet olur");
        Assert.AreEqual(6f, list[0].TimeLeft, 1e-5f);
        Assert.AreEqual(HudStatusKind.Disguise, list[1].Kind, "Kılıklı görünmezlik ayrı simge");
        Assert.AreEqual(HudStatusKind.Slow, list[2].Kind);
        Assert.AreEqual(-1, list[2].Count);

        status.TryAbsorbHit();
        list.Clear();
        HudStatusCollector.Collect(status, list);
        Assert.AreEqual(1, list[0].Count);
    }

    [Test]
    public void Collect_Weapon_HasShotsAndOwnSprite()
    {
        var status = new StatusEffects();
        var gun = new WordEntry { id = "tabanca", sprite = "tabanca_spr" };
        status.ArmWeapon(gun, 3, 10f);
        var list = new List<HudStatus>();
        HudStatusCollector.Collect(status, list);
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(HudStatusKind.Weapon, list[0].Kind);
        Assert.AreEqual(3, list[0].Count);
        Assert.AreEqual("tabanca_spr", list[0].SpriteOverride);
        Assert.AreEqual("tabanca", HudStatusCollector.WeaponSprite(new WordEntry { id = "tabanca" }));
        Assert.IsNull(HudStatusCollector.WeaponSprite(null));
    }

    [Test]
    public void Collect_FromFlags_HasNoTimes()
    {
        var list = new List<HudStatus>();
        HudStatusCollector.Collect(StatusFlags.Slow | StatusFlags.Shield | StatusFlags.Burning, list);
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(HudStatusKind.Shield, list[0].Kind);
        Assert.AreEqual(HudStatusKind.Burn, list[1].Kind);
        Assert.AreEqual(HudStatusKind.Slow, list[2].Kind);
        foreach (HudStatus s in list) Assert.IsFalse(s.HasTime);
    }

    [Test]
    public void Collect_NothingActive_IsEmpty()
    {
        var list = new List<HudStatus>();
        HudStatusCollector.Collect(new StatusEffects(), list);
        HudStatusCollector.Collect(StatusFlags.None, list);
        HudStatusCollector.Collect((StatusEffects)null, list);
        Assert.IsEmpty(list);
    }

    [Test]
    public void Snapshot_Clear_ResetsEverything()
    {
        var hud = new HudSnapshot { HasPlayer = true, Alive = true, Health = 2, ShoveLeft = 1f, Armed = true, WeaponShots = 3, ChanceText = "x" };
        hud.Statuses.Add(new HudStatus(HudStatusKind.Slow, 1f));
        hud.Clear();
        Assert.IsFalse(hud.HasPlayer);
        Assert.IsFalse(hud.Armed);
        Assert.AreEqual(-1, hud.WeaponShots);
        Assert.IsEmpty(hud.Statuses);
        Assert.AreEqual(string.Empty, hud.ChanceText);
    }

    [Test]
    public void FillHud_BaseSource_ReadsLocalPlayer()
    {
        var go = new GameObject("hud test");
        var source = go.AddComponent<FakeSource>();
        var hud = new HudSnapshot();

        source.FillHud(hud);
        Assert.IsFalse(hud.HasPlayer, "Yerel oyuncu yoksa gösterge boş");

        source.Local = new PlayerState(1, "Ben", 3);
        source.Local.TakeDamage(1);
        source.Local.Status.ApplyShield(1, 5f);
        source.FillHud(hud);
        Assert.IsTrue(hud.HasPlayer);
        Assert.IsTrue(hud.Alive);
        Assert.AreEqual(2, hud.Health);
        Assert.AreEqual(3, hud.MaxHealth);
        Assert.AreEqual(1, hud.Statuses.Count);

        source.Local.Eliminate(); // (TakeDamage kalkana takılırdı)
        source.FillHud(hud);
        Assert.IsFalse(hud.Alive);
        Assert.IsEmpty(hud.Statuses, "Hayalette etki simgesi yok");
        Object.DestroyImmediate(go);
    }

    /// <summary>Taban <see cref="TurnViewSource.FillHud"/>'u denemek için en küçük tur kaynağı.</summary>
    class FakeSource : TurnViewSource
    {
        public PlayerState Local;
        public override ITurnView View => null;
        public override IReadOnlyList<PlayerState> Players => new List<PlayerState>();
        public override IReadOnlyList<WordEntry> UsedWords => new List<WordEntry>();
        public override PlayerState LocalPlayer => Local;
        public override string DescribeLocalStatus() => string.Empty;
        public override string DescribeShove() => string.Empty;
        public override void SubmitWord(string text, System.Action<SubmitStatus> onResult) { }
        public override bool CanRestart => false;
        public override void Restart() { }
    }
}
