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

    [SerializeField] MatchBootstrap bootstrap;

    TerrainMap map;
    Func<int, int, bool> isBurning;
    SpriteRenderer[] tiles;
    GameObject[] flames;

    /// <summary>Tek kişilik sahnede maç kurulunca kendiliğinden bağlanır.</summary>
    void Start()
    {
        if (bootstrap != null && bootstrap.Terrain != null) Bind(bootstrap.Terrain.Map, bootstrap.Terrain.Fire.IsBurning);
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
    }

    void Unbind()
    {
        if (map != null) map.TileChanged -= OnTileChanged;
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        map = null;
    }

    void OnDestroy()
    {
        if (map != null) map.TileChanged -= OnTileChanged;
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
