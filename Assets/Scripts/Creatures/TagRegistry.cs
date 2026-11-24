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
        new TrapTag(),
        new PullTag(),
        new FlyingTag(),
        new HealTag(),
        new ShieldTag(),
        new StealthTag(),
        new EscapeTag(),
        new WallTag(),
        new ReviveTag(),
        new IgniteTag(),
        new MapWideTag(),
        new WaterTag(),
        new SkyTag(),
        new FireTag(),
        new WeaponTag());

    /// <summary>Hız zaten kelimenin değerinden geldiği için ek davranışı olmayan etiketler.</summary>
    public static readonly IReadOnlyCollection<string> LabelOnlyTags = new[] { "slow", "fast", "armored" };

    /// <summary>Henüz uygulanmamış etiketler. Sıradaki gruplarda tek tek uygulanacak.</summary>
    public static readonly IReadOnlyCollection<string> PendingTags = new string[0];

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

    /// <summary>
    /// Global tehdit kelimesinin etkilerini etiketlerinden kurar (ör. "map_wide" → tüm haritayı vuran etki).
    /// Etki üreten etiketi olmayan kelimede liste boş döner.
    /// </summary>
    public static List<IThreatEffect> CreateThreatEffects(WordEntry word)
    {
        var effects = new List<IThreatEffect>();
        if (word?.tags == null) return effects;

        foreach (string tag in word.tags)
        {
            if (tag != null && Implemented.TryGetValue(tag, out ITagBehaviour behaviour) && behaviour is IThreatEffectTag source)
            {
                IThreatEffect effect = source.CreateEffect(word);
                if (effect != null) effects.Add(effect);
            }
        }
        return effects;
    }

    /// <summary>
    /// Yazanın kendine uyguladığı etkileri (can, kalkan, görünmezlik, uçma...) uygular. Kelimenin etiketlerinden
    /// "self etkisi" olan her biri çalışır. Uygulanan etiket sayısını döndürür.
    /// </summary>
    public static int ApplySelfEffects(WordEntry word, SelfEffectContext context)
    {
        if (word?.tags == null || context.Writer == null) return 0;

        int applied = 0;
        foreach (string tag in word.tags)
        {
            if (tag != null && Implemented.TryGetValue(tag, out ITagBehaviour behaviour) && behaviour is ISelfEffectTag selfEffect)
            {
                selfEffect.ApplySelf(context);
                applied++;
            }
        }
        return applied;
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
