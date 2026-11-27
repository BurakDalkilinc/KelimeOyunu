using System.Collections.Generic;
using UnityEngine;

/// <summary>Yaklaşan bir yaratık vuruşu: hedefin yeri, vuruş yönü, darbenin görüneceği an ve vuranın kendisi.</summary>
public struct IncomingStrike
{
    /// <summary>Vuruşun hedefi (saldırı başlarken hedefin yeri).</summary>
    public Vector2 Position;

    /// <summary>Yaratıktan hedefe doğru birim yön.</summary>
    public Vector2 Direction;

    /// <summary>Darbenin ekranda görüneceği an (Time.time, sn).</summary>
    public float ImpactTime;

    /// <summary>Dev (boss) vuruşu mu?</summary>
    public bool Giant;

    /// <summary>Vuran yaratığın görüntüsü (eleme vuruşunda donması uzatılır); yoksa null.</summary>
    public object Attacker;
}

/// <summary>
/// Yaratık saldırı animasyonlarının bildirdiği yaklaşan darbeler. Hasar kuralda saldırı olayı anında işlenir, darbe ise
/// hazırlıktan sonra görünür; vurulan karakter tepkisini (donma, parlama, sayı, ses) darbe anına denk getirmek için burada
/// kendi yakınındaki darbeyi arar. Düz C#, test edilebilir; zaman dışarıdan verilir.
/// </summary>
public class StrikeRegistry
{
    /// <summary>Oyunun ortak kaydı (tek kişilikte ve ağda her istemcide ayrı, yalnızca görüntü için).</summary>
    public static readonly StrikeRegistry Shared = new StrikeRegistry();

    /// <summary>Kayıtta en fazla bu kadar darbe tutulur (eskiler atılır).</summary>
    const int MaxEntries = 64;

    readonly List<IncomingStrike> strikes = new List<IncomingStrike>();

    public int Count => strikes.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Shared.Clear();

    public void Add(IncomingStrike strike)
    {
        strikes.Add(strike);
        if (strikes.Count > MaxEntries) strikes.RemoveAt(0);
    }

    public void Clear() => strikes.Clear();

    /// <summary>
    /// <paramref name="at"/>'a <paramref name="radius"/> içinde (devde <paramref name="giantRadiusMultiplier"/> katı) olan ve darbesi
    /// <paramref name="now"/>'dan en çok <paramref name="lookBack"/> önce, en çok <paramref name="maxWait"/> sonra olan en yakın darbeyi bulur.
    /// Bulunan darbe kayıttan çıkarılır (iki karakter aynı darbeyi almasın). Süresi geçmiş kayıtlar da atılır.
    /// </summary>
    public bool TryTake(Vector2 at, float now, float radius, float giantRadiusMultiplier, float lookBack, float maxWait,
                        out IncomingStrike found)
    {
        found = default;
        int best = -1;
        float bestDistance = float.MaxValue;

        for (int i = strikes.Count - 1; i >= 0; i--)
        {
            IncomingStrike strike = strikes[i];
            if (strike.ImpactTime < now - lookBack)
            {
                strikes.RemoveAt(i);
                if (best > i) best--;
                continue;
            }
            if (strike.ImpactTime > now + maxWait) continue;

            float reach = radius * (strike.Giant ? Mathf.Max(1f, giantRadiusMultiplier) : 1f);
            float distance = Vector2.Distance(strike.Position, at);
            if (distance > reach || distance >= bestDistance) continue;

            best = i;
            bestDistance = distance;
        }

        if (best < 0) return false;
        found = strikes[best];
        strikes.RemoveAt(best);
        return true;
    }
}
