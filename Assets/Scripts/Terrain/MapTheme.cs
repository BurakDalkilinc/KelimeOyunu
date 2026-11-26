using System;
using System.Collections.Generic;

/// <summary>
/// Harita teması: köy (tahta evler, lav), karlı dağ (taş evler, buz, dondurucu su), çöl (kerpiç evler, bataklık kumu, kaktüs).
/// Hangi karoların kullanılacağını, köylerin düzenini ve resim adlarını tutar. Düz C# sınıfı, test edilebilir.
/// </summary>
public sealed class MapTheme
{
    /// <summary>Kısa kimlik ("koy", "kar", "col"): ayarlarda ve ağ mesajında bu kullanılır.</summary>
    public string Id { get; }

    /// <summary>Oyuncuya görünen ad.</summary>
    public string DisplayName { get; }

    /// <summary>Ev duvarının karosu.</summary>
    public TileType WallTile { get; }

    /// <summary>Ev içi, meydan ve yürüyüş yolunun karosu.</summary>
    public TileType FloorTile { get; }

    /// <summary>Havuzların karosu: köyde lav, karda dondurucu su, çölde bataklık kumu.</summary>
    public TileType PoolTile { get; }

    /// <summary>İlk kaç köy büyük (iki sıra ev) kurulur; kalanlar tek sıra.</summary>
    public int BigVillages { get; }

    /// <summary>Köyler arasında en az kaç karo boşluk kalır (seyrek köyler için büyük).</summary>
    public int VillageGap { get; }

    /// <summary>Zemin (çimen/kar/kum) resmi, onun ara sıra görünen ikinci hâli ve patika resmi.</summary>
    public string GroundSprite { get; }
    public string GroundAltSprite { get; }
    public string PathSprite { get; }

    /// <summary>Çatı resimlerinin ön eki: "_a" (üst sıra), "_ridge" (sırt), "_b" (ara), "_eave" (saçak) eklenir.</summary>
    public string RoofPrefix { get; }

    MapTheme(string id, string displayName, TileType wall, TileType floor, TileType pool, int bigVillages, int villageGap,
             string ground, string groundAlt, string path, string roofPrefix)
    {
        Id = id;
        DisplayName = displayName;
        WallTile = wall;
        FloorTile = floor;
        PoolTile = pool;
        BigVillages = bigVillages;
        VillageGap = villageGap;
        GroundSprite = ground;
        GroundAltSprite = groundAlt;
        PathSprite = path;
        RoofPrefix = roofPrefix;
    }

    public const string VillageId = "koy";
    public const string SnowId = "kar";
    public const string DesertId = "col";

    /// <summary>Tahta köyler, lav havuzları (ilk harita; davranışı değişmez).</summary>
    public static readonly MapTheme Village = new MapTheme(VillageId, "Tahta Köy", TileType.WoodWall, TileType.WoodFloor, TileType.Lava,
        bigVillages: 2, villageGap: 2, "t_grass", "t_grass_alt", "t_path", "t_roof");

    /// <summary>Taş evli köyler, donmuş göller, dondurucu su havuzları. Hiçbir şey yanmaz.</summary>
    public static readonly MapTheme Snow = new MapTheme(SnowId, "Karlı Dağ", TileType.StoneWall, TileType.StoneFloor, TileType.FreezingWater,
        bigVillages: 1, villageGap: 2, "t_snow", "t_snow_alt", "t_snow_path", "t_roof_snow");

    /// <summary>Seyrek kerpiç köyler, geniş açıklık, bataklık kumu ve kaktüsler.</summary>
    public static readonly MapTheme Desert = new MapTheme(DesertId, "Çöl Kasabası", TileType.AdobeWall, TileType.AdobeFloor, TileType.Quicksand,
        bigVillages: 1, villageGap: 6, "t_sand", "t_sand_alt", "t_sand_path", "t_roof_adobe");

    /// <summary>Bütün temalar (rastgele seçim bu sırayla yapılır).</summary>
    public static IReadOnlyList<MapTheme> All { get; } = new[] { Village, Snow, Desert };

    /// <summary>Kimliğe göre tema; bilinmiyorsa (ya da boşsa) false.</summary>
    public static bool TryFind(string id, out MapTheme theme)
    {
        foreach (MapTheme candidate in All)
        {
            if (string.Equals(candidate.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                theme = candidate;
                return true;
            }
        }
        theme = Village;
        return false;
    }

    /// <summary>Kimliğe göre tema; bilinmiyorsa köy.</summary>
    public static MapTheme FromId(string id) => TryFind(id, out MapTheme theme) ? theme : Village;

    /// <summary>
    /// Maçın teması: <paramref name="forcedId"/> geçerli bir kimlikse o, değilse harita tohumundan rastgele (aynı tohum hep aynı tema).
    /// Tohumdan ayrı bir üretici türetilir, böylece haritanın kendi rastgeleliği etkilenmez.
    /// </summary>
    public static MapTheme Choose(string forcedId, int seed)
    {
        if (!string.IsNullOrWhiteSpace(forcedId) && TryFind(forcedId, out MapTheme forced)) return forced;
        return All[new Random(unchecked(seed * 31 + 17)).Next(All.Count)];
    }

    /// <summary>Çatı resminin adı (parça: "a", "ridge", "b", "eave").</summary>
    public string RoofSprite(string part) => RoofPrefix + "_" + part;

    /// <summary>Kapı karosunun resmi (sol ya da sağ yarı): "t_door_{tema}_l/_r". Resimde içerisi üstte, eşik (dışarısı) altta.</summary>
    public string DoorSprite(bool left) => "t_door_" + Id + (left ? "_l" : "_r");

    /// <summary>Kapının önündeki paspasın resmi (sol ya da sağ yarı, saydam zeminli).</summary>
    public string DoormatSprite(bool left) => "t_doormat_" + Id + (left ? "_l" : "_r");

    /// <summary>Kapının üstündeki saçağın resmi (çatı katmanında, kapının iki karesine yan yana).</summary>
    public string AwningSprite => "t_awning_" + Id;

    public override string ToString() => Id;
}
