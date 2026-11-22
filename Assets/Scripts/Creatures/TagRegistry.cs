using System.Collections.Generic;

/// <summary>
/// Etiket → davranış tablosu. Yaratık doğarken kelimenin etiketleri buradan uygulanır.
/// Her GDD etiketi tam olarak şu üç gruptan birindedir:
///  - Uygulanmış: kendi <see cref="ITagBehaviour"/> sınıfı var.
///  - Etiket-yalnız: hız zaten kelimenin "stats.speed" değerinden geldiği için ayrıca bir şey yapmaz ("slow", "fast").
///  - Bekleyen: henüz yazılmadı (yol haritasındaki sonraki gruplar). Hiçbir şey yapmaz.
/// Yeni bir etiket yazılınca "bekleyen" listesinden çıkarılıp buraya eklenir; test bunu zorunlu kılar.
/// </summary>
public static class TagRegistry
{
    static readonly Dictionary<string, ITagBehaviour> Implemented = Build(
        new UndeadTag(),
        new PredatorTag(),
        new SwarmTag(),
        new InstantTag(),
        new KnockbackTag(),
        new SlowdownTag(),
        new AreaTag(),
        new ExplosiveTag(),
        new TrapTag());

    /// <summary>Hız zaten kelimenin değerinden geldiği için ek davranışı olmayan etiketler.</summary>
    public static readonly IReadOnlyCollection<string> LabelOnlyTags = new[] { "slow", "fast" };

    /// <summary>Henüz uygulanmamış etiketler. Sıradaki gruplarda tek tek uygulanacak.</summary>
    public static readonly IReadOnlyCollection<string> PendingTags = new[]
    {
        "flying", "pull", "armored",
        "water", "fire", "sky", "map_wide", "shield", "heal", "wall", "escape", "stealth"
    };

    public static IReadOnlyCollection<string> ImplementedTags => Implemented.Keys;

    /// <summary>Etiket bu üç gruptan birinde mi?</summary>
    public static bool IsKnown(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        return Implemented.ContainsKey(tag) || Contains(LabelOnlyTags, tag) || Contains(PendingTags, tag);
    }

    /// <summary>Kelimenin etiketlerini yaratığa uygular. Uygulanmamış ya da bilinmeyen etiket yok sayılır.</summary>
    public static void Apply(CreatureModel creature, IEnumerable<string> tags, SpawnContext context)
    {
        if (creature == null || tags == null) return;

        foreach (string tag in tags)
        {
            if (tag != null && Implemented.TryGetValue(tag, out ITagBehaviour behaviour))
                behaviour.Configure(creature, context);
        }
    }

    /// <summary>
    /// Kelimeden kaç yaratık doğacağını verir (en az 1). Sayıyı değiştiren etiket yoksa 1'dir;
    /// birden fazla varsa en büyüğü kazanır.
    /// </summary>
    public static int GetSpawnCount(WordEntry word)
    {
        if (word?.tags == null) return 1;

        int count = 1;
        foreach (string tag in word.tags)
        {
            if (tag != null && Implemented.TryGetValue(tag, out ITagBehaviour behaviour) && behaviour is ISpawnCountTag counter)
                count = System.Math.Max(count, counter.GetSpawnCount(word));
        }
        return count;
    }

    static Dictionary<string, ITagBehaviour> Build(params ITagBehaviour[] behaviours)
    {
        var map = new Dictionary<string, ITagBehaviour>();
        foreach (ITagBehaviour behaviour in behaviours)
            map.Add(behaviour.Tag, behaviour);
        return map;
    }

    static bool Contains(IReadOnlyCollection<string> list, string tag)
    {
        foreach (string item in list)
            if (item == tag) return true;
        return false;
    }
}
