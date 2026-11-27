using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss'ların evleri yıkması: gövdenin değdiği ev duvarı kareleri yıkılıp temanın zeminine döner (çatı o karede kalkar). Bir evin duvar
/// karelerinin <see cref="GameConfig.bossHouseCollapseRatio"/>'dan fazlası yıkılınca ev tamamen çöker: kalan duvarlar da gider, ev
/// listeden çıkar. Aktif küresel tehdidin sığınağı olan ev korunur (dışarıdan verilen koşul). Ağ istemcisinin, gelen yeni haritada
/// hangi duvarların yıkıldığını bulması da buradadır. Düz C# sınıfı, rastgelelik yok: aynı girdi hep aynı sonucu verir.
/// </summary>
public static class HouseDamage
{
    /// <summary>Bu yaratık yürürken ev yıkar mı? "boss" etiketi açar (<see cref="CreatureModel.BreaksHouses"/>); uçan, gökten düşen,
    /// alan, anında vuran, tuzak, mıknatıs ve mermi yıkmaz (yere basıp yürümüyorlar).</summary>
    public static bool CanBreakHouses(CreatureModel creature)
    {
        return creature != null && creature.BreaksHouses && !creature.IsFinished && !creature.IsFlying && !creature.IsSky
            && !creature.IsArea && !creature.IsStrike && !creature.IsTrap && !creature.IsPull && !creature.IsProjectile;
    }

    /// <summary>Dairenin (merkez, yarıçap) değdiği harita karelerini listeye yazar (liste önce temizlenir).</summary>
    public static void CellsUnderBody(TerrainMap map, Vector2 center, float radius, List<Vector2Int> into)
    {
        into.Clear();
        if (map == null) return;

        float r = Mathf.Max(0f, radius);
        Vector2Int cell = map.CellOf(center);
        int reach = Mathf.CeilToInt(r) + 1;
        for (int y = cell.y - reach; y <= cell.y + reach; y++)
        {
            for (int x = cell.x - reach; x <= cell.x + reach; x++)
            {
                if (!map.InBounds(x, y)) continue;
                Vector2 min = new Vector2(map.Origin.x + x, map.Origin.y + y);
                Vector2 closest = new Vector2(Mathf.Clamp(center.x, min.x, min.x + 1f), Mathf.Clamp(center.y, min.y, min.y + 1f));
                if ((closest - center).sqrMagnitude < r * r) into.Add(new Vector2Int(x, y));
            }
        }
    }

    /// <summary>Kare evin dış halkasında (duvar sırası) mı?</summary>
    public static bool IsBorder(House house, int x, int y)
    {
        return house.Contains(x, y) && (x == house.X || x == house.X + house.W - 1 || y == house.Y || y == house.Y + house.H - 1);
    }

    /// <summary>Evin başlangıçtaki duvar karesi sayısı: dış halka, kapı kareleri hariç.</summary>
    public static int WallCellCount(House house)
    {
        int ring = house.W <= 1 || house.H <= 1 ? house.W * house.H : 2 * house.W + 2 * house.H - 4;
        return Mathf.Max(1, ring - (house.HasDoor ? House.DoorWidth : 0));
    }

    /// <summary>Evin artık duvar olmayan (yıkılmış ya da yanıp çökmüş) duvar karesi sayısı.</summary>
    public static int BrokenWallCount(TerrainMap map, House house)
    {
        int broken = 0;
        for (int y = house.Y; y < house.Y + house.H; y++)
            for (int x = house.X; x < house.X + house.W; x++)
                if (IsBorder(house, x, y) && !house.IsDoorCell(x, y) && !TerrainMap.IsHouseWall(map.At(x, y))) broken++;
        return broken;
    }

    /// <summary>
    /// Değilen karelerdeki ev duvarlarını yıkar; oranı aşan evleri çökertir. <paramref name="isProtected"/> true dönen ev (sığınak)
    /// hiç yıkılmaz. Bir şey yıkıldıysa haritaya bildirir (<see cref="TerrainMap.NotifyWallsDemolished"/>). Sonuç
    /// <paramref name="result"/>'a yazılır (önce temizlenir); bir şey yıkıldıysa true.
    /// </summary>
    public static bool Apply(TerrainMap map, IReadOnlyList<Vector2Int> touched, float collapseRatio, Func<House, bool> isProtected,
                             HouseDamageResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        result.Clear();
        if (map == null || touched == null || touched.Count == 0) return false;

        TileType floor = map.Theme.FloorTile;
        var damaged = new List<int>(2);

        foreach (Vector2Int cell in touched)
        {
            TileType type = map.At(cell.x, cell.y);
            if (!map.InBounds(cell.x, cell.y) || !TerrainMap.IsHouseWall(type)) continue;

            // Yalnızca evlerin duvarları yıkılır (kayıtlı evi olmayan duvar, ör. elle konan, olduğu gibi kalır).
            int index = map.HouseIndexAt(cell.x, cell.y);
            if (index < 0) continue;
            if (isProtected != null && isProtected(map.Houses[index])) continue;

            map.Set(cell.x, cell.y, floor);
            result.Cells.Add(new DemolishedCell(cell.x, cell.y, type));
            if (!damaged.Contains(index)) damaged.Add(index);
        }

        // Çöken evler: sıraları büyükten küçüğe çıkarılır (listeden silince diğerlerinin sırası kaymasın).
        damaged.Sort();
        for (int i = damaged.Count - 1; i >= 0; i--)
        {
            House house = map.Houses[damaged[i]];
            if (BrokenWallCount(map, house) <= collapseRatio * WallCellCount(house)) continue;

            for (int y = house.Y; y < house.Y + house.H; y++)
            {
                for (int x = house.X; x < house.X + house.W; x++)
                {
                    TileType type = map.At(x, y);
                    if (!TerrainMap.IsHouseWall(type)) continue;
                    map.Set(x, y, floor);
                    result.Cells.Add(new DemolishedCell(x, y, type));
                }
            }
            result.Collapsed.Add(house);
            map.RemoveHouseAt(damaged[i]);
        }

        map.NotifyWallsDemolished(result.Cells, result.Collapsed.Count);
        return result.Any;
    }

    // ---- Ağ istemcisi: yeni haritada hangi duvarlar yıkılmış? ----

    /// <summary>Haritadaki evlerin şu anki duvar karelerini listeye yazar (yeni durum uygulanmadan önce çağrılır).</summary>
    public static void SnapshotHouseWalls(TerrainMap map, List<DemolishedCell> into)
    {
        into.Clear();
        if (map == null) return;
        foreach (House house in map.Houses)
            for (int y = house.Y; y < house.Y + house.H; y++)
                for (int x = house.X; x < house.X + house.W; x++)
                    if (TerrainMap.IsHouseWall(map.At(x, y))) into.Add(new DemolishedCell(x, y, map.At(x, y)));
    }

    /// <summary>
    /// Önceki duvarlar (<see cref="SnapshotHouseWalls"/>) ile yeni haritayı karşılaştırır: zemine dönen duvarlar yıkılmıştır (yanıp
    /// kömürleşenler sayılmaz, onlar yangındır). Yeni ev listesi eskinin alt kümesi değilse (yeni maçın haritası geldi) yıkım sayılmaz.
    /// Çöken ev sayısı = eksilen ev sayısı. Yıkım varsa true.
    /// </summary>
    public static bool FindDemolished(TerrainMap map, IReadOnlyList<DemolishedCell> wallsBefore, IReadOnlyList<House> housesBefore,
                                      List<DemolishedCell> into, out int collapsedHouses)
    {
        into.Clear();
        collapsedHouses = 0;
        if (map == null || wallsBefore == null || housesBefore == null) return false;

        foreach (House house in map.Houses)
            if (!Contains(housesBefore, house)) return false;

        foreach (DemolishedCell wall in wallsBefore)
        {
            TileType now = map.At(wall.X, wall.Y);
            if (TerrainMap.IsSolid(now) || now == TileType.CharredFloor) continue;
            into.Add(wall);
        }

        collapsedHouses = Mathf.Max(0, housesBefore.Count - map.Houses.Count);
        return into.Count > 0;
    }

    static bool Contains(IReadOnlyList<House> list, House house)
    {
        foreach (House other in list)
            if (Same(other, house)) return true;
        return false;
    }

    /// <summary>İki ev aynı mı (yer, boyut ve kapı)?</summary>
    public static bool Same(House a, House b)
    {
        return a.X == b.X && a.Y == b.Y && a.W == b.W && a.H == b.H && a.HasDoor == b.HasDoor
            && (!a.HasDoor || (a.DoorX == b.DoorX && a.DoorUp == b.DoorUp));
    }
}
