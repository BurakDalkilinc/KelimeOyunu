using System.Collections.Generic;

/// <summary>
/// İstemciden host'a giden mesajlar için gönderen başına hız sınırı (kova yöntemi): kısa süreli patlamaya (<c>capacity</c>)
/// izin verir, sonra saniyede <c>refillPerSecond</c> kadar hak yeniler. Mesaj seline karşı korur; normal oyun asla sınıra takılmaz.
/// Zamanı dışarıdan alır, düz C# sınıfıdır, test edilebilir.
/// </summary>
public class RpcRateLimiter
{
    sealed class Bucket
    {
        public float Tokens;
        public float LastTime;
    }

    readonly float capacity;
    readonly float refillPerSecond;
    readonly Dictionary<ulong, Bucket> buckets = new Dictionary<ulong, Bucket>();

    public RpcRateLimiter(float capacity, float refillPerSecond)
    {
        this.capacity = capacity < 1f ? 1f : capacity;
        this.refillPerSecond = refillPerSecond < 0f ? 0f : refillPerSecond;
    }

    /// <summary>Bu gönderenin mesajı şimdi kabul edilsin mi? Kabul edilirse bir hak harcanır.</summary>
    public bool Allow(ulong sender, float now)
    {
        if (!buckets.TryGetValue(sender, out Bucket bucket))
        {
            bucket = new Bucket { Tokens = capacity, LastTime = now };
            buckets[sender] = bucket;
        }

        float elapsed = now - bucket.LastTime;
        if (elapsed > 0f)
        {
            bucket.Tokens = System.Math.Min(capacity, bucket.Tokens + elapsed * refillPerSecond);
            bucket.LastTime = now;
        }

        if (bucket.Tokens < 1f) return false;

        bucket.Tokens -= 1f;
        return true;
    }

    /// <summary>Bağlantısı kopan gönderenin kaydını siler.</summary>
    public void Forget(ulong sender)
    {
        buckets.Remove(sender);
    }
}
