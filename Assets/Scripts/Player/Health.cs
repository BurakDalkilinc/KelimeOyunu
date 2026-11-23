using System;

/// <summary>
/// Can hesabı. Unity'den bağımsız düz C# sınıfı olduğu için kolayca test edilir.
/// Elenme kararı da buradan çıkar (can 0 olunca <see cref="IsDead"/> true olur).
/// </summary>
public class Health
{
    public int Max { get; }
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    /// <summary>Can değişince (yeni can, en yüksek can) ile çağrılır.</summary>
    public event Action<int, int> Changed;

    /// <summary>Can 0'a düştüğü anda bir kez çağrılır.</summary>
    public event Action Died;

    public Health(int max)
    {
        if (max < 1) throw new ArgumentOutOfRangeException(nameof(max), "En az 1 can gerekir.");
        Max = max;
        Current = max;
    }

    /// <summary>Hasar verir. Ölü oyuncuya hasar işlemez. Gerçekten düşen canı döndürür.</summary>
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return 0;

        int before = Current;
        Current = Math.Max(0, Current - amount);
        Changed?.Invoke(Current, Max);
        if (IsDead) Died?.Invoke();
        return before - Current;
    }

    /// <summary>
    /// Elenmiş oyuncuyu yeniden doğurur: canı <paramref name="amount"/> (en az 1, en fazla <see cref="Max"/>) olur.
    /// Zaten hayattaysa bir şey yapmaz. Gerçekten verilen canı döndürür (yaşıyorsa 0).
    /// </summary>
    public int Revive(int amount)
    {
        if (!IsDead) return 0;

        Current = Math.Min(Max, Math.Max(1, amount));
        Changed?.Invoke(Current, Max);
        return Current;
    }

    /// <summary>Can verir (en fazla <see cref="Max"/>). Ölü oyuncu iyileşmez. Gerçekten artan canı döndürür.</summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead) return 0;

        int before = Current;
        Current = Math.Min(Max, Current + amount);
        if (Current != before) Changed?.Invoke(Current, Max);
        return Current - before;
    }
}
