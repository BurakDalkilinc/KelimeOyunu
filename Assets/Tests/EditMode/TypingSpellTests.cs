using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Yeni yazı kutusu ve kelime büyüsünün düz hesapları: sayaç çubuğu (doluluk, renk, titreme), kutunun hali, tuş perdesi,
/// "..." noktaları, silinen harfin düşüşü, red dağılması, harflere bölme, Bezier yay, büyü zaman çizelgesi (stagger, doğma
/// senkronu, en geç varış), hedef türü, puf kuralı, ekran kenarı hesapları ve süs zarın yüzü.
/// </summary>
public class TypingSpellTests
{
    const float Eps = 1e-4f;

    static SpellTimings Timings(float maxArrive = 0.5f)
    {
        return new SpellTimings
        {
            LeaveStagger = 0.05f, RiseSeconds = 0.2f, FlightSeconds = 0.4f, FlightStagger = 0.04f,
            MaxArriveSeconds = maxArrive, HoverMaxSeconds = 4f,
        };
    }

    // ---- Sayaç çubuğu ----

    [Test]
    public void TimerFraction_IsLeftOverTotal_Clamped()
    {
        Assert.AreEqual(0.5f, TimerBarRules.Fraction(5f, 10f), Eps);
        Assert.AreEqual(1f, TimerBarRules.Fraction(12f, 10f), Eps);
        Assert.AreEqual(0f, TimerBarRules.Fraction(-1f, 10f), Eps);
        Assert.AreEqual(0f, TimerBarRules.Fraction(5f, 0f), Eps);
    }

    [Test]
    public void TimerColor_GoesGreenYellowRed()
    {
        Color g = Color.green, y = Color.yellow, r = Color.red;
        Assert.AreEqual(g, TimerBarRules.ColorFor(1f, g, y, r, 0.5f, 0.25f));
        Assert.AreEqual(y, TimerBarRules.ColorFor(0.5f, g, y, r, 0.5f, 0.25f));
        Assert.AreEqual(r, TimerBarRules.ColorFor(0.25f, g, y, r, 0.5f, 0.25f));
        Assert.AreEqual(r, TimerBarRules.ColorFor(0f, g, y, r, 0.5f, 0.25f));

        Color between = TimerBarRules.ColorFor(0.375f, g, y, r, 0.5f, 0.25f);
        Assert.AreEqual(Color.Lerp(r, y, 0.5f), between);
    }

    [Test]
    public void TimerShake_OnlyInWarning_AndGrowsAsTimeRunsOut()
    {
        Assert.AreEqual(0f, TimerBarRules.ShakeAmplitude(5f, 3f, 2f), Eps);
        Assert.AreEqual(0f, TimerBarRules.ShakeAmplitude(0f, 3f, 2f), Eps);
        float early = TimerBarRules.ShakeAmplitude(2.9f, 3f, 2f);
        float late = TimerBarRules.ShakeAmplitude(0.2f, 3f, 2f);
        Assert.Greater(early, 0f);
        Assert.Greater(late, early);
        Assert.LessOrEqual(late, 2f);
    }

    // ---- Kutu hali ve yazma efektleri ----

    [Test]
    public void BoxMode_FollowsPhaseAndWriter()
    {
        Assert.AreEqual(TypingBoxMode.Rolling, TypingFxMath.ModeFor(TurnPhase.Dice, true));
        Assert.AreEqual(TypingBoxMode.Rolling, TypingFxMath.ModeFor(TurnPhase.Dice, false));
        Assert.AreEqual(TypingBoxMode.Typing, TypingFxMath.ModeFor(TurnPhase.Typing, true));
        Assert.AreEqual(TypingBoxMode.Watching, TypingFxMath.ModeFor(TurnPhase.Typing, false));
        Assert.AreEqual(TypingBoxMode.Hidden, TypingFxMath.ModeFor(TurnPhase.Result, true));
        Assert.AreEqual(TypingBoxMode.Hidden, TypingFxMath.ModeFor(TurnPhase.NotStarted, false));
        Assert.AreEqual(TypingBoxMode.Hidden, TypingFxMath.ModeFor(TurnPhase.MatchOver, true));
    }

    [Test]
    public void KeyPitch_StaysInRange_AndIsStablePerLetter()
    {
        var seen = new HashSet<float>();
        foreach (char c in "abcçdefgğhıijklmnoöprsştuüvyz")
        {
            float pitch = TypingFxMath.KeyPitch(c, 0.1f, 5);
            Assert.That(pitch, Is.InRange(0.9f - Eps, 1.1f + Eps));
            Assert.AreEqual(pitch, TypingFxMath.KeyPitch(c, 0.1f, 5));
            seen.Add(pitch);
        }
        Assert.Greater(seen.Count, 2, "Harfler farklı perdelerde çalmalı");
        Assert.AreEqual(1f, TypingFxMath.KeyPitch('a', 0f, 5));
        Assert.AreEqual(1f, TypingFxMath.KeyPitch('a', 0.1f, 1));
    }

    [Test]
    public void Dots_CycleFromZeroToMax()
    {
        Assert.AreEqual(0, TypingFxMath.DotCount(0f, 0.5f, 3));
        Assert.AreEqual(1, TypingFxMath.DotCount(0.6f, 0.5f, 3));
        Assert.AreEqual(3, TypingFxMath.DotCount(1.6f, 0.5f, 3));
        Assert.AreEqual(0, TypingFxMath.DotCount(2.1f, 0.5f, 3));
        Assert.AreEqual(0, TypingFxMath.DotCount(5f, 0.5f, 0));
    }

    [Test]
    public void DeletedLetter_FallsFasterAndFadesOut()
    {
        Vector2 start = TypingFxMath.Fall(0f, 0.5f, 100f);
        Vector2 mid = TypingFxMath.Fall(0.25f, 0.5f, 100f);
        Vector2 late = TypingFxMath.Fall(0.45f, 0.5f, 100f);
        Assert.AreEqual(0f, start.x, Eps);
        Assert.AreEqual(1f, start.y, Eps);
        Assert.Greater(late.x - mid.x, mid.x - start.x, "Yerçekimiyle hızlanır");
        Assert.Less(late.y, mid.y);
        Assert.AreEqual(0f, TypingFxMath.Fall(0.5f, 0.5f, 100f).y, Eps);
    }

    [Test]
    public void RejectScatter_SpreadsOutAndComesBack()
    {
        Assert.AreEqual(Vector2.zero, TypingFxMath.Scatter(0, 5, -0.1f, 0.5f, 10f));
        Assert.AreEqual(Vector2.zero, TypingFxMath.Scatter(0, 5, 0.5f, 0.5f, 10f));
        Vector2 first = TypingFxMath.Scatter(0, 5, 0.15f, 0.5f, 10f);
        Vector2 last = TypingFxMath.Scatter(4, 5, 0.15f, 0.5f, 10f);
        Assert.Less(first.x, 0f, "Sol harf sola");
        Assert.Greater(last.x, 0f, "Sağ harf sağa");
        Assert.LessOrEqual(first.magnitude, 10f * 1.5f);
        Assert.Less(TypingFxMath.Scatter(0, 5, 0.49f, 0.5f, 10f).magnitude, first.magnitude, "Sonunda yerine döner");
    }

    [Test]
    public void SplitLetters_KeepsSurrogatePairsTogether()
    {
        CollectionAssert.AreEqual(new[] { "z", "o", "m" }, TypingFxMath.SplitLetters("zom"));
        CollectionAssert.AreEqual(new[] { "a", "\U0001F600", "b" }, TypingFxMath.SplitLetters("a\U0001F600b"));
        Assert.AreEqual(0, TypingFxMath.SplitLetters(null).Count);
        Assert.AreEqual(0, TypingFxMath.SplitLetters(string.Empty).Count);
    }

    // ---- Bezier yay ----

    [Test]
    public void ArcPath_StartsAndEndsAtEndpoints()
    {
        Vector2 a = new Vector2(0f, 0f), b = new Vector2(10f, 0f);
        Vector2 c = ArcPath.Control(a, b, 0.3f, 1f);
        Assert.AreEqual(a, ArcPath.Point(a, c, b, 0f));
        Assert.AreEqual(b, ArcPath.Point(a, c, b, 1f));
        Assert.AreEqual(b, ArcPath.Point(a, c, b, 2f), "t sıkıştırılır");
    }

    [Test]
    public void ArcPath_ControlBendsPerpendicular_SideFlips()
    {
        Vector2 a = new Vector2(0f, 0f), b = new Vector2(10f, 0f);
        Vector2 left = ArcPath.Control(a, b, 0.3f, 1f);
        Vector2 right = ArcPath.Control(a, b, 0.3f, -1f);
        Assert.AreEqual(5f, left.x, Eps);
        Assert.AreEqual(3f, Mathf.Abs(left.y), Eps, "Bükülme yol uzunluğunun katı");
        Assert.AreEqual(-left.y, right.y, Eps);
        Assert.AreEqual(new Vector2(5f, 0f), ArcPath.Control(a, b, 0f, 1f));
        Assert.AreEqual(a, ArcPath.Control(a, a, 0.3f, 1f), "Sıfır uzunlukta orta nokta");

        Vector2 mid = ArcPath.Point(a, left, b, 0.5f);
        Assert.AreEqual(left.y * 0.5f, mid.y, Eps, "Eğrinin tepesi kontrolün yarısı kadar bükülür");
        Assert.AreEqual(1f, ArcPath.SideFor(0));
        Assert.AreEqual(-1f, ArcPath.SideFor(1));
    }

    [Test]
    public void ArcPath_TangentPointsTowardControlAtStart()
    {
        Vector2 a = Vector2.zero, c = new Vector2(0f, 5f), b = new Vector2(10f, 0f);
        Vector2 t0 = ArcPath.Tangent(a, c, b, 0f);
        Assert.AreEqual(0f, t0.x, Eps);
        Assert.Greater(t0.y, 0f);
    }

    // ---- Büyü zaman çizelgesi ----

    [Test]
    public void Plan_WithoutTarget_LettersLeaveInStaggerAndWait()
    {
        var plan = new WordSpellPlan(4, 10f, Timings());
        Assert.AreEqual(10f, plan.LeaveTime(0), Eps);
        Assert.AreEqual(10.15f, plan.LeaveTime(3), Eps);
        Assert.IsTrue(float.IsPositiveInfinity(plan.DepartTime(0)));
        Assert.IsFalse(plan.IsFlying(0, 11f));
        Assert.AreEqual(0f, plan.GrowProgress(11f));
        Assert.IsFalse(plan.IsDone(11f));
        Assert.IsTrue(plan.HasExpired(14f), "Hedef gelmezse söner");
        Assert.IsTrue(plan.IsDone(14f));
    }

    [Test]
    public void Plan_TargetKnown_LastLetterArrivesWithinMax()
    {
        foreach (int letters in new[] { 1, 3, 8, 30 })
        {
            var plan = new WordSpellPlan(letters, 0f, Timings(0.5f));
            plan.SetTarget(1.2f); // Hedef sonradan belli oldu (ör. fareyle hedef seçildi)
            Assert.LessOrEqual(plan.LastArrival, 1.2f + 0.5f + Eps, letters + " harf");
            Assert.GreaterOrEqual(plan.FirstArrival, 1.2f);
            for (int i = 1; i < letters; i++)
            {
                Assert.GreaterOrEqual(plan.DepartTime(i), plan.DepartTime(i - 1), "Sırayla kalkar");
                Assert.GreaterOrEqual(plan.ArriveTime(i), plan.ArriveTime(i - 1), "Sırayla varır");
            }
        }
    }

    [Test]
    public void Plan_UsesConfiguredStaggerWhenItFits()
    {
        var plan = new WordSpellPlan(3, 0f, Timings(2f));
        plan.SetTarget(0f);
        Assert.AreEqual(0.04f, plan.DepartTime(1) - plan.DepartTime(0), Eps);
        Assert.AreEqual(0.4f, plan.ArriveTime(0) - plan.DepartTime(0), Eps);
    }

    [Test]
    public void Plan_SecondTargetIsIgnored_AndTargetNotBeforeStart()
    {
        var plan = new WordSpellPlan(2, 5f, Timings());
        plan.SetTarget(4f);
        Assert.AreEqual(5f, plan.TargetTime.Value, Eps);
        float last = plan.LastArrival;
        plan.SetTarget(6f);
        Assert.AreEqual(last, plan.LastArrival, Eps);
    }

    [Test]
    public void Plan_GrowSyncsWithLastArrival()
    {
        var plan = new WordSpellPlan(5, 0f, Timings());
        plan.SetTarget(0.1f);
        Assert.AreEqual(0f, plan.GrowProgress(0.1f), Eps);
        Assert.AreEqual(1f, plan.GrowProgress(plan.LastArrival), Eps);
        Assert.AreEqual(1f, plan.GrowProgress(plan.LastArrival + 1f), Eps);
        float mid = plan.GrowProgress((0.1f + plan.LastArrival) * 0.5f);
        Assert.That(mid, Is.InRange(0.4f, 0.6f));
        Assert.IsTrue(plan.IsDone(plan.LastArrival));
        Assert.IsFalse(plan.HasExpired(100f), "Hedefi olan büyü sönmez, varır");
    }

    [Test]
    public void Plan_FlightProgressIsMonotonicFromZeroToOne()
    {
        var plan = new WordSpellPlan(1, 0f, Timings());
        plan.SetTarget(0f);
        float previous = -1f;
        for (int k = 0; k <= 20; k++)
        {
            float t = plan.DepartTime(0) + (plan.ArriveTime(0) - plan.DepartTime(0)) * k / 20f;
            float u = plan.FlightProgress(0, t);
            Assert.GreaterOrEqual(u, previous);
            previous = u;
        }
        Assert.AreEqual(1f, previous, Eps);
        Assert.IsTrue(plan.HasArrived(0, plan.ArriveTime(0)));
    }

    [Test]
    public void Plan_RiseEasesFromZeroToOne()
    {
        var plan = new WordSpellPlan(2, 0f, Timings());
        Assert.AreEqual(0f, plan.RiseProgress(1, 0f), Eps);
        Assert.AreEqual(1f, plan.RiseProgress(1, 1f), Eps);
        Assert.Greater(plan.RiseProgress(0, 0.1f), 0.5f, "Önce hızlı çıkar (ease-out)");
    }

    [Test]
    public void SpellTimings_FromConfig_CopiesValues()
    {
        var config = ScriptableObject.CreateInstance<GameConfig>();
        SpellTimings t = SpellTimings.From(config);
        Assert.AreEqual(config.spellMaxArriveSeconds, t.MaxArriveSeconds);
        Assert.AreEqual(config.spellHoverMaxSeconds, t.HoverMaxSeconds);
        Assert.LessOrEqual(config.spellMaxArriveSeconds, 0.5f, "Harfler en geç 0,5 sn'de varır");
        Assert.Greater(config.spellHoverMaxSeconds, config.targetSelectionSeconds, "Hedef seçilirken harfler sönmesin");
        Object.DestroyImmediate(config);
    }

    // ---- Kurallar ----

    [Test]
    public void SpellKind_ComesFromScope()
    {
        Assert.AreEqual(SpellTargetKind.Creature, WordSpellRules.KindFor(null));
        Assert.AreEqual(SpellTargetKind.Creature, WordSpellRules.KindFor(new WordEntry { category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget }));
        Assert.AreEqual(SpellTargetKind.Global, WordSpellRules.KindFor(new WordEntry { category = WordEntry.CategoryThreat, scope = WordEntry.ScopeGlobal }));
        Assert.AreEqual(SpellTargetKind.Self, WordSpellRules.KindFor(new WordEntry { category = WordEntry.CategoryDefense, scope = WordEntry.ScopeSelf }));
    }

    [Test]
    public void SpellMatch_TreatsNullAndEmptyAlike()
    {
        Assert.IsTrue(WordSpellRules.Matches("zombi", "zombi"));
        Assert.IsFalse(WordSpellRules.Matches("zombi", "kurt"));
        Assert.IsTrue(WordSpellRules.Matches(null, string.Empty));
        Assert.IsFalse(WordSpellRules.Matches(null, "kurt"));
    }

    [Test]
    public void Poof_SkipsProjectilesStrikesAndTinyThings()
    {
        Assert.IsTrue(WordSpellRules.ShouldPoof(false, false, 0.5f, 0.2f));
        Assert.IsFalse(WordSpellRules.ShouldPoof(true, false, 0.5f, 0.2f));
        Assert.IsFalse(WordSpellRules.ShouldPoof(false, true, 0.5f, 0.2f));
        Assert.IsFalse(WordSpellRules.ShouldPoof(false, false, 0.1f, 0.2f));
    }

    // ---- Ekran hesapları ----

    [Test]
    public void ClampToEdge_KeepsInsidePoints_MovesOutsideOntoEdge()
    {
        Vector2 screen = new Vector2(1000f, 500f);
        Assert.AreEqual(new Vector2(300f, 200f), SpellScreenMath.ClampToEdge(new Vector2(300f, 200f), screen, 10f));

        Vector2 right = SpellScreenMath.ClampToEdge(new Vector2(3000f, 250f), screen, 10f);
        Assert.AreEqual(990f, right.x, 0.01f);
        Assert.AreEqual(250f, right.y, 0.01f);

        Vector2 corner = SpellScreenMath.ClampToEdge(new Vector2(-5000f, -5000f), screen, 10f);
        Assert.IsTrue(SpellScreenMath.IsInside(corner, screen, 9.9f));
        Assert.IsTrue(Mathf.Abs(corner.x - 10f) < 0.01f || Mathf.Abs(corner.y - 10f) < 0.01f, "Kenarda durur");
    }

    [Test]
    public void EdgeMarkers_AreOnTheBorderAndFaceTheCenter()
    {
        var positions = new List<Vector2>();
        var angles = new List<float>();
        Vector2 screen = new Vector2(800f, 600f);
        SpellScreenMath.EdgeMarkers(8, screen, 20f, positions, angles);
        Assert.AreEqual(8, positions.Count);
        Assert.AreEqual(8, angles.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2 p = positions[i];
            bool onEdge = Mathf.Abs(p.x - 20f) < 0.01f || Mathf.Abs(p.x - 780f) < 0.01f
                          || Mathf.Abs(p.y - 20f) < 0.01f || Mathf.Abs(p.y - 580f) < 0.01f;
            Assert.IsTrue(onEdge, "Kenarda: " + p);
            Vector2 facing = new Vector2(Mathf.Cos(angles[i] * Mathf.Deg2Rad), Mathf.Sin(angles[i] * Mathf.Deg2Rad));
            Assert.Greater(Vector2.Dot(facing, (screen * 0.5f - p).normalized), 0.99f);
        }
        SpellScreenMath.EdgeMarkers(0, screen, 20f, positions, angles);
        Assert.AreEqual(0, positions.Count);
    }

    // ---- Süs zar ----

    [Test]
    public void LandedFace_IsOneToSix_StableAndVaried()
    {
        var faces = new HashSet<int>();
        for (int turn = 0; turn < 40; turn++)
            for (int side = 0; side < 2; side++)
            {
                int face = DieFaces.LandedFace(turn, side);
                Assert.That(face, Is.InRange(1, 6));
                Assert.AreEqual(face, DieFaces.LandedFace(turn, side));
                faces.Add(face);
            }
        Assert.AreEqual(6, faces.Count, "Bütün yüzler çıkabilmeli");
    }
}
