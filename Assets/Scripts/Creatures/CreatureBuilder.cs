using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kabul edilen saldırı kelimesinden sahneye konacak yaratık modellerini kurar: sürü sayısı, doğma noktası,
/// yumruk/bomba/muz kabuğunun yerleşimi. Tek kişilik sahnede <see cref="CreatureSpawner"/>, ağ maçında host bunu kullanır.
/// Düz C# sınıfı, test edilebilir.
/// </summary>
public static class CreatureBuilder
{
    /// <summary>
    /// Saldırı kelimesi için model(ler) kurar. Sürü kelimesi birden fazla model verir; ilki (index 0) etiketi taşır.
    /// <paramref name="targetPosition"/> hedefin o anki konumudur. <paramref name="map"/> verilirse yaratık evin içinde, duvarda ya da
    /// tehlikeli zeminde doğmaz: gövdesinin sığdığı en yakın yere kaydırılır (bkz. <see cref="SpawnPlacement"/>).
    /// </summary>
    public static List<CreatureModel> BuildAttack(WordEntry word, PlayerState target, Vector2 targetPosition,
                                                  GameConfig config, ArenaBounds arena, System.Random random, TerrainMap map = null)
    {
        var models = new List<CreatureModel>();

        // Sürü (swarm) kelimesi birden fazla yaratık doğurur; hepsi aynı noktanın etrafında bir halkada başlar.
        int count = TagRegistry.GetSpawnCount(word);
        Vector2 center = ArenaSpawn.PickPoint(arena, targetPosition, config.creatureSpawnMinDistance, random);

        for (int i = 0; i < count; i++)
        {
            Vector2 spawn = count == 1 ? center : center + RingOffset(i, count, config.swarmSpawnRingRadius);
            CreatureModel model = CreatureFactory.CreateAttack(word, target, spawn, config, arena, i, count);

            // Mıknatıs arenanın merkezine konur ve hedefi oraya çeker.
            if (model.IsPull) model.PlaceAt(Vector2.zero);

            if (model.PlacesNearTarget)
            {
                // Yumruk hedefin tam üstüne düşer; patlayıcı (bomba) ve tuzak (muz kabuğu) yanına düşer.
                Vector2 at = model.PlacementOffset > 0f
                    ? ArenaSpawn.PointBeside(targetPosition, model.PlacementOffset, random)
                    : targetPosition;
                model.PlaceAt(at);
            }
            else if (!model.IsPull) MoveToClearSpot(model, map); // Hedefin üstüne/yanına düşenler ve mıknatıs olduğu yerde kalır

            if (config.creaturesHuntEveryone) MakeHunter(model);
            CreatureAggression.Apply(model, word, config); // Hız/ömür çarpanı, atılma, sendeletme, vuramadığından dönme
            models.Add(model);
        }

        return models;
    }

    /// <summary>
    /// Kovalayan yaratığı avcı yapar: vurduktan sonra kaybolmaz, en yakındakine geçer, hedef ölünce yenisini seçer.
    /// Yerinde patlayan, alan, tuzak, çekim ve hedefin üstüne düşen yaratıklar olduğu gibi kalır.
    /// </summary>
    public static void MakeHunter(CreatureModel model)
    {
        if (model == null || model.IsStrike || model.IsArea || model.IsTrap || model.IsPull || model.IsProjectile || model.PlacesNearTarget) return;
        if (model.Speed <= 0f) return;

        model.Persistent = true;
        model.TargetsNearest = true;
        model.RetargetsWhenTargetDies = true;
    }

    /// <summary>Bilinmeyen kelimenin zararsız "???" yaratığı: arenanın rastgele bir yerinde doğar.</summary>
    public static CreatureModel BuildUnknown(GameConfig config, ArenaBounds arena, System.Random random, TerrainMap map = null)
    {
        Vector2 spawn = ArenaSpawn.PickPoint(arena, Vector2.zero, 0f, random);
        CreatureModel model = CreatureFactory.CreateUnknown(spawn, config, arena);
        MoveToClearSpot(model, map);
        return model;
    }

    /// <summary>Yaratığı gövdesinin sığdığı (ev, duvar, tehlike olmayan) en yakın yere taşır. Harita yoksa bir şey yapmaz.</summary>
    static void MoveToClearSpot(CreatureModel model, TerrainMap map)
    {
        if (map == null || model == null) return;
        model.PlaceAt(SpawnPlacement.FindClearSpot(map, model.Position, model.Radius));
    }

    /// <summary>Sürü üyesinin halka üzerindeki yeri (üyeler eşit aralıkla dizilir).</summary>
    static Vector2 RingOffset(int index, int count, float radius)
    {
        float angle = Mathf.PI * 2f * index / count;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
