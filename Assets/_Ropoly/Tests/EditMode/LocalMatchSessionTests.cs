using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Match;

namespace Ropoly.Tests.EditMode
{
    public sealed class LocalMatchSessionTests
    {
        private static readonly string[] CreatureIds =
        {
            "pip", "bubu", "sunny", "peach", "mochi", "nibi", "poppy", "bean",
        };

        [Test]
        public void NewSession_CreatesSerializableLobbyPlayersFromRules()
        {
            LocalMatchSession session = new LocalMatchSession(4, 1500, CreatureIds);

            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Lobby));
            Assert.That(session.State.Players, Has.Count.EqualTo(4));
            Assert.That(session.State.Players.Select(player => player.Cash), Is.All.EqualTo(1500));
            Assert.That(session.State.Players.Select(player => player.BoardPosition), Is.All.Zero);
            Assert.That(session.CanStart, Is.False);
        }

        [Test]
        public void CreatureSelection_IsExclusiveAcrossPlayers()
        {
            LocalMatchSession session = new LocalMatchSession(2, 1500, CreatureIds);

            Assert.That(
                session.TrySelectCreature(0, "pip"),
                Is.EqualTo(CreatureSelectionResult.Success));
            Assert.That(
                session.TrySelectCreature(1, "pip"),
                Is.EqualTo(CreatureSelectionResult.CreatureAlreadySelected));
            Assert.That(session.State.Players[1].IsReady, Is.False);
        }

        [Test]
        public void MatchStartsOnlyAfterEveryIncludedPlayerChooses()
        {
            LocalMatchSession session = new LocalMatchSession(2, 900, CreatureIds);

            Assert.That(session.TryStartMatch(), Is.False);
            session.TrySelectCreature(0, "pip");
            session.TrySelectCreature(1, "bubu");

            Assert.That(session.CanStart, Is.True);
            Assert.That(session.TryStartMatch(), Is.True);
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.InProgress));
            Assert.That(
                session.TrySelectCreature(0, "sunny"),
                Is.EqualTo(CreatureSelectionResult.MatchAlreadyStarted));
        }

        [Test]
        public void PlayerCount_CanChangeOnlyWithinLobbyLimits()
        {
            LocalMatchSession session = new LocalMatchSession(4, 1500, CreatureIds);
            session.TrySelectCreature(3, "peach");

            Assert.That(session.TrySetPlayerCount(2), Is.True);
            Assert.That(session.State.Players, Has.Count.EqualTo(2));
            Assert.That(session.TrySetPlayerCount(1), Is.False);
            Assert.That(session.TrySetPlayerCount(5), Is.False);
            Assert.That(session.TrySetPlayerCount(4), Is.True);
            Assert.That(session.State.Players[3].IsReady, Is.False);
        }

        [Test]
        public void AuthoritativeDiceRoll_RecordsValuesAndControlsTurnOrder()
        {
            LocalMatchSession session = CreateStartedTwoPlayerSession();

            Assert.That(session.State.Turn, Is.Not.Null);
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.State.Turn.TurnNumber, Is.EqualTo(1));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));

            Assert.That(
                session.TryRollDice(new FixedDiceRollSource(3, 5), out DiceRoll roll),
                Is.True);
            Assert.That(roll.FirstDie, Is.EqualTo(3));
            Assert.That(roll.SecondDie, Is.EqualTo(5));
            Assert.That(roll.Total, Is.EqualTo(8));
            Assert.That(roll.IsDouble, Is.False);
            Assert.That(session.State.Turn.LastRoll, Is.SameAs(roll));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingMovement));
            Assert.That(
                session.TryRollDice(new FixedDiceRollSource(1, 1), out _),
                Is.False,
                "A player cannot roll twice before the turn advances.");

            Assert.That(session.TryAdvanceTurn(), Is.True);
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.EqualTo(1));
            Assert.That(session.State.Turn.TurnNumber, Is.EqualTo(2));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));
            Assert.That(session.State.Turn.LastRoll, Is.Null);

            Assert.That(
                session.TryRollDice(new FixedDiceRollSource(6, 6), out DiceRoll doubleRoll),
                Is.True);
            Assert.That(doubleRoll.IsDouble, Is.True);
            Assert.That(session.TryAdvanceTurn(), Is.True);
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.State.Turn.TurnNumber, Is.EqualTo(3));
        }

        [Test]
        public void AuthoritativeDiceRoll_RejectsInvalidSourceValues()
        {
            LocalMatchSession session = CreateStartedTwoPlayerSession();

            Assert.That(
                session.TryRollDice(new FixedDiceRollSource(0, 7), out DiceRoll roll),
                Is.False);
            Assert.That(roll, Is.Null);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));
        }

        private static LocalMatchSession CreateStartedTwoPlayerSession()
        {
            LocalMatchSession session = new LocalMatchSession(2, 1500, CreatureIds);
            session.TrySelectCreature(0, "pip");
            session.TrySelectCreature(1, "bubu");
            Assert.That(session.TryStartMatch(), Is.True);
            return session;
        }

        private sealed class FixedDiceRollSource : IDiceRollSource
        {
            private readonly Queue<int> _values;

            public FixedDiceRollSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int NextDieValue()
            {
                return _values.Dequeue();
            }
        }
    }
}
