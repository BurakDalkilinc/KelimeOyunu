using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Haritayı piksel karolarla çizer: her kare için bir sprite, yanan karelerin üstüne animasyonlu alev. Karo değişince (yanıp kömürleşme)
/// ilgili sprite güncellenir. Hiçbir kural işletmez; tek kişilik sahnede <see cref="MatchBootstrap"/>'ın, ağ maçında host'tan gelen
/// durumun haritasını gösterir.
/// </summary>
public class TerrainView : MonoBehaviour
{
    const int TerrainOrder = -9;
    const int FireOrder = -7;

    /// <summary>Çatı karakterlerin (10) ve yaratıkların (8) üstünde çizilir: altındakiler görünmez.</summary>
    const int RoofOrder = 15;

    /// <summary>Çatının saydamlaşıp belirme hızı (saniyede alfa).</summary>
    const float RoofFadePerSecond = 6f;

    [SerializeField] MatchBootstrap bootstrap;

    TerrainMap map;
    Func<int, int, bool> isBurning;
    SpriteRenderer[] tiles;
    GameObject[] flames;
    readonly List<GameObject> roofObjects = new List<GameObject>();
    readonly List<SpriteRenderer[]> roofTiles = new List<SpriteRenderer[]>();
    readonly List<float> roofAlpha = new List<float>();

    /// <summary>
    /// Bu bilgisayardaki oyuncunun konumu; içinde durduğu evin çatısı görünmez olur. Boşsa hiçbir çatı gizlenmez.
    /// Tek kişilik sahnede kendiliğinden bağlanır, ağda <see cref="NetTerrainView"/> bağlar.
    /// </summary>
    public Func<Vector2?> ViewerPosition { get; set; }

    /// <summary>Tek kişilik sahnede maç kurulunca kendiliğinden bağlanır.</summary>
    void Start()
    {
        if (bootstrap != null && bootstrap.Terrain != null)
        {
            Bind(bootstrap.Terrain.Map, bootstrap.Terrain.Fire.IsBurning);
            ViewerPosition = LocalPlayerPosition;
        }
    }

    /// <summary>Tek kişilikte yerel oyuncu bot olmayan tek oyuncudur.</summary>
    Vector2? LocalPlayerPosition()
    {
        if (bootstrap == null || bootstrap.Match == null) return null;
        foreach (PlayerState player in bootstrap.Match.Players)
            if (!player.IsBot) return bootstrap.TryGetPosition(player.Id, out Vector2 at) ? at : (Vector2?)null;
        return null;
    }

    /// <summary>Haritayı ve "karo yanıyor mu" bilgisini bağlar, bütün karoları çizer. Tekrar çağrılırsa baştan kurar.</summary>
    public void Bind(TerrainMap newMap, Func<int, int, bool> burning)
    {
        Unbind();

        map = newMap;
        isBurning = burning;
        tiles = new SpriteRenderer[map.Width * map.Height];
        flames = new GameObject[tiles.Length];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var go = new GameObject($"Karo {x},{y}");
                go.transform.SetParent(transform, false);
                go.transform.position = map.CellCenter(x, y);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = TerrainOrder;
                tiles[map.Index(x, y)] = sr;
                Refresh(x, y);
            }
        }

        map.TileChanged += OnTileChanged;
        map.HousesChanged += RebuildRoofs;
        RebuildRoofs();
    }

    void Unbind()
    {
        if (map != null)
        {
            map.TileChanged -= OnTileChanged;
            map.HousesChanged -= RebuildRoofs;
        }
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        roofObjects.Clear();
        roofTiles.Clear();
        roofAlpha.Clear();
        map = null;
    }

    void OnDestroy()
    {
        if (map == null) return;
        map.TileChanged -= OnTileChanged;
        map.HousesChanged -= RebuildRoofs;
    }

    /// <summary>Her ev için kiremit çatı kurar: üst sıra, sırt, ara sıralar, en altta saçak.</summary>
    void RebuildRoofs()
    {
        foreach (GameObject roof in roofObjects)
            if (roof != null) Destroy(roof);
        roofObjects.Clear();
        roofTiles.Clear();
        roofAlpha.Clear();
        if (map == null) return;

        foreach (House house in map.Houses)
        {
            var root = new GameObject($"Çatı {house.X},{house.Y}");
            root.transform.SetParent(transform, false);

            var renderers = new SpriteRenderer[house.W * house.H];
            for (int cy = 0; cy < house.H; cy++)
            {
                int fromTop = house.H - 1 - cy;
                string spriteName = fromTop == 0 ? "t_roof_a"
                    : fromTop == 1 ? "t_roof_ridge"
                    : cy == 0 ? "t_roof_eave"
                    : "t_roof_b";

                for (int cx = 0; cx < house.W; cx++)
                {
                    var go = new GameObject("kiremit");
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = map.CellCenter(house.X + cx, house.Y + cy);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteLibrary.Get(spriteName);
                    sr.sortingOrder = RoofOrder;
                    renderers[cy * house.W + cx] = sr;
                }
            }

            roofObjects.Add(root);
            roofTiles.Add(renderers);
            roofAlpha.Add(1f);
        }
    }

    /// <summary>İçindeki (ya da yanan / yanıp çöken) evin çatısını yumuşakça saydamlaştırır, dışarı çıkınca geri getirir.</summary>
    void UpdateRoofs()
    {
        if (map == null) return;

        Vector2? viewer = ViewerPosition?.Invoke();
        int count = Mathf.Min(map.Houses.Count, roofObjects.Count);

        for (int i = 0; i < count; i++)
        {
            bool hide = RoofRules.ShouldHide(map, map.Houses[i], viewer, isBurning);
            float alpha = Mathf.MoveTowards(roofAlpha[i], hide ? 0f : 1f, RoofFadePerSecond * Time.deltaTime);
            if (Mathf.Approximately(alpha, roofAlpha[i])) continue;

            roofAlpha[i] = alpha;
            roofObjects[i].SetActive(alpha > 0.01f);
            foreach (SpriteRenderer sr in roofTiles[i]) sr.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    void OnTileChanged(int x, int y)
    {
        Refresh(x, y);

        // Üstündeki uçurum kenarı sprite'ı komşuya bağlı: alttaki karoyu da tazele.
        if (map.InBounds(x, y - 1)) Refresh(x, y - 1);
    }

    void Update()
    {
        if (map == null || isBurning == null) return;

        UpdateRoofs();

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                int index = map.Index(x, y);
                bool burning = isBurning(x, y);

                if (burning && flames[index] == null) flames[index] = CreateFlame(x, y);
                else if (flames[index] != null && flames[index].activeSelf != burning) flames[index].SetActive(burning);
            }
        }
    }

    GameObject CreateFlame(int x, int y)
    {
        var go = new GameObject($"Alev {x},{y}");
        go.transform.SetParent(transform, false);
        go.transform.position = map.CellCenter(x, y);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = FireOrder;
        go.AddComponent<SpriteAnimator>().SetFrames(SpriteLibrary.GetFrames("t_fire"));
        return go;
    }

    void Refresh(int x, int y)
    {
        SpriteRenderer sr = tiles[map.Index(x, y)];
        TileType type = map.At(x, y);

        // Eski animasyonu temizle (lav → kömür gibi bir değişim olursa).
        var old = sr.GetComponent<SpriteAnimator>();
        if (old != null) Destroy(old);

        switch (type)
        {
            case TileType.Path: sr.sprite = SpriteLibrary.Get("t_path"); break;
            case TileType.WoodFloor: sr.sprite = SpriteLibrary.Get("t_wood"); break;
            case TileType.WoodWall: sr.sprite = SpriteLibrary.Get("t_woodwall"); break;
            case TileType.CharredFloor: sr.sprite = SpriteLibrary.Get("t_charred"); break;
            case TileType.Chasm:
                // Üstündeki karo uçurum değilse kaya dudağı çiz.
                sr.sprite = SpriteLibrary.Get(map.At(x, y + 1) == TileType.Chasm ? "t_chasm" : "t_chasm_edge");
                break;
            case TileType.Lava:
                sr.gameObject.AddComponent<SpriteAnimator>().SetFrames(SpriteLibrary.GetFrames("t_lava"));
                break;
            default:
                sr.sprite = SpriteLibrary.Get(((x * 7 + y * 13) % 3 == 0) ? "t_grass_alt" : "t_grass");
                break;
        }
    }
}
