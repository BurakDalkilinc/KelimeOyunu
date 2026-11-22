using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>TargetSelection (süre ve doğrulama) ve TargetHitTester (tıklanan oyuncu) testleri.</summary>
public class TargetSelectionTests
{
    class FakeWorld : IPlayerLocator
    {
        readonly List<PlayerState> players = new List<PlayerState>();
        readonly Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>();

        public IReadOnlyList<PlayerState> Players => players;

        public PlayerState Add(int id, Vector2 position)
        {
            var player = new PlayerState(id, "P" + id, 3);
            players.Add(player);
            positions[id] = position;
            return player;
        }

        public void Forget(int id) => positions.Remove(id);

        public bool TryGetPosition(int playerId, out Vector2 position) => positions.TryGetValue(playerId, out position);
    }

    static PlayerState NewPlayer(int id) => new PlayerState(id, "P" + id, 3);

    // ---- TargetSelection ----

    [Test]
    public void New_IsPendingWithFullTime()
    {
        var selection = new TargetSelection(NewPlayer(1), 3f);

        Assert.AreEqual(TargetSelectionState.Pending, selection.State);
        Assert.IsTrue(selection.IsPending);
        Assert.AreEqual(3f, selection.TimeLeft, 0.0001f);
        Assert.IsNull(selection.Chosen);
    }

    [Test]
    public void TrySelect_ValidOpponent_SelectsAndLocks()
    {
        var writer = NewPlayer(1);
        var other = NewPlayer(2);
        var selection = new TargetSelection(writer, 3f);

        Assert.IsTrue(selection.TrySelect(other));

        Assert.AreEqual(TargetSelectionState.Selected, selection.State);
        Assert.AreSame(other, selection.Chosen);
        Assert.IsFalse(selection.TrySelect(NewPlayer(3)), "Seçim bittikten sonra değişmez");
        Assert.AreSame(other, selection.Chosen);
    }

    [Test]
    public void TrySelect_Writer_IsRejected()
    {
        var writer = NewPlayer(1);
        var selection = new TargetSelection(writer, 3f);

        Assert.IsFalse(selection.TrySelect(writer));
        Assert.IsTrue(selection.IsPending);
    }

    [Test]
    public void TrySelect_EliminatedOrNull_IsRejected()
    {
        var dead = NewPlayer(2);
        dead.Health.TakeDamage(99);
        var selection = new TargetSelection(NewPlayer(1), 3f);

        Assert.IsFalse(selection.TrySelect(dead));
        Assert.IsFalse(selection.TrySelect(null));
        Assert.IsTrue(selection.IsPending);
    }

    [Test]
    public void Tick_TimesOutExactlyWhenTimeRunsOut()
    {
        var selection = new TargetSelection(NewPlayer(1), 3f);

        selection.Tick(2.9f);
        Assert.IsTrue(selection.IsPending);

        selection.Tick(0.1f);
        Assert.AreEqual(TargetSelectionState.TimedOut, selection.State);
        Assert.AreEqual(0f, selection.TimeLeft, 0.0001f);
    }

    [Test]
    public void TrySelect_AfterTimeout_IsRejected()
    {
        var selection = new TargetSelection(NewPlayer(1), 1f);
        selection.Tick(2f);

        Assert.IsFalse(selection.TrySelect(NewPlayer(2)));
        Assert.AreEqual(TargetSelectionState.TimedOut, selection.State);
    }

    [Test]
    public void Tick_AfterSelected_DoesNotTimeOut()
    {
        var selection = new TargetSelection(NewPlayer(1), 1f);
        selection.TrySelect(NewPlayer(2));

        selection.Tick(10f);

        Assert.AreEqual(TargetSelectionState.Selected, selection.State);
    }

    [Test]
    public void Tick_NegativeDelta_DoesNotAddTime()
    {
        var selection = new TargetSelection(NewPlayer(1), 3f);
        selection.Tick(-5f);
        Assert.AreEqual(3f, selection.TimeLeft, 0.0001f);
    }

    [Test]
    public void ZeroSeconds_IsTimedOutImmediately()
    {
        var selection = new TargetSelection(NewPlayer(1), 0f);
        Assert.AreEqual(TargetSelectionState.TimedOut, selection.State);
    }

    [Test]
    public void Cancel_StopsPending_ButNotAFinishedSelection()
    {
        var pending = new TargetSelection(NewPlayer(1), 3f);
        pending.Cancel();
        Assert.AreEqual(TargetSelectionState.Cancelled, pending.State);
        Assert.IsFalse(pending.TrySelect(NewPlayer(2)));

        var done = new TargetSelection(NewPlayer(1), 3f);
        done.TrySelect(NewPlayer(2));
        done.Cancel();
        Assert.AreEqual(TargetSelectionState.Selected, done.State);
    }

    [Test]
    public void Constructor_NullWriter_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new TargetSelection(null, 3f));
    }

    // ---- TargetHitTester ----

    [Test]
    public void FindAt_ClickOnPlayer_ReturnsThatPlayer()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, new Vector2(0f, 0f));
        var other = world.Add(2, new Vector2(5f, 2f));

        Assert.AreSame(other, TargetHitTester.FindAt(new Vector2(5.3f, 2.1f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_ClickOnEmptyGround_ReturnsNull()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, Vector2.zero);
        world.Add(2, new Vector2(5f, 2f));

        Assert.IsNull(TargetHitTester.FindAt(new Vector2(-6f, -3f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_ClickOnWriter_ReturnsNull()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, new Vector2(1f, 1f));

        Assert.IsNull(TargetHitTester.FindAt(new Vector2(1f, 1f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_ClickOnEliminatedPlayer_ReturnsNull()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, Vector2.zero);
        var dead = world.Add(2, new Vector2(4f, 0f));
        dead.Health.TakeDamage(99);

        Assert.IsNull(TargetHitTester.FindAt(new Vector2(4f, 0f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_TwoPlayersClose_PicksTheNearest()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, new Vector2(-8f, 0f));
        var near = world.Add(2, new Vector2(2.2f, 0f));
        world.Add(3, new Vector2(2.9f, 0f));

        Assert.AreSame(near, TargetHitTester.FindAt(new Vector2(2.4f, 0f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_RadiusIsInclusiveAndRespected()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, new Vector2(-8f, 0f));
        var other = world.Add(2, new Vector2(0f, 0f));

        Assert.AreSame(other, TargetHitTester.FindAt(new Vector2(0.9f, 0f), world, writer, 0.9f));
        Assert.IsNull(TargetHitTester.FindAt(new Vector2(0.95f, 0f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_PlayerWithoutPosition_IsSkipped()
    {
        var world = new FakeWorld();
        var writer = world.Add(1, Vector2.zero);
        world.Add(2, new Vector2(3f, 0f));
        world.Forget(2);

        Assert.IsNull(TargetHitTester.FindAt(new Vector2(3f, 0f), world, writer, 0.9f));
    }

    [Test]
    public void FindAt_NullWorld_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => TargetHitTester.FindAt(Vector2.zero, null, null, 1f));
    }
}
