using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kan ve parçalanma görüntüleri: vurulana kan sıçrar, damlalar yere düşüp kalıcı leke olur; elenen karakter
/// resminden kesilmiş parçalara ayrılıp savrulur, sekip yerde kalır. Yalnızca görüntüdür, oyun kurallarını değiştirmez.
/// Çok fazla leke/parça birikirse en eskileri silinir (oyun yavaşlamasın).
/// </summary>
public static class Gore
{
    /// <summary>Yere düşmüş lekelerin sırası: zeminin hemen üstü, karakterlerin altı.</summary>
    public const int StainOrder = -6;

    /// <summary>Havadaki damla ve parçaların sırası (karakterlerin üstü).</summary>
    public const int AirOrder = 12;

    const int MaxStains = 1500;
    const int MaxGibs = 300;

    static readonly Queue<GameObject> stains = new Queue<GameObject>();
    static readonly Queue<GameObject> gibs = new Queue<GameObject>();
    static Sprite pixel;

    static readonly Color[] BloodColors =
    {
        new Color(0.62f, 0.04f, 0.06f), new Color(0.78f, 0.08f, 0.09f), new Color(0.48f, 0.02f, 0.05f), new Color(0.9f, 0.15f, 0.12f),
    };

    /// <summary>Tek piksellik beyaz kare (renk sonradan verilir).</summary>
    public static Sprite Pixel
    {
        get
        {
            if (pixel != null) return pixel;
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return pixel;
        }
    }

    public static Color RandomBlood() => BloodColors[Random.Range(0, BloodColors.Length)];

    /// <summary>Bir noktadan <paramref name="count"/> kan damlası saçar.</summary>
    public static void Splash(Vector2 at, int count, float power = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Random.Range(1.5f, 5.5f) * power;
            BloodDrop.Spawn(at, dir * speed, Random.Range(0.2f, 0.6f), Random.Range(2f, 7f) * power);
        }
    }

    /// <summary>
    /// Karakteri parçalara ayırır: <paramref name="source"/> resmi 2 sütun x 3 satır parçaya kesilir, her parça
    /// merkezden dışarı savrulur ve arkasında kan izi bırakır. Bir de büyük kan sıçraması olur.
    /// </summary>
    public static void Explode(Sprite source, Vector2 at, float worldSize, Color tint)
    {
        Splash(at, 45, 1.3f);
        if (source == null) return;

        Rect rect = source.textureRect;
        const int columns = 2, rows = 3;
        float cellW = rect.width / columns, cellH = rect.height / rows;
        float pixelsPerUnit = source.pixelsPerUnit;
        float scale = worldSize / Mathf.Max(1f, rect.width / pixelsPerUnit);

        for (int cx = 0; cx < columns; cx++)
        for (int cy = 0; cy < rows; cy++)
        {
            var cell = new Rect(rect.x + cx * cellW, rect.y + cy * cellH, cellW, cellH);
            Sprite piece = Piece(source, cell, cx * rows + cy);

            // Parçanın karakter içindeki yeri (merkeze göre); oradan dışarı savrulur.
            Vector2 offset = new Vector2((cx + 0.5f) / columns - 0.5f, (cy + 0.5f) / rows - 0.5f) * worldSize;
            Vector2 dir = (offset.sqrMagnitude > 0.0001f ? offset.normalized : Random.insideUnitCircle.normalized)
                          + Random.insideUnitCircle * 0.6f;
            GibPiece.Spawn(piece, at + offset, dir.normalized * Random.Range(2f, 4.5f), Random.Range(5f, 9f), scale, tint);
        }
    }

    /// <summary>
    /// Yanarak/kömürleşerek ölen: karakter parçalanmaz, simsiyah kömür hâlinde yere yatar ve maç boyunca orada kalır.
    /// Üstünden birkaç saniye duman ve kıvılcım çıkar, çevresine kül saçılır.
    /// </summary>
    public static void ExplodeCharred(Sprite source, Vector2 at, float worldSize, Color tint)
    {
        Burn(at);

        var corpse = new GameObject("Kömür ceset");
        corpse.transform.position = new Vector3(at.x, at.y - worldSize * 0.15f, 0f);
        corpse.transform.rotation = Quaternion.Euler(0f, 0f, Random.value < 0.5f ? 90f : -90f); // Yana yatmış
        corpse.transform.localScale = Vector3.one * worldSize;
        var sr = corpse.AddComponent<SpriteRenderer>();
        sr.sprite = source != null ? source : SpriteFactory.Circle();
        sr.color = new Color(0.07f, 0.055f, 0.05f, 1f);
        sr.sortingOrder = StainOrder + 2;
        RegisterGib(corpse);

        // Bir süre tüter.
        BurnVisual smoke = BurnVisual.Attach(corpse);
        smoke.Burning = true;
        Object.Destroy(smoke, 3f);

        // Yanık izi ve kül.
        Color ash = new Color(0.12f, 0.1f, 0.09f);
        for (int i = 0; i < 12; i++)
            BloodDrop.Spawn(at, Random.insideUnitCircle * 1.5f, 0.2f, Random.Range(1f, 3f), Random.Range(0.08f, 0.16f), ash, true);
    }

    /// <summary>Lavda yanan karakter: kıvılcımlar ve duman yükselir, birkaç kömürleşmiş parça savrulur.</summary>
    public static void Burn(Vector2 at)
    {
        Color[] embers = { new Color(1f, 0.8f, 0.2f), new Color(1f, 0.45f, 0.05f), new Color(0.9f, 0.2f, 0.05f) };
        for (int i = 0; i < 30; i++)
            BloodDrop.Spawn(at, Random.insideUnitCircle * 2.5f, Random.Range(0.1f, 0.5f), Random.Range(4f, 10f),
                            Random.Range(0.08f, 0.16f), embers[Random.Range(0, embers.Length)], false);
        for (int i = 0; i < 10; i++)
            BloodDrop.Spawn(at, Random.insideUnitCircle * 1.2f, Random.Range(0.4f, 1f), Random.Range(5f, 9f),
                            Random.Range(0.25f, 0.4f), new Color(0.2f, 0.2f, 0.22f, 0.75f), false);
        for (int i = 0; i < 4; i++)
            BloodDrop.Spawn(at, Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f), 0.3f, Random.Range(4f, 7f),
                            Random.Range(0.15f, 0.25f), new Color(0.12f, 0.08f, 0.06f), true); // Kömür parçaları yerde kalır
    }

    /// <summary>
    /// Ekrandaki bütün kanı, parçaları, kömür cesetleri, is lekelerini ve havadaki kıvılcım/dumanları siler.
    /// Yeni maç başlarken çağrılır (ağ maçında sahne yeniden yüklenmez, izler kalmasın).
    /// </summary>
    public static int ClearAll()
    {
        int count = 0;
        while (stains.Count > 0) { GameObject go = stains.Dequeue(); if (go != null) { Object.Destroy(go); count++; } }
        while (gibs.Count > 0) { GameObject go = gibs.Dequeue(); if (go != null) { Object.Destroy(go); count++; } }
        foreach (BloodDrop drop in Object.FindObjectsByType<BloodDrop>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Object.Destroy(drop.gameObject); count++; }
        foreach (GibPiece gib in Object.FindObjectsByType<GibPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Object.Destroy(gib.gameObject); count++; }
        foreach (ExplosionEffect blast in Object.FindObjectsByType<ExplosionEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Object.Destroy(blast.gameObject); count++; }
        return count;
    }

    static readonly Dictionary<Sprite, Sprite[]> pieceCache = new Dictionary<Sprite, Sprite[]>();

    /// <summary>Kaynak resmin parçası; her resim için bir kez kesilir (önbellek).</summary>
    static Sprite Piece(Sprite source, Rect cell, int index)
    {
        if (!pieceCache.TryGetValue(source, out Sprite[] pieces) || pieces == null)
        {
            pieces = new Sprite[6];
            pieceCache[source] = pieces;
        }
        if (pieces[index] == null)
            pieces[index] = Sprite.Create(source.texture, cell, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
        return pieces[index];
    }

    /// <summary>Lekeyi kayda alır; çok birikirse en eskisini siler.</summary>
    public static void RegisterStain(GameObject stain)
    {
        stains.Enqueue(stain);
        while (stains.Count > MaxStains)
        {
            GameObject old = stains.Dequeue();
            if (old != null) Object.Destroy(old);
        }
    }

    /// <summary>Parçayı kayda alır; çok birikirse en eskisini siler.</summary>
    public static void RegisterGib(GameObject gib)
    {
        gibs.Enqueue(gib);
        while (gibs.Count > MaxGibs)
        {
            GameObject old = gibs.Dequeue();
            if (old != null) Object.Destroy(old);
        }
    }
}
