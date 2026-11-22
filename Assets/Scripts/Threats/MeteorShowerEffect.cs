using UnityEngine;

/// <summary>
/// Meteor yağmuru: tehdidin süresi boyunca ("stats.duration") kelimenin "stats.count" kadar meteoru eşit aralıklarla düşürür.
/// Her meteor, yumruk/bomba gibi bir alan vuruşudur: önce gölge görünür (uyarı), sonra alandakilere hasar verir.
/// Bir kısmı arenanın rastgele yerine, bir kısmı rastgele bir oyuncunun yakınına düşer (GameConfig.meteorAimedChance).
/// Haritada çözüm alanı yoktur; çözüm gölgeleri görüp kaçmaktır. Düz C# sınıfı.
/// </summary>
public class MeteorShowerEffect : IThreatEffect
{
    const int MaxMeteors = 200;

    ThreatContext context;
    int total;
    int spawned;
    float interval;
    float elapsed;

    public bool IsFinished { get; private set; }

    public bool NeedsWarning => false; // Her meteor kendi gölge uyarısını verir

    /// <summary>Şimdiye kadar düşürülen meteor sayısı.</summary>
    public int Spawned => spawned;

    /// <summary>Toplam düşecek meteor sayısı.</summary>
    public int Total => total;

    public void Begin(ThreatContext context)
    {
        this.context = context;

        WordStats stats = context.Word?.stats;
        total = Mathf.Clamp(stats != null ? stats.count : 0, 1, MaxMeteors);

        float duration = stats != null && stats.duration > 0f ? stats.duration : 10f;
        interval = duration / total;
        elapsed = 0f;
        spawned = 0;

        SpawnDue(); // İlk meteor hemen
    }

    public void Tick(float deltaSeconds)
    {
        if (IsFinished) return;

        elapsed += Mathf.Max(0f, deltaSeconds);
        SpawnDue();
    }

    /// <summary>Zamanı gelmiş (aralığa göre) bütün meteorları düşürür; büyük zaman adımında geride kalanları yakalar.</summary>
    void SpawnDue()
    {
        while (spawned < total && elapsed >= spawned * interval)
        {
            SpawnOne();
            spawned++;
        }

        if (spawned >= total) IsFinished = true;
    }

    void SpawnOne()
    {
        if (context.Creatures == null) return;

        PlayerState nominal = PickAlivePlayer();
        if (nominal == null) return; // Kimse kalmadıysa düşecek yer de yok

        Vector2 position = PickPosition();
        CreatureModel meteor = CreatureFactory.CreateAttack(context.Word, nominal, position, context.Config, context.Arena);
        meteor.PlaceAt(position); // Etiketlerin yerleşim ayarı ne olursa olsun, meteor seçilen noktaya düşer

        // Kelimenin süresi tüm yağmurun süresidir; tek bir meteor uyarı + kısa bir iz kadar yaşar.
        meteor.LimitLife(meteor.StrikeWarningSeconds + context.Config.strikeLingerSeconds);

        context.Creatures.Spawn(meteor, string.Empty);
    }

    /// <summary>
    /// Düşme noktası: bazen rastgele bir oyuncunun yakını (tehdit olsun), çoğu zaman arenanın rastgele bir yeri.
    /// </summary>
    Vector2 PickPosition()
    {
        if (context.Random.NextDouble() < context.Config.meteorAimedChance)
        {
            PlayerState aimed = PickAlivePlayer();
            if (aimed != null && context.World.TryGetPosition(aimed.Id, out Vector2 playerPosition))
            {
                float offset = (float)context.Random.NextDouble() * context.Config.meteorAimSpread;
                return ArenaSpawn.PointBeside(playerPosition, offset, context.Random);
            }
        }

        return ArenaSpawn.PickPoint(context.Arena, Vector2.zero, 0f, context.Random, 0.5f);
    }

    PlayerState PickAlivePlayer()
    {
        int alive = 0;
        foreach (PlayerState player in context.World.Players)
            if (player.IsAlive) alive++;
        if (alive == 0) return null;

        int pick = context.Random.Next(alive);
        foreach (PlayerState player in context.World.Players)
        {
            if (!player.IsAlive) continue;
            if (pick-- == 0) return player;
        }
        return null;
    }
}
