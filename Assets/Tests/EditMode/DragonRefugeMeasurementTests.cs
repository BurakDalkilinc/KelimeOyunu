using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Ejderha (ateşli global tehdit) ölçümü: tehdit yazıldığında rastgele yerdeki 8 oyuncu (oyuncu hızıyla) en yakın mağaraya düz koşar;
/// mağaraya ulaşma oranı Console'a yazılır (bkz. <see cref="DragonRefugeSim"/>). Hedef: en az %80'i hasar almadan sığınır.
/// </summary>
public class DragonRefugeMeasurementTests
{
    const int Runs = 300;
    const int Seed = 4321;

    GameConfig config;

    [SetUp]
    public void SetUp() => config = ScriptableObject.CreateInstance<GameConfig>();

    [TearDown]
    public void TearDown()
    {
        if (config != null) Object.DestroyImmediate(config);
    }

    static WordEntry Word(string id) => WordDatabase.LoadFromResources(50).FindById(id);

    [Test]
    public void Measure_FireThreats_LogsCaveReachRates()
    {
        foreach (string id in new[] { "ejderha", "anka_kusu", "ates_devi" })
        {
            WordEntry word = Word(id);
            Assert.IsNotNull(word, id);
            DragonRefugeSim.Result r = DragonRefugeSim.Run(word, config, Runs, Seed, config.playerMoveSpeed);
            Debug.Log("[Ejderha ölçümü] " + DragonRefugeSim.Format(id, r));
        }
    }

    [Test]
    public void Ejderha_AtLeast80Percent_ReachTheCaveInTime_AndUnhurt()
    {
        DragonRefugeSim.Result r = DragonRefugeSim.Run(Word("ejderha"), config, Runs, Seed, config.playerMoveSpeed);

        Assert.GreaterOrEqual(r.InTimeRate, 0.8f, "Uyarı süresinde mağaraya koşarak yetişen");
        Assert.GreaterOrEqual(r.SafeRate, 0.8f, "Hiç hasar almadan mağaraya giren");
        Assert.GreaterOrEqual(r.Zones * r.Capacity, r.Players, "Mağaralar herkesi alır");
    }
}
