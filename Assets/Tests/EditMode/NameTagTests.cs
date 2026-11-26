using NUnit.Framework;
using UnityEngine;

/// <summary>İsim etiketi kuralları: görünürlük (görünmezlik/kılık, ölüm, hayalet, çatı altı), renk, kısaltma, varsayılan ad.</summary>
public class NameTagTests
{
    static NameTagInfo Alive(bool local = false) => new NameTagInfo
    {
        Name = "Bot 3",
        IsLocal = local,
        IsAlive = true,
        BodyShown = true,
        IsStealthed = false,
    };

    [Test]
    public void Visible_ForANormalCharacter_OwnAndOthers()
    {
        Assert.IsTrue(NameTagRules.IsVisible(Alive(), false));
        Assert.IsTrue(NameTagRules.IsVisible(Alive(local: true), false));
    }

    [Test]
    public void Stealth_HidesOthersName_OwnStaysFaint()
    {
        NameTagInfo other = Alive();
        other.IsStealthed = true;
        Assert.IsFalse(NameTagRules.IsVisible(other, false), "kılık/görünmezlik başkasının adını gizler");

        NameTagInfo own = Alive(local: true);
        own.IsStealthed = true;
        Assert.IsTrue(NameTagRules.IsVisible(own, false), "kendi adını görmeye devam eder");
        Color color = NameTagRules.ColorFor(own, Color.yellow, Color.white, 0.4f);
        Assert.AreEqual(0.4f, color.a, 0.001f, "kendi görünmezliğinde ad soluk");
    }

    [Test]
    public void Dead_OthersNameDisappears_OwnGhostIsFaint()
    {
        NameTagInfo deadOther = Alive();
        deadOther.IsAlive = false;
        Assert.IsFalse(NameTagRules.IsVisible(deadOther, false), "ölünce isim kalkar");

        NameTagInfo hiddenGhost = Alive();
        hiddenGhost.IsAlive = false;
        hiddenGhost.BodyShown = false;
        Assert.IsFalse(NameTagRules.IsVisible(hiddenGhost, false));

        NameTagInfo ownGhost = Alive(local: true);
        ownGhost.IsAlive = false;
        Assert.IsTrue(NameTagRules.IsVisible(ownGhost, false), "kendi hayaletin adı görünür");
        Assert.AreEqual(0.5f, NameTagRules.ColorFor(ownGhost, Color.yellow, Color.white, 0.5f).a, 0.001f, "hayalet soluk");
    }

    [Test]
    public void BodyNotShown_HidesName()
    {
        NameTagInfo info = Alive();
        info.BodyShown = false;
        Assert.IsFalse(NameTagRules.IsVisible(info, false));
    }

    [Test]
    public void UnderRoof_HidesName_DoorAndOpenRoofDoNot()
    {
        Assert.IsFalse(NameTagRules.IsVisible(Alive(), true), "çatı altındaki karakterin adı gizli");

        // Kural zinciri: çatı altı haritadan hesaplanır (kapı karesi çatısız, izleyen içerideyse çatı yok).
        var map = new TerrainMap(20, 12);
        map.AddHouse(new House(5, 5, 5, 4, 6, true)); // Kapı üst duvarda: (6,8) ve (7,8)
        Vector2 viewerOutside = map.CellCenter(1, 1);

        bool inside = RoofRules.IsUnderVisibleRoof(map, map.CellCenter(7, 6), viewerOutside, null);
        bool atDoor = RoofRules.IsUnderVisibleRoof(map, map.CellCenter(7, 8), viewerOutside, null);
        bool viewerInside = RoofRules.IsUnderVisibleRoof(map, map.CellCenter(7, 6), map.CellCenter(6, 6), null);

        Assert.IsFalse(NameTagRules.IsVisible(Alive(), inside));
        Assert.IsTrue(NameTagRules.IsVisible(Alive(), atDoor), "kapıda duran görünür");
        Assert.IsTrue(NameTagRules.IsVisible(Alive(), viewerInside), "aynı evdeysen içerideki görünür");
    }

    [Test]
    public void Color_OwnIsOwnColor_OthersOtherColor()
    {
        Assert.AreEqual(Color.yellow, NameTagRules.ColorFor(Alive(local: true), Color.yellow, Color.white, 0.4f));
        Assert.AreEqual(Color.white, NameTagRules.ColorFor(Alive(), Color.yellow, Color.white, 0.4f));
    }

    [Test]
    public void EmptyName_IsHidden()
    {
        NameTagInfo info = Alive();
        info.Name = "";
        Assert.IsFalse(NameTagRules.IsVisible(info, false));
    }

    [Test]
    public void Shorten_CutsLongNames()
    {
        Assert.AreEqual("Bot 3", NameTagRules.Shorten("  Bot 3 ", 14));
        Assert.AreEqual("Çokuzunbi.", NameTagRules.Shorten("Çokuzunbirisim", 10));
        Assert.AreEqual(10, NameTagRules.Shorten("Çokuzunbirisim", 10).Length);
        Assert.AreEqual("", NameTagRules.Shorten(null, 5));
    }

    [Test]
    public void DefaultName_BotsAreNumberedFromOne()
    {
        Assert.AreEqual("Bot 1", NameTagRules.DefaultName(100, true, 100));
        Assert.AreEqual("Bot 4", NameTagRules.DefaultName(103, true, 100));
        Assert.AreEqual("Oyuncu 2", NameTagRules.DefaultName(2, false, 100));
    }
}
