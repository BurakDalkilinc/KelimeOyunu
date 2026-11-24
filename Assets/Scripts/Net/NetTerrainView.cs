using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ağ maçında haritayı gösterir: host'tan gelen durum metnini (karo türleri ve yanma) yerel bir haritaya uygular ve
/// <see cref="TerrainView"/> ile çizer. İstemcide yangın ya da hasar hesaplanmaz; yalnızca görüntüdür (kurallar host'tadır).
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

        map.TryApply(text, burning);

        // Evler maç boyunca değişmez; yalnızca farklıysa yeniden kur.
        string houses = channel.LatestHouses ?? "";
        if (houses != appliedHouses)
        {
            appliedHouses = houses;
            map.TryApplyHouses(houses);
        }
    }
}
