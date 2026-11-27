using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Arayüz cilasının düz hesapları: yumuşatma eğrileri (Easing), punch/sarsıntı/kayma/stagger (UiTween), düğme yayı,
/// sayaç tiki, geçiş deseni, uçuşan harfler, konfeti, zar yüzleri ve maç sonu satırları.
/// </summary>
public class UiPolishTests
{
    const float Eps = 1e-4f;

    // ---- Easing ----

    [Test]
    public void Easing_AllCurves_StartAtZeroAndEndAtOne()
    {
        foreach (System.Func<float, float> curve in new System.Func<float, float>[]
                 { Easing.Linear, Easing.InCubic, Easing.OutCubic, t => Easing.OutBack(t), Easing.OutElastic })
        {
            Assert.AreEqual(0f, curve(0f), Eps);
            Assert.AreEqual(1f, curve(1f), Eps);
        }
    }

    [Test]
    public void Easing_ClampsOutsideZeroToOne()
    {
        Assert.AreEqual(0f, Easing.OutCubic(-2f), Eps);
        Assert.AreEqual(1f, Easing.OutCubic(3f), Eps);
        Assert.AreEqual(1f, Easing.OutBack(5f), Eps);
        Assert.AreEqual(1f, Easing.OutElastic(2f), Eps);
    }

    [Test]
    public void Easing_OutCubic_IsFastFirst_InCubic_IsSlowFirst()
    {
        Assert.Greater(Easing.OutCubic(0.25f), 0.25f);
        Assert.Less(Easing.InCubic(0.25f), 0.25f);
    }

    [Test]
    public void Easing_OutBack_OvershootsBeforeSettling()
    {
        float max = 0f;
        for (int i = 0; i <= 100; i++) max = Mathf.Max(max, Easing.OutBack(i / 100f));
        Assert.Greater(max, 1.05f);
        // Daha büyük taşma değeri daha çok aşar.
        Assert.Greater(Easing.OutBack(0.7f, 3f), Easing.OutBack(0.7f, 1f));
    }

    [Test]
    public void Easing_OutElastic_WobblesAroundOne()
    {
        bool above = false, below = false;
        for (int i = 5; i < 100; i++)
        {
            float v = Easing.OutElastic(i / 100f);
            if (v > 1.001f) above = true;
            if (v < 0.999f) below = true;
        }
        Assert.IsTrue(above && below);
    }

    // ---- UiTween ----

    [Test]
    public void PunchIn_StartsSmall_OvershootsThenRestsAtOne()
    {
        Assert.AreEqual(0.2f, UiTween.PunchIn(0f, 0.4f, 0.2f, 2f), Eps);
        float max = 0f;
        for (int i = 0; i <= 40; i++) max = Mathf.Max(max, UiTween.PunchIn(i * 0.01f, 0.4f, 0.2f, 2f));
        Assert.Greater(max, 1f);
        Assert.AreEqual(1f, UiTween.PunchIn(0.4f, 0.4f, 0.2f, 2f), Eps);
        Assert.AreEqual(1f, UiTween.PunchIn(9f, 0.4f, 0.2f, 2f), Eps);
        Assert.AreEqual(1f, UiTween.PunchIn(0f, 0f, 0.2f, 2f), Eps, "Süre 0 ise hemen tam boy");
    }

    [Test]
    public void Bump_StartsBig_EndsAtOne_AndIsOneOutsideItsTime()
    {
        Assert.AreEqual(1.5f, UiTween.Bump(0f, 0.5f, 0.5f), Eps);
        Assert.AreEqual(1f, UiTween.Bump(0.5f, 0.5f, 0.5f), Eps);
        Assert.AreEqual(1f, UiTween.Bump(-1f, 0.5f, 0.5f), Eps);
    }

    [Test]
    public void Shake_FadesOut_AndIsZeroAfterwards()
    {
        float early = 0f, late = 0f;
        for (int i = 0; i < 10; i++) early = Mathf.Max(early, Mathf.Abs(UiTween.Shake(i * 0.01f, 1f, 20f, 10f)));
        for (int i = 80; i < 100; i++) late = Mathf.Max(late, Mathf.Abs(UiTween.Shake(i * 0.01f, 1f, 20f, 10f)));
        Assert.Greater(early, late);
        Assert.LessOrEqual(early, 20f + Eps);
        Assert.AreEqual(0f, UiTween.Shake(1f, 1f, 20f, 10f), Eps);
        Assert.AreEqual(0f, UiTween.Shake(-0.1f, 1f, 20f, 10f), Eps);
        Assert.AreEqual(UiTween.Shake(0.13f, 1f, 20f, 10f), UiTween.Shake(0.13f, 1f, 20f, 10f), "Rastgele değil");
    }

    [Test]
    public void SlideOut_GoesFromZeroToDistance_AndFadeMatches()
    {
        Assert.AreEqual(0f, UiTween.SlideOut(0f, 0.3f, 50f), Eps);
        Assert.AreEqual(50f, UiTween.SlideOut(0.3f, 0.3f, 50f), Eps);
        Assert.Less(UiTween.SlideOut(0.15f, 0.3f, 50f), 25f, "Gittikçe hızlanır");
        Assert.AreEqual(1f, UiTween.Fade(0f, 0.3f, false), Eps);
        Assert.AreEqual(0f, UiTween.Fade(0.3f, 0.3f, false), Eps);
        Assert.AreEqual(1f, UiTween.Fade(0.3f, 0.3f, true), Eps);
    }

    [Test]
    public void SecondTick_PeaksRightAfterEachWholeSecond()
    {
        Assert.AreEqual(1f, UiTween.SecondTick(2.9999f), 0.01f);
        Assert.Less(UiTween.SecondTick(2.1f), 0.1f);
        Assert.AreEqual(0f, UiTween.SecondTick(0f), Eps);
        Assert.GreaterOrEqual(UiTween.Pulse(0.25f, 1f), 0f);
        Assert.AreEqual(1f, UiTween.Pulse(0.5f, 1f), Eps);
    }

    [Test]
    public void Stagger_RowsStartOneAfterAnother()
    {
        const float delay = 0.2f, fade = 0.25f;
        Assert.AreEqual(0, UiTween.StaggerStarted(-0.1f, 4, delay));
        Assert.AreEqual(1, UiTween.StaggerStarted(0f, 4, delay));
        Assert.AreEqual(2, UiTween.StaggerStarted(0.25f, 4, delay));
        Assert.AreEqual(4, UiTween.StaggerStarted(10f, 4, delay), "Satır sayısını aşmaz");
        Assert.AreEqual(4, UiTween.StaggerStarted(0f, 4, 0f), "Aralık 0 ise hepsi birden");

        Assert.AreEqual(0f, UiTween.StaggerProgress(0.1f, 1, delay, fade), Eps, "2. satır henüz başlamadı");
        Assert.AreEqual(0.4f, UiTween.StaggerProgress(0.3f, 1, delay, fade), Eps);
        Assert.AreEqual(1f, UiTween.StaggerProgress(1f, 1, delay, fade), Eps);
        Assert.AreEqual(1f, UiTween.StaggerProgress(0.2f, 1, delay, 0f), Eps);
    }

    // ---- Yay ve düğme ----

    [Test]
    public void UiSpring_ReachesTarget_AndStaysStableWithHugeFrames()
    {
        var spring = new UiSpring(1f);
        for (int i = 0; i < 120; i++) spring.Step(1.2f, 1f / 60f, 420f, 20f);
        Assert.AreEqual(1.2f, spring.Value, 0.01f);

        var lagged = new UiSpring(1f);
        lagged.Step(1.2f, 5f, 420f, 20f); // Takılan kare: patlamamalı
        Assert.IsFalse(float.IsNaN(lagged.Value));
        Assert.Less(Mathf.Abs(lagged.Value - 1.2f), 0.5f);

        lagged.Snap(3f);
        Assert.AreEqual(3f, lagged.Value);
        Assert.AreEqual(0f, lagged.Velocity);
    }

    [Test]
    public void UiSpring_WithLowDamping_Overshoots()
    {
        var spring = new UiSpring(1f);
        float max = 1f;
        for (int i = 0; i < 60; i++) max = Mathf.Max(max, spring.Step(1.1f, 1f / 60f, 420f, 8f));
        Assert.Greater(max, 1.1f);
    }

    [Test]
    public void MenuButtonMotion_GrowsOnHover_ShrinksOnPress_ReportsHoverEnterOnce()
    {
        var motion = new MenuButtonMotion();
        Assert.IsTrue(motion.Update(true, false, 1f / 60f, 1.1f, 0.9f, 420f, 20f), "İlk üzerine gelme");
        for (int i = 0; i < 60; i++) Assert.IsFalse(motion.Update(true, false, 1f / 60f, 1.1f, 0.9f, 420f, 20f));
        Assert.AreEqual(1.1f, motion.Scale, 0.01f);

        for (int i = 0; i < 60; i++) motion.Update(true, true, 1f / 60f, 1.1f, 0.9f, 420f, 20f);
        Assert.AreEqual(0.9f, motion.Scale, 0.01f);

        for (int i = 0; i < 60; i++) motion.Update(false, false, 1f / 60f, 1.1f, 0.9f, 420f, 20f);
        Assert.AreEqual(1f, motion.Scale, 0.01f);
        Assert.IsTrue(motion.Update(true, false, 1f / 60f, 1.1f, 0.9f, 420f, 20f), "Çıkıp yeniden girince tekrar haber verir");
    }

    // ---- Sayaç ----

    [Test]
    public void CountdownTicker_TicksOncePerSecond_OnlyInTheLastSeconds()
    {
        var ticker = new CountdownTicker();
        int ticks = 0;
        var tickedAt = new List<int>();
        for (float left = 10f; left > 0f; left -= 0.05f)
        {
            if (ticker.Update(left, 3))
            {
                ticks++;
                tickedAt.Add(Mathf.CeilToInt(left));
            }
        }
        Assert.AreEqual(3, ticks);
        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, tickedAt);
    }

    [Test]
    public void CountdownTicker_StartingInsideTheWindow_TicksImmediately_AndResetWorks()
    {
        var ticker = new CountdownTicker();
        Assert.IsTrue(ticker.Update(2.5f, 3));
        Assert.IsFalse(ticker.Update(2.4f, 3));
        ticker.Reset();
        Assert.IsTrue(ticker.Update(2.3f, 3));
        Assert.IsFalse(CountdownTicker.IsWarning(0f, 3));
        Assert.IsFalse(CountdownTicker.IsWarning(3.01f, 3));
        Assert.IsTrue(CountdownTicker.IsWarning(3f, 3));
    }

    // ---- Geçiş deseni ----

    [Test]
    public void PixelDissolve_IsDeterministic_AndCoversMoreAsProgressGrows()
    {
        Assert.AreEqual(PixelDissolve.Threshold(5, 7), PixelDissolve.Threshold(5, 7));
        int quarter = 0, threeQuarters = 0, total = 0;
        for (int y = 0; y < 30; y++)
            for (int x = 0; x < 50; x++)
            {
                float t = PixelDissolve.Threshold(x, y);
                Assert.That(t, Is.InRange(0f, 1f));
                if (PixelDissolve.IsCovered(x, y, 0.25f)) quarter++;
                if (PixelDissolve.IsCovered(x, y, 0.75f)) threeQuarters++;
                Assert.IsFalse(PixelDissolve.IsCovered(x, y, 0f));
                Assert.IsTrue(PixelDissolve.IsCovered(x, y, 1f));
                total++;
            }
        Assert.Less(quarter, threeQuarters);
        Assert.AreEqual(0.25f, quarter / (float)total, 0.08f, "Eşikler aşağı yukarı düzgün dağılır");
    }

    // ---- Menü arka planı ----

    [Test]
    public void FloatingLetters_StayNearTheScreen_AndMoveUpOverTime()
    {
        for (int i = 0; i < 30; i++)
        {
            for (float time = 0f; time < 200f; time += 7.3f)
            {
                FloatingLetter letter = FloatingLetters.Get(i, time, 1920f, 1080f, 30f, 40f, 120f, 0.2f);
                Assert.That(letter.Y, Is.InRange(-letter.Size - 1f, 1080f + letter.Size + 1f));
                Assert.That(letter.Size, Is.InRange(40f, 120f));
                Assert.That(letter.Alpha, Is.InRange(0f, 0.2f + Eps));
            }
        }

        FloatingLetter a = FloatingLetters.Get(3, 10f, 1920f, 1080f, 30f, 40f, 120f, 0.2f);
        FloatingLetter b = FloatingLetters.Get(3, 10.1f, 1920f, 1080f, 30f, 40f, 120f, 0.2f);
        Assert.Less(b.Y, a.Y + 0.01f, "Kısa sürede yukarı (ekranda y azalır) gider");
        Assert.AreEqual(a.Letter, b.Letter);
    }

    // ---- Konfeti ----

    [Test]
    public void Confetti_FallsAndLeavesTheScreen()
    {
        var field = new ConfettiField();
        field.Burst(new System.Random(1), 50, 1920f, 300f, 900f, 5);
        Assert.AreEqual(50, field.Pieces.Count);
        foreach (ConfettiPiece piece in field.Pieces) Assert.That(piece.ColorIndex, Is.InRange(0, 4));

        float before = field.Pieces[0].Position.y;
        field.Step(0.1f, 700f, 0.9f, 1080f);
        Assert.AreEqual(50, field.Pieces.Count);
        Assert.Greater(field.Pieces[0].Position.y, before - 1f);

        for (int i = 0; i < 400 && field.Pieces.Count > 0; i++) field.Step(1f / 30f, 700f, 0.9f, 1080f);
        Assert.AreEqual(0, field.Pieces.Count, "Hepsi ekranın altından çıkar");
    }

    [Test]
    public void Confetti_SameSeed_SameBurst()
    {
        var a = new ConfettiField();
        var b = new ConfettiField();
        a.Burst(new System.Random(42), 10, 800f, 100f, 200f, 3);
        b.Burst(new System.Random(42), 10, 800f, 100f, 200f, 3);
        for (int i = 0; i < 10; i++) Assert.AreEqual(a.Pieces[i].Position, b.Pieces[i].Position);
    }

    // ---- Zar yüzleri ----

    [Test]
    public void DieFaces_HaveAsManyPipsAsTheirNumber_InsideTheFace()
    {
        for (int face = 1; face <= 6; face++)
        {
            Vector2Int[] pips = DieFaces.PipCells(face);
            Assert.AreEqual(face, pips.Length);
            var unique = new HashSet<Vector2Int>(pips);
            Assert.AreEqual(face, unique.Count);
            foreach (Vector2Int p in pips)
            {
                Assert.That(p.x, Is.InRange(1, DieFaces.Size - 2));
                Assert.That(p.y, Is.InRange(1, DieFaces.Size - 2));
            }
        }
        Assert.AreEqual(1, DieFaces.PipCells(7).Length, "Aralık dışı sayı 1-6'ya katlanır");
        Assert.AreEqual(6, DieFaces.PipCells(0).Length);
    }

    // ---- Maç sonu satırları ----

    [Test]
    public void StatRows_HaveNoticeTurnsPlayersAndWords_InOrder()
    {
        string before = Loc.Current;
        Loc.SetLanguage("tr");
        try
        {
            var words = new List<WordEntry> { new WordEntry { id = "kurt", display = "Kurt" } };
            List<string> rows = MatchSummary.GetStatRows("Deftere 1 yeni kelime eklendi!", 7, 8, words);
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("Deftere 1 yeni kelime eklendi!", rows[0]);
            Assert.AreEqual("Oynanan tur: 7", rows[1]);
            Assert.AreEqual("Oyuncu sayısı: 8", rows[2]);
            Assert.AreEqual(MatchSummary.GetWordsText(words), rows[3]);

            Assert.AreEqual(3, MatchSummary.GetStatRows("", 1, 2, null).Count, "Bildirim yoksa satırı da yok");
            Assert.AreEqual("KAZANDIN!", MatchSummary.GetTitleFor(new PlayerState(0, "Sen", 3), true));
            Assert.AreEqual("Kazanan: Bot 2", MatchSummary.GetTitleFor(new PlayerState(2, "Bot 2", 3), false));
        }
        finally
        {
            Loc.SetLanguage(before);
        }
    }

    [Test]
    public void NewUiKeys_ExistInBothLanguages()
    {
        string[] keys =
        {
            "menu.solo", "menu.multiplayer", "menu.book", "menu.quit", "menu.back", "menu.lan_host", "menu.lan_join",
            "menu.advanced_closed", "menu.advanced_open", "banner.ready", "banner.your_turn", "typing.hint_time",
            "match.you_won", "match.stat_turns", "match.stat_players", "match.play_again", "match.main_menu",
        };
        foreach (string language in new[] { "tr", "en" })
        {
            LocTable table = Loc.TableFor(language);
            foreach (string key in keys) Assert.IsTrue(table.TryGet(key, out string _), language + ": " + key);
        }
    }
}
