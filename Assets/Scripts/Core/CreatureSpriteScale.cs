using UnityEngine;

/// <summary>
/// Koşan yaratığın sprite büyüklüğü: normal yaratıklar 1 kat (16 piksel = 1 birim); boss gibi büyük yaratıklar
/// yarıçaplarına göre tam sayı katı büyür (piksel sanat bozulmasın). Düz hesap, test edilebilir.
/// </summary>
public static class CreatureSpriteScale
{
    public static float ForRadius(float radius) => IsGiant(radius) ? Mathf.Max(1f, Mathf.Round(radius * 2f)) : 1f;

    /// <summary>Dev yaratık (boss) mı? Devler çatıların (15) da üstünde çizilir, evlerin arkasında kaybolmaz.</summary>
    public static bool IsGiant(float radius) => radius >= 1f;

    /// <summary>Koşan yaratığın çizim sırası: normalde 8 (karakterlerin altında), dev ise 16 (çatıların üstünde).</summary>
    /// <summary>
    /// Karakterler, yaratıklar (devler dahil), araçlar ve çatılar aynı sıradadır; aralarındaki öne/arkaya kararını ekrandaki
    /// yükseklik verir (Renderer2D: Custom Axis 0,1,0 — aşağıdaki önde). Böylece evin önündeki çatının önünde, içindeki ve
    /// arkasındaki çatının arkasında kalır.
    /// </summary>
    public const int EntityOrder = 10;

    public static int SortingOrder(float radius) => EntityOrder;
}
