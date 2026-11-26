using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Haritayı piksel karolarla çizer: her kare için bir sprite, yanan karelerin üstüne animasyonlu alev. Karo değişince (yanıp kömürleşme)
/// ilgili sprite güncellenir. Zemin, patika ve çatı resimleri haritanın temasından (<see cref="MapTheme"/>) gelir; bir resim henüz
/// yoksa köy temasındaki karşılığı kullanılır. Hiçbir kural işletmez; tek kişilik sahnede <see cref="MatchBootstrap"/>'ın, ağ maçında
/// host'tan gelen durumun haritasını gösterir.
/// </summary>
public class TerrainView : MonoBehaviour
{
    const int TerrainOrder = -9;
    const int FireOrder = -7;

    /// <summary>Ev duvarı bloklarının sırası: zeminin üstü, alevlerin altı.</summary>
    const int WallStackOrder = -8;

    /// <summary>Katmanlı (3D piksel) ev duvarının yüksekliği (dilim = 1 piksel) ve çatının kalınlığı.</summary>
    const int WallLayers = 8;
    const int RoofLayers = 3;

    /// <summary>Kaktüsün katman sayısı (duvardan biraz yüksek).</summary>
    const int CactusLayers = 10;

    /// <summary>Uçurumun ve lav havuzunun derinliği (piksel): kenardaki zemin bu kadar aşağı inen bir yüz olarak çizilir.</summary>
    const int ChasmDepth = 10;

    /// <summary>Uçurum ve lav karoları en altta: üstlerine kenardaki zeminin aşağı inen yüzü çizilir.</summary>
    const int HoleOrder = -25;
    const int LavaDepth = 3;
    const float Pixel = 1f / 16f;

    /// <summary>
    /// Çatı karakterlerle aynı sıradadır; her evin çatısı evin güney kenarında duran tek bir grup olarak ekrandaki yüksekliğe göre
    /// sıralanır: evin önündekiler çatının önünde, içindekiler ve arkasındakiler çatının arkasında (görünmez) kalır.
    /// </summary>
    const int RoofOrder = CreatureSpriteScale.EntityOrder;

    /// <summary>Çatının saydamlaşıp belirme hızı (saniyede alfa).</summary>
    const float RoofFadePerSecond = 6f;

    [SerializeField] MatchBootstrap bootstrap;

    TerrainMap map;
    Func<int, int, bool> isBurning;
    SpriteRenderer[] tiles;
    GameObject[] flames;
    SpriteStack[] wallStacks;
    TileType[] wallStackTypes;
    GameObject[] cliffs;
    int[] cliffDepths;
    readonly List<GameObject> roofObjects = new List<GameObject>();
    readonly List<SpriteStack[]> roofTiles = new List<SpriteStack[]>();
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
        wallStacks = new SpriteStack[tiles.Length];
        wallStackTypes = new TileType[tiles.Length];
        cliffs = new GameObject[tiles.Length];
        cliffDepths = new int[tiles.Length];

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
        map.ThemeChanged += OnThemeChanged;
        RebuildRoofs();
    }

    /// <summary>Tema değişti: bütün karolar ve çatılar yeni temanın resimleriyle yeniden çizilir.</summary>
    void OnThemeChanged()
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                Refresh(x, y);
        RebuildRoofs();
    }

    void Unbind()
    {
        if (map != null)
        {
            map.TileChanged -= OnTileChanged;
            map.HousesChanged -= RebuildRoofs;
            map.ThemeChanged -= OnThemeChanged;
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
        map.ThemeChanged -= OnThemeChanged;
    }

    /// <summary>Temanın resmi; yoksa köy temasındaki karşılığı (resim çizilmeden önce de oyun çökmeden çalışsın).</summary>
    static Sprite SpriteOr(string spriteName, string fallbackName)
    {
        Sprite sprite = SpriteLibrary.Get(spriteName);
        return sprite != null ? sprite : SpriteLibrary.Get(fallbackName);
    }

    /// <summary>Animasyon kareleri; yoksa yedek resmin kareleri.</summary>
    static Sprite[] FramesOr(string spriteName, string fallbackName)
    {
        Sprite[] frames = SpriteLibrary.GetFrames(spriteName);
        return frames.Length > 0 ? frames : SpriteLibrary.GetFrames(fallbackName);
    }

    /// <summary>Her ev için temanın çatısını kurar: üst sıra, sırt, ara sıralar, en altta saçak.</summary>
    void RebuildRoofs()
    {
        foreach (GameObject roof in roofObjects)
            if (roof != null) Destroy(roof);
        roofObjects.Clear();
        roofTiles.Clear();
        roofAlpha.Clear();
        if (map == null) return;
        MapTheme theme = map.Theme;

        foreach (House house in map.Houses)
        {
            var root = new GameObject($"Çatı {house.X},{house.Y}");
            root.transform.SetParent(transform, false);

            // Grubun noktası evin güney kenarı (sıralama buna göre); çatı duvarların üstüne oturur: duvar yüksekliği kadar yukarıda.
            Vector2 southWest = map.CellCenter(house.X, house.Y) - new Vector2(0.5f, 0.5f);
            root.transform.position = new Vector3(southWest.x + house.W * 0.5f, southWest.y, 0f);
            var group = root.AddComponent<UnityEngine.Rendering.SortingGroup>();
            group.sortingOrder = RoofOrder;
            Vector3 lift = new Vector3(0f, WallLayers * Pixel, 0f);
            var renderers = new SpriteStack[house.W * house.H];
            for (int cy = 0; cy < house.H; cy++)
            {
                int fromTop = house.H - 1 - cy;
                string part = fromTop == 0 ? "a"
                    : fromTop == 1 ? "ridge"
                    : cy == 0 ? "eave"
                    : "b";
                Sprite roofSprite = SpriteOr(theme.RoofSprite(part), MapTheme.Village.RoofSprite(part));

                for (int cx = 0; cx < house.W; cx++)
                {
                    var anchor = new GameObject("kiremit");
                    anchor.transform.SetParent(root.transform, false);
                    anchor.transform.position = (Vector3)map.CellCenter(house.X + cx, house.Y + cy) + lift;
                    renderers[cy * house.W + cx] = SpriteStack.Create(anchor.transform, "kiremit katmanları",
                        StackLibrary.Extrude(roofSprite, RoofLayers), RoofOrder, 1f, 1f, 0.7f);
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
            foreach (SpriteStack tile in roofTiles[i]) tile.SetAlpha(alpha);
        }
    }

    void OnTileChanged(int x, int y)
    {
        Refresh(x, y);

        // Üstündeki uçurum kenarı sprite'ı komşuya bağlı: alttaki karoyu da tazele.
        if (map.InBounds(x, y - 1)) Refresh(x, y - 1);

        // Kuzeydeki karonun uçurum yüzü bu karoya bağlı (bu karo uçurum/lav oldu ya da olmaktan çıktı).
        if (map.InBounds(x, y + 1)) RefreshCliff(x, y + 1);
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

    /// <summary>
    /// İçinden geçilmeyen kare (ev duvarı, kaktüs) yükselen bir blok olarak çizilir (katmanlı); duvar yanıp çökünce blok kalkar.
    /// Tür değişirse (ör. tema değişti) blok yeniden kurulur.
    /// </summary>
    void RefreshWallStack(int x, int y, TileType type)
    {
        int index = map.Index(x, y);
        bool solid = TerrainMap.IsSolid(type);

        if (wallStacks[index] != null && (!solid || wallStackTypes[index] != type))
        {
            Destroy(wallStacks[index].transform.parent.gameObject);
            wallStacks[index] = null;
        }
        if (!solid || wallStacks[index] != null) return;

        Sprite[] slices = StackSlices(type);
        if (slices == null) return; // Resim yok: düz karo yeter

        var anchor = new GameObject($"Duvar bloğu {x},{y}");
        anchor.transform.SetParent(transform, false);
        anchor.transform.position = map.CellCenter(x, y);
        wallStacks[index] = SpriteStack.Create(anchor.transform, "katmanlar", slices, WallStackOrder, 1f);
        wallStackTypes[index] = type;
    }

    /// <summary>Blok dilimleri: kaktüsün hazır katmanlı çizimi varsa o, yoksa düz resim kalınlaştırılır.</summary>
    static Sprite[] StackSlices(TileType type)
    {
        if (TerrainMap.IsPrickly(type))
            return StackLibrary.Get("t_cactus") ?? StackLibrary.Extrude(SpriteLibrary.Get("t_cactus"), CactusLayers);
        return StackLibrary.Extrude(SpriteOr(WallSprite(type), "t_woodwall"), WallLayers);
    }

    /// <summary>Ev duvarının resmi (tahta, taş, kerpiç).</summary>
    static string WallSprite(TileType type)
    {
        switch (type)
        {
            case TileType.StoneWall: return "t_stonewall";
            case TileType.AdobeWall: return "t_adobewall";
            default: return "t_woodwall";
        }
    }

    /// <summary>Çukur gibi çizilen karo: uçurum ya da havuz (lav, dondurucu su, bataklık kumu).</summary>
    static bool IsHole(TileType type) => type == TileType.Chasm || TerrainMap.IsPool(type);

    /// <summary>
    /// Güneyinde uçurum ya da lav olan zemin karosu, aşağı doğru inen bir yüz olarak çizilir: aynı resim katman katman 1 piksel
    /// aşağıda ve gittikçe koyu tekrar edilir (uçurumda derin, lavda sığ). Böylece uçurum çukur, lav çökük havuz gibi görünür.
    /// </summary>
    void RefreshCliff(int x, int y)
    {
        int index = map.Index(x, y);
        TileType here = map.At(x, y);
        TileType south = map.InBounds(x, y - 1) ? map.At(x, y - 1) : here;
        bool hereIsHole = IsHole(here);
        int depth = hereIsHole ? 0 : south == TileType.Chasm ? ChasmDepth : TerrainMap.IsPool(south) ? LavaDepth : 0;

        // Karo ya da güneyi değişmiş olabilir (çimen kömürleşti, uçurum kapandı): yüzü baştan kur.
        Sprite face = tiles[index].sprite;
        if (cliffs[index] != null)
        {
            Destroy(cliffs[index]);
            cliffs[index] = null;
        }
        cliffDepths[index] = depth;
        if (depth == 0 || face == null) return;

        var root = new GameObject($"Uçurum yüzü {x},{y}");
        root.transform.SetParent(transform, false);
        root.transform.position = map.CellCenter(x, y);
        for (int i = 1; i <= depth; i++)
        {
            var layer = new GameObject("kat " + i);
            layer.transform.SetParent(root.transform, false);
            layer.transform.localPosition = new Vector3(0f, -i * Pixel, 0f);
            var sr = layer.AddComponent<SpriteRenderer>();
            sr.sprite = face;
            float shade = Mathf.Lerp(0.75f, 0.25f, (float)i / depth); // Aşağı indikçe karanlık
            sr.color = new Color(shade, shade * 0.95f, shade * 0.9f, 1f);
            sr.sortingOrder = TerrainOrder - i; // Zeminin altında; yukarıdaki kat aşağıdakinin üstünde
            sr.sortingLayerID = tiles[index].sortingLayerID;
        }
        cliffs[index] = root;
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
        sr.sortingOrder = IsHole(type) ? HoleOrder : TerrainOrder;
        RefreshWallStack(x, y, type);
        MapTheme theme = map.Theme;
        MapTheme village = MapTheme.Village;

        // Eski animasyonu temizle (lav → kömür gibi bir değişim olursa).
        var old = sr.GetComponent<SpriteAnimator>();
        if (old != null) Destroy(old);

        switch (type)
        {
            case TileType.Path: sr.sprite = SpriteOr(theme.PathSprite, village.PathSprite); break;
            case TileType.WoodFloor: sr.sprite = SpriteLibrary.Get("t_wood"); break;
            case TileType.StoneFloor: sr.sprite = SpriteOr("t_stone", "t_path"); break;
            case TileType.AdobeFloor: sr.sprite = SpriteOr("t_adobe", "t_wood"); break;
            case TileType.WoodWall:
            case TileType.StoneWall:
            case TileType.AdobeWall:
                sr.sprite = SpriteOr(WallSprite(type), "t_woodwall");
                break;
            case TileType.CharredFloor: sr.sprite = SpriteLibrary.Get("t_charred"); break;
            case TileType.Ice: sr.sprite = SpriteOr("t_ice", theme.GroundSprite); break;
            case TileType.Cactus: sr.sprite = SpriteOr(theme.GroundSprite, village.GroundSprite); break; // Kaktüs bloğu üstünde
            case TileType.Chasm:
                // Üstündeki karo uçurum değilse kaya dudağı çiz.
                sr.sprite = SpriteLibrary.Get(map.At(x, y + 1) == TileType.Chasm ? "t_chasm" : "t_chasm_edge");
                break;
            case TileType.Lava:
                sr.gameObject.AddComponent<SpriteAnimator>().SetFrames(SpriteLibrary.GetFrames("t_lava"));
                break;
            case TileType.FreezingWater:
                sr.gameObject.AddComponent<SpriteAnimator>().SetFrames(FramesOr("t_water", "t_lava"));
                break;
            case TileType.Quicksand:
                sr.gameObject.AddComponent<SpriteAnimator>().SetFrames(FramesOr("t_quicksand", "t_lava"));
                break;
            default:
                bool alt = (x * 7 + y * 13) % 3 == 0;
                sr.sprite = alt ? SpriteOr(theme.GroundAltSprite, village.GroundAltSprite) : SpriteOr(theme.GroundSprite, village.GroundSprite);
                break;
        }

        RefreshCliff(x, y); // Karonun resmi belli olduktan sonra: uçurum yüzü o resimden yapılır
    }
}
