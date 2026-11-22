using UnityEngine;

/// <summary>Hedef seçme işleminin durumu.</summary>
public enum TargetSelectionState
{
    /// <summary>Oyuncu hâlâ seçiyor.</summary>
    Pending,

    /// <summary>Bir hedef seçildi.</summary>
    Selected,

    /// <summary>Süre doldu, hedef seçilmedi (rastgele bir rakip atanır).</summary>
    TimedOut,

    /// <summary>İptal edildi (yazan elendi, maç bitti...).</summary>
    Cancelled
}

/// <summary>
/// Saldırı kelimesini yazan insan oyuncunun hedef seçme işlemi: süre sayar ve seçilen hedefi doğrular.
/// Yazan kendini seçemez, elenmiş biri seçilemez. Düz C# sınıfı; fare ve ekran bağlantısı
/// <see cref="TargetSelector"/>'dadır.
/// </summary>
public class TargetSelection
{
    public PlayerState Writer { get; }
    public float TimeLeft { get; private set; }
    public TargetSelectionState State { get; private set; } = TargetSelectionState.Pending;

    /// <summary>Seçilen hedef; yalnızca <see cref="TargetSelectionState.Selected"/> durumunda dolu.</summary>
    public PlayerState Chosen { get; private set; }

    public bool IsPending => State == TargetSelectionState.Pending;

    public TargetSelection(PlayerState writer, float seconds)
    {
        Writer = writer ?? throw new System.ArgumentNullException(nameof(writer));
        TimeLeft = Mathf.Max(0f, seconds);
        if (TimeLeft <= 0f) State = TargetSelectionState.TimedOut;
    }

    /// <summary>Hedefi seçmeyi dener. Geçersizse (yazanın kendisi, elenmiş, null, işlem bitmiş) false döner.</summary>
    public bool TrySelect(PlayerState candidate)
    {
        if (!IsPending) return false;
        if (candidate == null || candidate == Writer || !candidate.IsAlive || candidate.Status.IsStealthed) return false;

        Chosen = candidate;
        State = TargetSelectionState.Selected;
        return true;
    }

    /// <summary>Zamanı ilerletir; süre dolunca durum <see cref="TargetSelectionState.TimedOut"/> olur.</summary>
    public void Tick(float deltaSeconds)
    {
        if (!IsPending) return;

        TimeLeft -= Mathf.Max(0f, deltaSeconds);
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            State = TargetSelectionState.TimedOut;
        }
    }

    /// <summary>İşlemi iptal eder. Zaten bitmişse bir şey yapmaz.</summary>
    public void Cancel()
    {
        if (IsPending) State = TargetSelectionState.Cancelled;
    }
}
