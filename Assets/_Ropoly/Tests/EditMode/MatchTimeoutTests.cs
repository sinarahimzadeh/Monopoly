using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Core.Match;
using Ropoly.Core.Time;

namespace Ropoly.Tests.EditMode
{
    public sealed class MatchTimeoutTests
    {
        private static readonly string[] CreatureIds =
        {
            "pip", "bubu", "sunny", "peach", "mochi", "nibi", "poppy", "bean",
        };

        [Test]
        public void MatchStart_SetsTurnDeadline()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(playerCount: 2, turnDurationSeconds: 60, clock: testClock);

            Assert.That(session.TryStartMatch(), Is.True);
            Assert.That(session.State.Turn.Deadline, Is.EqualTo(initialTime.AddSeconds(60)));
            Assert.That(session.CurrentTurnRemainingTime?.TotalSeconds, Is.EqualTo(60));
        }

        [Test]
        public void InactivityTimeout_EliminatesCurrentPlayerReleasesAssetsAndAdvancesTurn()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(
                playerCount: 3,
                turnDurationSeconds: 60,
                clock: testClock,
                board: CreateTwoPropertyBoard());

            session.TryStartMatch();

            // Player 0 buys a property
            session.TryRollDice(new FixedDiceRollSource(1, 1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            Assert.That(session.TryPurchaseCurrentProperty(), Is.EqualTo(PropertyPurchaseResult.Success));
            Assert.That(session.TryGetPropertyAt(2, out PropertyState property), Is.True);
            Assert.That(property.OwnerPlayerIndex, Is.EqualTo(0));

            // Player 0's turn is awaiting end, but Player 0 becomes inactive
            int eliminatedPlayer = -1;
            EliminationReason eliminatedReason = (EliminationReason)(-1);
            session.PlayerEliminated += (idx, reason) =>
            {
                eliminatedPlayer = idx;
                eliminatedReason = reason;
            };

            testClock.AdvanceSeconds(61);
            bool stateChanged = session.Tick();

            Assert.That(stateChanged, Is.True);
            Assert.That(eliminatedPlayer, Is.EqualTo(0));
            Assert.That(eliminatedReason, Is.EqualTo(EliminationReason.InactivityTimeout));
            Assert.That(session.State.Players[0].IsEliminated, Is.True);
            Assert.That(session.State.Players[0].Cash, Is.Zero);
            Assert.That(property.IsOwned, Is.False, "Assets must return to the bank.");

            // Match continues with Player 1's turn
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.InProgress));
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.EqualTo(1));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));
        }

        [Test]
        public void InactivityTimeout_InTwoPlayerMatch_FinishesMatchWithRemainingPlayerAsWinner()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(playerCount: 2, turnDurationSeconds: 60, clock: testClock);

            session.TryStartMatch();

            // Player 0 doesn't roll, timeout expires
            testClock.AdvanceSeconds(60);
            session.Tick();

            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(session.State.WinnerPlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void DebtTimeout_ExecutesAutoBankruptcyAndElimination()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateDebtSession(playerCount: 3, clock: testClock);

            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.DebtResolution));
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.EqualTo(1));

            int eliminatedPlayer = -1;
            EliminationReason eliminatedReason = (EliminationReason)(-1);
            session.PlayerEliminated += (idx, reason) =>
            {
                eliminatedPlayer = idx;
                eliminatedReason = reason;
            };

            // Player 1 is in debt and does not resolve it in time
            testClock.AdvanceSeconds(61);
            bool stateChanged = session.Tick();

            Assert.That(stateChanged, Is.True);
            Assert.That(eliminatedPlayer, Is.EqualTo(1));
            Assert.That(eliminatedReason, Is.EqualTo(EliminationReason.DebtTimeout));
            Assert.That(session.State.Players[1].IsEliminated, Is.True);
            Assert.That(session.State.Turn.RentPayment.Status, Is.EqualTo(RentPaymentStatus.CancelledByBankruptcy));

            // Turn advances to Player 2
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.InProgress));
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.EqualTo(2));
        }

        [Test]
        public void AcceptedAction_ResetsTurnDeadline()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(playerCount: 2, turnDurationSeconds: 60, clock: testClock);

            session.TryStartMatch();

            // 40 seconds pass
            testClock.AdvanceSeconds(40);
            Assert.That(session.CurrentTurnRemainingTime?.TotalSeconds, Is.EqualTo(20));

            // Player takes an action (rolls dice)
            session.TryRollDice(new FixedDiceRollSource(2, 3), out _);

            // Deadline is reset back to full turn duration from now
            Assert.That(session.CurrentTurnRemainingTime?.TotalSeconds, Is.EqualTo(60));
        }

        [Test]
        public void Disconnect_StartsGracePeriodAndDoesNotImmediatelyEliminate()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(
                playerCount: 3,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: testClock);

            session.TryStartMatch();

            Assert.That(session.TryDisconnectPlayer(2), Is.True);
            Assert.That(session.State.Players[2].IsDisconnected, Is.True);
            Assert.That(session.GetPlayerDisconnectRemainingTime(2)?.TotalSeconds, Is.EqualTo(60));

            // 30 seconds pass
            testClock.AdvanceSeconds(30);
            session.Tick();

            Assert.That(session.State.Players[2].IsEliminated, Is.False);
            Assert.That(session.GetPlayerDisconnectRemainingTime(2)?.TotalSeconds, Is.EqualTo(30));
        }

        [Test]
        public void Reconnect_BeforeGracePeriodExpires_RestoresConnectedStatus()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(
                playerCount: 3,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: testClock);

            session.TryStartMatch();
            session.TryDisconnectPlayer(2);

            testClock.AdvanceSeconds(30);
            Assert.That(session.TryReconnectPlayer(2), Is.True);
            Assert.That(session.State.Players[2].IsConnected, Is.True);
            Assert.That(session.GetPlayerDisconnectRemainingTime(2), Is.Null);

            // Even if more time passes, Player 2 is NOT eliminated
            testClock.AdvanceSeconds(60);
            session.Tick();
            Assert.That(session.State.Players[2].IsEliminated, Is.False);
        }

        [Test]
        public void DisconnectTimeout_EliminatesPlayerAndReleasesAssets()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(
                playerCount: 3,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: testClock);

            session.TryStartMatch();
            session.TryDisconnectPlayer(1);

            int eliminatedPlayer = -1;
            EliminationReason eliminatedReason = (EliminationReason)(-1);
            session.PlayerEliminated += (idx, reason) =>
            {
                eliminatedPlayer = idx;
                eliminatedReason = reason;
            };

            testClock.AdvanceSeconds(61);
            bool stateChanged = session.Tick();

            Assert.That(stateChanged, Is.True);
            Assert.That(eliminatedPlayer, Is.EqualTo(1));
            Assert.That(eliminatedReason, Is.EqualTo(EliminationReason.DisconnectTimeout));
            Assert.That(session.State.Players[1].IsEliminated, Is.True);
        }

        [Test]
        public void EliminatedPlayer_IsSkippedBySubsequentTurns()
        {
            var initialTime = new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            LocalMatchSession session = CreateSession(
                playerCount: 3,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: testClock);

            session.TryStartMatch();

            // Player 1 disconnects and times out during Player 0's turn
            session.TryDisconnectPlayer(1);
            testClock.AdvanceSeconds(61);
            session.Tick();
            Assert.That(session.State.Players[1].IsEliminated, Is.True);

            // Player 0 finishes turn
            session.TryRollDice(new FixedDiceRollSource(1, 1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryAdvanceTurn();

            // Turn skips Player 1 and goes directly to Player 2
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.EqualTo(2));
        }

        private static LocalMatchSession CreateSession(
            int playerCount,
            int turnDurationSeconds = 60,
            int disconnectGraceSeconds = 60,
            IClock clock = null,
            BoardSnapshot board = null)
        {
            LocalMatchSession session = new LocalMatchSession(
                playerCount,
                1500,
                CreatureIds,
                board ?? CreateSimpleBoard(),
                passStartCash: 200,
                mortgageEnabled: true,
                turnDurationSeconds: turnDurationSeconds,
                disconnectGraceSeconds: disconnectGraceSeconds,
                clock: clock);

            for (int i = 0; i < playerCount; i++)
            {
                session.TrySelectCreature(i, CreatureIds[i]);
            }

            return session;
        }

        private static LocalMatchSession CreateDebtSession(int playerCount, IClock clock)
        {
            LocalMatchSession session = new LocalMatchSession(
                playerCount,
                150,
                CreatureIds,
                CreateDebtResolutionBoard(),
                passStartCash: 0,
                mortgageEnabled: true,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: clock);

            for (int i = 0; i < playerCount; i++)
            {
                session.TrySelectCreature(i, CreatureIds[i]);
            }

            session.TryStartMatch();

            // Player 0 buys rent property on tile 4
            session.TryRollDice(new FixedDiceRollSource(2, 2), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryPurchaseCurrentProperty();
            session.TryAdvanceTurn();

            // Player 1 buys rescue asset on tile 2
            session.TryRollDice(new FixedDiceRollSource(1, 1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryPurchaseCurrentProperty();
            session.TryAdvanceTurn();

            if (playerCount == 3)
            {
                // Player 2 takes turn
                session.TryRollDice(new FixedDiceRollSource(1, 2), out _);
                session.TryBeginMovement(out _);
                session.TryCompleteMovement();
                session.TryAdvanceTurn();
            }

            // Player 0 takes turn
            session.TryRollDice(new FixedDiceRollSource(1, 1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryAdvanceTurn();

            // Player 1 lands on tile 4 (rent property), owes $90
            session.TryRollDice(new FixedDiceRollSource(1, 1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();

            return session;
        }

        private static BoardSnapshot CreateSimpleBoard()
        {
            return new BoardSnapshot(
                schemaVersion: 1,
                boardId: "simple-test",
                displayName: "Simple Test",
                countries: new CountrySnapshot[0],
                cities: new CitySnapshot[0],
                airports: new AirportSnapshot[0],
                utilities: new UtilitySnapshot[0],
                specialSpaces: new[]
                {
                    new SpecialSpaceSnapshot("start", "Start", SpecialSpaceKind.Start, string.Empty, 0),
                    new SpecialSpaceSnapshot("rest", "Rest", SpecialSpaceKind.Vacation, string.Empty, 0),
                },
                tiles: new[]
                {
                    new BoardTileSnapshot(0, BoardTileKind.Special, "start"),
                    new BoardTileSnapshot(1, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(2, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(3, BoardTileKind.Special, "rest"),
                });
        }

        private static BoardSnapshot CreateTwoPropertyBoard()
        {
            return new BoardSnapshot(
                schemaVersion: 1,
                boardId: "two-property-test",
                displayName: "Two Property Test",
                countries: new CountrySnapshot[0],
                cities: new[]
                {
                    new CitySnapshot("city1", "City 1", 200, 100, 100, new[] { 20 }),
                    new CitySnapshot("city2", "City 2", 200, 100, 100, new[] { 20 }),
                },
                airports: new AirportSnapshot[0],
                utilities: new UtilitySnapshot[0],
                specialSpaces: new[]
                {
                    new SpecialSpaceSnapshot("start", "Start", SpecialSpaceKind.Start, string.Empty, 0),
                    new SpecialSpaceSnapshot("rest", "Rest", SpecialSpaceKind.Vacation, string.Empty, 0),
                },
                tiles: new[]
                {
                    new BoardTileSnapshot(0, BoardTileKind.Special, "start"),
                    new BoardTileSnapshot(1, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(2, BoardTileKind.City, "city1"),
                    new BoardTileSnapshot(3, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(4, BoardTileKind.City, "city2"),
                    new BoardTileSnapshot(5, BoardTileKind.Special, "rest"),
                });
        }

        private static BoardSnapshot CreateDebtResolutionBoard()
        {
            return new BoardSnapshot(
                schemaVersion: 1,
                boardId: "debt-board",
                displayName: "Debt Board",
                countries: new CountrySnapshot[0],
                cities: new[]
                {
                    new CitySnapshot("rescue", "Rescue Asset", 100, 50, 100, new[] { 10 }),
                    new CitySnapshot("rent", "Rent Property", 25, 12, 100, new[] { 90 }),
                },
                airports: new AirportSnapshot[0],
                utilities: new UtilitySnapshot[0],
                specialSpaces: new[]
                {
                    new SpecialSpaceSnapshot("start", "Start", SpecialSpaceKind.Start, string.Empty, 0),
                    new SpecialSpaceSnapshot("rest", "Rest", SpecialSpaceKind.Vacation, string.Empty, 0),
                },
                tiles: new[]
                {
                    new BoardTileSnapshot(0, BoardTileKind.Special, "start"),
                    new BoardTileSnapshot(1, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(2, BoardTileKind.City, "rescue"),
                    new BoardTileSnapshot(3, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(4, BoardTileKind.City, "rent"),
                    new BoardTileSnapshot(5, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(6, BoardTileKind.Special, "rest"),
                    new BoardTileSnapshot(7, BoardTileKind.Special, "rest"),
                });
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
