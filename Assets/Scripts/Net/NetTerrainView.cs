using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağ maçında haritayı gösterir: host'tan gelen durum metnini (tema, karo türleri ve yanma) yerel bir haritaya uygular ve
/// <see cref="TerrainView"/> ile çizer. İstemcide yangın ya da hasar hesaplanmaz; yalnızca görüntüdür (kurallar host'tadır).
/// Boss'un yıktığı duvarlar da bu tam durumla gelir (karo + ev listesi; geç bağlanan da doğru haritayı görür); önceki haritayla
/// karşılaştırıp (<see cref="HouseDamage.FindDemolished"/>) yıkılan duvarlarda toz/sarsıntı/ses oynatılır.
/// </summary>
public class NetTerrainView : MonoBehaviour
{
    [SerializeField] NetTurnChannel channel;
    [SerializeField] TerrainView view;
    [SerializeField] GameConfig config;

    TerrainMap map;
    bool[] burning;
    int appliedVersion = -1;
    string appliedHouses = null;
    readonly List<DemolishedCell> wallsBefore = new List<DemolishedCell>();
    readonly List<House> housesBefore = new List<House>();
    readonly List<DemolishedCell> demolished = new List<DemolishedCell>();

    void Awake()
    {
        if (channel == null || view == null || config == null)
        {
            Debug.LogError("NetTerrainView: channel, view ya da config eksik.", this);
            enabled = false;
            return;
        }

        map = new TerrainMap(Mathf.RoundToInt(config.arenaSize.x), Mathf.RoundToInt(config.arenaSize.y));
        burning = new bool[map.Width * map.Height];
        view.Bind(map, (x, y) => burning[map.Index(x, y)]);
        view.ViewerPosition = LocalAvatarPosition;
    }

    /// <summary>Bu bilgisayardaki oyuncunun karakteri (çatı, içindeyken saydamlaşır); yoksa null.</summary>
    static Vector2? LocalAvatarPosition()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return null;
        return (Vector2)manager.LocalClient.PlayerObject.transform.position;
    }

    void Update()
    {
        if (channel.TerrainVersion == appliedVersion) return;
        appliedVersion = channel.TerrainVersion;

        // Boş metin: maç yok ya da yeni maç hazırlanıyor, düz çimen göster.
        string text = channel.LatestTerrain;
        if (string.IsNullOrEmpty(text))
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    map.Set(x, y, TileType.Grass);
            System.Array.Clear(burning, 0, burning.Length);
            appliedHouses = "";
            map.TryApplyHouses("");
            return;
        }

        // Yıkım efekti için önceki duvarlar ve evler (aynı temada kalınırsa karşılaştırılır).
        MapTheme theme = MapTheme.FromId(channel.LatestTheme);
        bool sameTheme = theme == map.Theme;
        HouseDamage.SnapshotHouseWalls(map, wallsBefore);
        housesBefore.Clear();
        housesBefore.AddRange(map.Houses);

        // Tema (zemin/çatı resimleri) host'takiyle aynı olsun; değişirse görünüm bütün karoları yeniden çizer.
        map.Theme = theme;
        map.TryApply(text, burning);

        // Evler yalnızca farklıysa yeniden kurulur (boss bir evi çökertince listeden çıkar).
        string houses = channel.LatestHouses ?? "";
        if (houses != appliedHouses)
        {
            appliedHouses = houses;
            map.TryApplyHouses(houses);
        }

        if (sameTheme && HouseDamage.FindDemolished(map, wallsBefore, housesBefore, demolished, out int collapsed))
            map.NotifyWallsDemolished(demolished, collapsed);
    }
}
