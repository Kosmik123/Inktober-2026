using Inktober2026.Game;
using NUnit.Framework;
using UnityEngine;

namespace Inktober2026.Tests.EditMode
{
    public class GameFlowTests
    {
        private GameObject gameObject;
        private GameFlow flow;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Game Flow");
            flow = gameObject.AddComponent<GameFlow>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void StartsReady()
        {
            Assert.AreEqual(GameState.Ready, flow.State);
        }

        [Test]
        public void BeginRoundMovesFromReadyToPlaying()
        {
            flow.BeginRound();

            Assert.AreEqual(GameState.Playing, flow.State);
        }

        [Test]
        public void WinIsIgnoredBeforeTheRoundBegins()
        {
            flow.Win();

            Assert.AreEqual(GameState.Ready, flow.State);
        }

        [Test]
        public void WinEndsTheRoundThatIsPlayed()
        {
            flow.BeginRound();
            flow.Win();

            Assert.AreEqual(GameState.Won, flow.State);
        }

        [Test]
        public void RestartReturnsToReadyFromAnyState()
        {
            flow.BeginRound();
            flow.Win();
            flow.Restart();

            Assert.AreEqual(GameState.Ready, flow.State);
        }

        [Test]
        public void EventIsRaisedWithTheNewStateOnly()
        {
            var states = new System.Collections.Generic.List<GameState>();
            flow.OnStateChanged += states.Add;

            flow.BeginRound();
            flow.BeginRound();
            flow.Win();
            flow.Restart();

            CollectionAssert.AreEqual(new[] { GameState.Playing, GameState.Won, GameState.Ready }, states);
        }
    }
}
