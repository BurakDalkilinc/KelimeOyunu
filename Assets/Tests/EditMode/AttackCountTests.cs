using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Saldırı animasyonunun dayandığı vuruş sayacı: her vuruşta artar ve ağ üzerinden taşınır.</summary>
public class AttackCountTests
{
    class FakeWorld : ICreatureWorld
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();
        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position)
        {
            var player = new PlayerState(id, "P" + id, 10);
            players.Add(player);
            positions[id] = position;
            return player;
        }

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
        public bool TryApplyKnockback(int playerId, Vector2 distance) => false;
    }

    [Test]
    public void ChasingCreature_CountsEachHit()
    {
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
        try
        {
            var world = new FakeWorld();
            PlayerState target = world.Add(1, new Vector2(0.3f, 0f));
            var word = new WordEntry
            {
                id = "kurt", display = "Kurt", category = WordEntry.CategoryAttack, scope = WordEntry.ScopeTarget,
                tags = new string[0], stats = new WordStats { damage = 1, speed = 4f, duration = 20f, radius = 0.4f },
            };
            CreatureModel wolf = CreatureFactory.CreateAttack(word, target, Vector2.zero, config, new ArenaBounds(new Vector2(34f, 19f)), 0, 1);
            wolf.Persistent = true;
            Assert.AreEqual(0, wolf.AttackCount);

            var hits = new List<CreatureHit>();
            for (int i = 0; i < 600; i++) CreatureDriver.Advance(wolf, 1f / 60f, world, config, hits, null);

            Assert.Greater(wolf.AttackCount, 1, "her vuruşta artar");
            Assert.AreEqual(10 - wolf.AttackCount, target.Health.Current, "vuruş sayısı verilen hasarla aynı");
        }
        finally { Object.DestroyImmediate(config); }
    }

    [Test]
    public void SnapshotCodec_CarriesAttackCount()
    {
        var list = new List<CreatureSnapshot> { new CreatureSnapshot { Id = 3, WordId = "godzilla", X = 1f, Y = 2f, Radius = 1.3f, Label = "Godzilla 5/12", Attacks = 7 } };
        Assert.IsTrue(CreatureSnapshotCodec.TryDecode(CreatureSnapshotCodec.Encode(list), out List<CreatureSnapshot> back));
        Assert.AreEqual(7, back[0].Attacks);
    }
}
